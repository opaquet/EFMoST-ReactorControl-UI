using Core.DataTypes;
using Core.Modules.Interfaces;
using System.Diagnostics;
using MathNet.Numerics.LinearAlgebra;


namespace Core.Modules {
    internal class ProcessSimulation : IProcessSimulation, IDisposable {

        public event Action<string, int>? LogEvent;
        public event Action<string>? StartupEvent;
        public event Action? SimulationStepFinished;
        public IReadOnlyList<double> SimTimeValues => GetThreadSafeValue(_allSimTimesUntilNow);
        public IReadOnlyList<double> SimTimePredictions => GetThreadSafeValue(_FutureSimTimes2Hours);
        public IReadOnlyList<double[]> SimStateValues => GetThreadSafeValue(_allSimValuesUntilNow);
        public IReadOnlyList<double[]> SimStatePredictions => GetThreadSafeValue(_FutureSimValues2Hours);

        private readonly object _lock = new object();
        private IAppSettings _appSettings;
        private IDataHandler _dataStore;
        private IReactorControl _reactorControl;

        private double _actualFeedRate = 0;
        private double _actualVentilationRate = 0;
        private double[] _simStateValues = new double[6];

        public double Biomass { get => _simStateValues[0]; set => _simStateValues[0] = value; }
        public double Sugar { get => _simStateValues[1]; set => _simStateValues[1] = value; }
        public double Ethanol { get => _simStateValues[2]; set => _simStateValues[2] = value; }
        public double Oxygen { get => _simStateValues[3] * 1000; set => _simStateValues[3] = value / 1000; }
        public double Temperature { get => _simStateValues[4] - 273.15; set => _simStateValues[4] = value + 273.15; }
        public double Volume { get => _simStateValues[5]; set => _simStateValues[5] = value; }
        public double TotalFeedVolume { get; private set; } = 0;

        public double DeltaTime { get; private set; } = 0;
        public double CalcTime { get; private set; } = 0;

        private List<double> _allSimTimesUntilNow = new List<double>();
        private List<double> _FutureSimTimes2Hours = new List<double>();

        private List<double[]> _allSimValuesUntilNow = new List<double[]>();
        private List<double[]> _FutureSimValues2Hours = new List<double[]>();

        private Timer? _simIntervalTimer;

        private double _tNow = 0;
        private double _tLast = 0;
        private DateTime _startTime = DateTime.Now;

        public ProcessSimulation(IAppSettings settings, IDataHandler dataHandler, IReactorControl reactorControl) {
            _appSettings = settings;
            _dataStore = dataHandler;
            _reactorControl = reactorControl;


        }

        private IReadOnlyList<T> GetThreadSafeValue<T>(List<T> list) {
            lock (_lock) { return new List<T>(list); }
        }

        public void Dispose() {
            LogEvent?.Invoke("SimModule stopped!", 1);
            _simIntervalTimer?.Dispose();
        }

        public void Begin() {
            LogEvent?.Invoke("SimModule started!", 1);

            // copy initial values from settings
            _simStateValues = new double[] { 
                _appSettings.SimSettings.StartBio,
                _appSettings.SimSettings.StartSugar,
                0.001,
                0.008,
                _appSettings.SimSettings.StartTemp,
                _appSettings.SimSettings.StartV
            };

            InitializeSigmaPoints();

            _simIntervalTimer?.Dispose();
            StartupEvent?.Invoke("starting controlIntervalTimer...");
            _simIntervalTimer = new Timer(SimTimerCallback, null, 0, Convert.ToInt32(_appSettings.SimSettings.SimDeltaTime * 1000));
        }

        public void Reset() {
            _simIntervalTimer?.Dispose();

            LogEvent?.Invoke("SimModule reset!", 1);
            lock (_lock) {
                _startTime = DateTime.Now;
                _tNow = 0;
                _tLast = 0;

                TotalFeedVolume = 0;

                _allSimTimesUntilNow = new List<double>();
                _FutureSimTimes2Hours = new List<double>();
                _allSimValuesUntilNow = new List<double[]>();
                _FutureSimValues2Hours = new List<double[]>();
            }

            _simIntervalTimer = new Timer(SimTimerCallback, null, 0, Convert.ToInt32(_appSettings.SimSettings.SimDeltaTime * 1000));
        }

        bool simstate = false;

        private void SimTimerCallback(object? state) {
            if (simstate) return;

            simstate = true;
            // determine time...
            _tNow = ( DateTime.Now - _startTime ).TotalSeconds;       
            DeltaTime = _tNow - _tLast;


            // Get control inputs to the system
            // when feed automation is active, it may run in pwm mode, and then, querying the setpoint wil result in wrong values
            if (_dataStore.GetLastControllerDatum().Automatic[3])
                _actualFeedRate = _reactorControl.CalculatedFeedRateValue / 3600;
            //else
            //    _acualFeedRate = _dataStore.GetLastControllerDatum().Setpoints[3] / 3600;
            _actualVentilationRate = _dataStore.GetLastControllerDatum().Setpoints[2] / 60;


            // calculate sigma points (adding/subtracting scaled errors to last known state)
            
            sigmaPoints[0] = (double[]) _simStateValues.Clone();
            for (int i = 0; i < N; i++) {
                sigmaPoints[i + 1] = (double[]) _simStateValues.Clone();
                sigmaPoints[i + 1 + N] = (double[]) _simStateValues.Clone();
                for (int j = 0; j < N; j++) {
                    sigmaPoints[i + 1][j]     += pFilt[i,j] * (N + lambda);
                    sigmaPoints[i + 1 + N][j] -= pFilt[i,j] * (N + lambda);
                }
            }
            Debug.Print(""); Debug.Print(""); Debug.Print(""); Debug.Print("");
            Debug.Print("*******************************************************************");

            // perform simulation for each sigma point
            for (int i = 0; i < NSP; i++) {
                (_, sigmaPointStates[i]) = RK45SolverLast(model: SimModel, initialValues: sigmaPoints[i], xStart: _tLast, xEnd: _tNow, tol: 1e-8);

            }
            Debug.Print("Prozesssimulation (Sigma Points):");

            // calculate weigted state value -> prediction
            for (int i = 0; i < N; i++) {
                _simStateValues[i] = 0;
                for (int j = 0; j < NSP; j++) {
                    _simStateValues[i] += sigmaPointStates[j][i] * w[j];
                }
            }
            Matrix<double> simState = Matrix<double>.Build.DenseOfRowArrays(_simStateValues);
            Debug.Print(simState.ToMatrixString());

            // estimate simulation error
            for (int i = 0; i < N; i++) {
                for (int j = 0; j < N; j++) {
                    P[i,j] = Q[i,j] * (_tNow - _tLast);
                }
            }
            for (int j = 0; j < NSP; j++) {
                for (int k = 0; k < N; k++) {
                    for (int l = 0; l < N; l++) {
                        P[k,l] += wc[j] * ( sigmaPointStates[j][k] - _simStateValues[k] ) * ( sigmaPointStates[j][l] - _simStateValues[l] );
                    }
                }
            }

            Debug.Print("Prozesskovarianz (Sigma Points):");
            Debug.Print(P.ToMatrixString());

            // Kalman filter calculation
            Debug.Print("Messung:");
            Matrix<double> measuredState = Matrix<double>.Build.DenseOfRowArrays(
                new double[] {
                _dataStore.GetLastComputedValueDatum().Ethanol, // ethanol
                _dataStore.GetLastControllerDatum().Values[7] / 1000,  // oxygen
                _dataStore.GetLastControllerDatum().Values[4] + 273.15,  // temperature
                _dataStore.GetLastControllerDatum().Values[0],  // volume
                }    
            );
            Debug.Print(measuredState.ToMatrixString());

            //more debug Print
            Debug.Print("Messmodell:");
            Debug.Print(H.ToMatrixString());

            Debug.Print("Messrauschen:");
            Debug.Print(R.ToMatrixString());

            Debug.Print("Prozessrauschen:");
            Debug.Print(Q.ToMatrixString());

            //Kalman Gain
            K = ( P*H.Transpose()*(H*P*H.Transpose()+R).Inverse());



            Debug.Print("Kalman Gain:"); 
            Debug.Print(K.ToMatrixString());

            // Filtered state
            Debug.Print("Zustand geschätzt (Kalman):");

            Matrix<double> fS = 
                ( simState.Transpose() + K  * ( measuredState.Transpose() - H*simState.Transpose() )).Transpose();
            
            Debug.Print(fS.ToMatrixString());


            for (int j = 0; j < N; j++) {
                _simStateValues[j] = fS[0,j];
            }



            // Filtered Error
            pFilt = (P - K * H * P);
            Debug.Print("Prozesskovarianz geschätzt (Kalman):");
            Debug.Print(pFilt.ToMatrixString());

            //later addition: sum up the total feed volume
            TotalFeedVolume += _actualFeedRate * ( _tNow - _tLast );

            // update values
            
            lock (_lock) {
                _allSimTimesUntilNow.Add(_tNow);
                _allSimValuesUntilNow.Add((double[]) _simStateValues.Clone());
            }


            // solve from now to 2 hours in the future
            (List<double> TL, List<double[]> YL) = RK45Solver(model: SimModel,
                                         initialValues: _simStateValues,
                                         xStart: _tNow,
                                         xEnd: _tNow + 3600,
                                         tol: 1e-8);
            lock (_lock) {
                _FutureSimTimes2Hours = TL.ToArray().ToList();
                _FutureSimValues2Hours = YL.ToArray().ToList();
            }
            simstate = false;

            CalcTime = ( DateTime.Now - _startTime ).TotalSeconds - _tNow;
            _tLast = _tNow;
            SimulationStepFinished?.Invoke();
        }


        double[] w, wc;
        Matrix<double> Q, R, H, P, K, pFilt;
        double[] _filteredStateValues;
        double[] measuredState;
        double[][] sigmaPoints;
        double[][] sigmaPointStates;
        int N, NSP;
        double lambda;

        private void InitializeSigmaPoints() {
            // solve for model error determination (sigma points / unscented transform)
            const double alpha = 0.01;
            const double kappa = 0;
            const double beta = 2;

            N = _simStateValues.Length;
            NSP = 2 * N + 1;
            lambda = alpha * alpha * ( N + kappa ) - N;
            w = new double[NSP];
            w[0] = lambda / ( N + lambda );
            for (int i = 1; i < NSP; i++) {
                w[i] = 1 / ( 2 * ( N + lambda ) );
            }


            H = Matrix<double>.Build.DenseOfArray(new double[,]  
                { { 0,0,1,0,0,0},
                  { 0,0,0,1,0,0},
                  { 0,0,0,0,1,0},
                  { 0,0,0,0,0,1} }
            );



            wc = new double[NSP];
            wc[0] = lambda / ( N + lambda ) + ( 1 - alpha * alpha + beta );
            for (int i = 1; i < NSP; i++) {
                wc[i] = w[i];
            }


            P = Matrix<double>.Build.Dense(N, N);
            K = Matrix<double>.Build.Dense(N, N);
            Q = Matrix<double>.Build.DenseOfDiagonalArray(new double[] { 0.2,  0.2,.1, 0.1, 0.2, 0.5});
            R = Matrix<double>.Build.DenseOfDiagonalArray(new double[] { 1, 0.2, 0.5, 10});
            pFilt = Q.Clone();



            sigmaPoints = new double[NSP][];
            for (int i = 0; i < NSP; i++) {
                sigmaPoints[i] = (double[]) _simStateValues.Clone();
            }

            sigmaPointStates = new double[NSP][];
            for (int i = 0; i < NSP; i++) {
                sigmaPointStates[i] = new double[N];
            }

            _filteredStateValues = (double[]) _simStateValues.Clone();
        }


        #region helper funtions for simulation model
        private double Arrhenius(double T) => _appSettings.SimSettings.ArrheniusA0 * Math.Exp(-_appSettings.SimSettings.ArrheniusEA / TSimulationSettings.GAS_CONSTANT / T);
        private double Monod(double S, double mueMax) => ( mueMax * S ) / ( _appSettings.SimSettings.MonodConstant + S );
        private double Sigmoid(double x, double mean, double width) => 1 - 1 / ( 1 + Math.Exp(-( x - mean ) * width) );
        private double CoolingSurface(double V) {
            double h = ( V / 1000 ) / ( Math.PI * Math.Pow(_appSettings.SimSettings.ReactorRadius, 2) );
            return    Math.PI * Math.Pow(_appSettings.SimSettings.ReactorRadius, 2) 
                      + 2 * Math.Pow(_appSettings.SimSettings.ReactorRadius, 2) * h;
        }
        #endregion

        /// <summary>
        /// The actual process model consisting of 6 coupled non-linear differential equations that take the system 
        /// state as well as the control inputs into account. Control inputs are taken from the class variables as 
        /// theese do not change that rapidly (at least in this model)
        /// </summary>
        /// <param name="x">the actual x or time value (not used in this case) (double)</param>
        /// <param name="y">the actual state of the system (double[])</param>
        /// <returns>the change in state (double[])</returns>
        /// <exception cref="ArgumentException"> the number of states must be 6, otherwise something went wrong...</exception>
        private double[] SimModel(double x, double[] y) {
            if (y.Length != 6) { throw new ArgumentException($"Lenght of y must be 6! It is {y.Length}..."); }

            // copy into new Varibales so that the names are easier to recognize in the calcuations
            double X  = Math.Max(y[0], _appSettings.SimSettings.StartBio);
            double G  = Math.Max(y[1],0);
            double E  = Math.Max(y[2],0);
            double O2 = Math.Max(y[3],0);
            double T  = Math.Max(y[4],273);
            double V  = Math.Max(y[5],0);

            // calculate a temp correction factor based on the arrhenius equation
            double Ar = Arrhenius(T) * Sigmoid(T, 273.15 + 39, .5);

            // oxygen consumption factor using a sigma so that oxygen consumpten is zero, when oxygen conectration is zero
            double ox_factor = 1 - Sigmoid(O2, 0.0001, 40000);

            // calculate actual rate coefficients based on modified monod kinetics
            double m_ox = Monod(G, _appSettings.SimSettings.MueMaxOx) * Ar * Sigmoid(G, 0.065, 400) * ox_factor;
            double m_red = Monod(G, _appSettings.SimSettings.MueMaxRed) * Ar * ( 1 - Sigmoid(G, 0.065, 400) );
            double m_eth = Monod(E, _appSettings.SimSettings.MueMaxEth) * Ar * ( 1 - Sigmoid(G, 0.065, 80) );

            // active cooling emulating an active temp control (increasing coolant flow to maximum if temp > 30 °C)
            double act_cool = 1 - Sigmoid(T, 303.15, 2);

            // all reaction heat terms calculated and combined into a single value
            double RE = V * X * ( 
                            m_ox / _appSettings.SimSettings.YieldGOx * ( -_appSettings.SimSettings.ReactionHeatOx/ TSimulationSettings.GLUCOSE_MOLAR_WEIGTH ) 
                          + m_red / _appSettings.SimSettings.YieldGRed * ( -_appSettings.SimSettings.ReactionHeatRed/ TSimulationSettings.GLUCOSE_MOLAR_WEIGTH ) 
                          + ( m_ox + m_red + m_eth ) * ( -_appSettings.SimSettings.ReactionHeatBiomass ) );

            // the actual 6 differential equations:
            double dX = X * ( m_ox + m_red + m_eth ) 
                        - _actualFeedRate / V * X;

            double dG = -X * ( m_ox / _appSettings.SimSettings.YieldGOx + m_red / _appSettings.SimSettings.YieldGRed ) 
                        + _actualFeedRate / V * ( _appSettings.SimSettings.FeedSugarConcentration - G );

            double dE = X * ( m_red / _appSettings.SimSettings.YieldGRed - m_eth / _appSettings.SimSettings.YieldGRed ) 
                        - _actualFeedRate / V * E;

            double dO2 = -X * ( _appSettings.SimSettings.RequiredOxygenOx * m_ox + _appSettings.SimSettings.RequiredOxygenRed * m_red ) * ox_factor 
                        - _actualFeedRate / V * O2           
                        + _actualVentilationRate / V  * (0.0082 - Math.Max(O2,0)) * 10;

            double dT = _actualFeedRate / V * ( _appSettings.SimSettings.FeedTemp - T ) 
                         + RE / TSimulationSettings.WATER_SPECIFIC_HEAT / V 
                         + ( _appSettings.SimSettings.EnviromentTemp - T ) * CoolingSurface(V) * _appSettings.SimSettings.HeatTransferCoeffienctSteelAir / TSimulationSettings.WATER_SPECIFIC_HEAT / V 
                         + act_cool * _appSettings.SimSettings.CoolantFlow * TSimulationSettings.WATER_SPECIFIC_HEAT * ( _appSettings.SimSettings.CoolantTemp - T ) / TSimulationSettings.WATER_SPECIFIC_HEAT / V;
            
            double dV = _actualFeedRate;

            return new double[] { dX, dG, dE, dO2, dT, dV };
        }

        #region ODE Solvers

        /// <summary>
        /// Same as ode45 solver but only returns last element and no time or x values (since there would only be one)
        /// </summary>
        /// <param name="model">A Delegate or Func object containing the model to solve. It must accept a double(x) and a double array(y) and return a double array(dy)</param>
        /// <param name="initialValues">The initial values that should be used when solving the ODE</param>
        /// <param name="xEnd">End x value (or time), default = 100</param>
        /// <param name="tol"></param>
        /// <param name="xStart">Start x value (or time), default = 0</param>
        /// <returns>An arry containing the state at the last point in time (or x)</returns>
        private (double, double[]) RK45SolverLast(Func<double, double[], double[]> model, double[] initialValues, double xEnd = 100, double tol = 1e-6, double xStart = 0) {
            (List<double> X, List<double[]> Y) = RK45Solver(model, initialValues, xEnd, tol, xStart);
            return (X.Last(), Y.Last().Select(p => ( p < 0 ) ? 0 : p).ToArray());
        }

        /// <summary>
        /// Variable step size ODE solver based on Dormand-Prince pair (RK4 and RK4) to determine step size. Fast solver suitable for almost all models.
        /// </summary>
        /// <param name="model">A Delegate or Func object containing the model to solve. It must accept a double(x) and a double array(y) and return a double array(dy)</param>
        /// <param name="initialValues">The initial values that should be used when solving the ODE</param>
        /// <param name="xStart">Start x value (or time), default = 0</param>
        /// <param name="xEnd">End x value (or time), default = 100</param>
        /// <param name="tol">The tolerance which is used to increas or deacrese the step size</param>
        /// <returns>A tuple of two arrays, the first containing the x values and the second containint arrays of y values (one for each x value)</returns>
        private (List<double>, List<double[]>) RK45Solver(Func<double, double[], double[]> model, double[] initialValues, double xEnd = 100, double tol = 1e-6, double xStart = 0) {
            List<double> xValues = new();
            List<double[]> yValues = new();

            double[] y = (double[]) initialValues.Clone();
            double dx = 0.1;  // Initial step size in seconds
            bool errorTooLarge = false;
            double x = xStart;
            while (x < xEnd) {
                if (!errorTooLarge) {
                    xValues.Add(x);
                    yValues.Add((double[]) y.Clone());
                }

                double[] k1 = model(x, y);
                double[] k2 = model(x + 0.25 * dx, AddArrays(y, ScaleArray(k1, 0.25 * dx)));
                double[] k3 = model(x + 3.0 / 8.0 * dx, AddArrays(y, AddArrays(ScaleArray(k1, 3.0 / 32.0 * dx), ScaleArray(k2, 9.0 / 32.0 * dx))));
                double[] k4 = model(x + 12.0 / 13.0 * dx, AddArrays(y, AddArrays(AddArrays(ScaleArray(k1, 1932.0 / 2197.0 * dx), ScaleArray(k2, -7200.0 / 2197.0 * dx)), ScaleArray(k3, 7296.0 / 2197.0 * dx))));
                double[] k5 = model(x + dx, AddArrays(y, AddArrays(AddArrays(ScaleArray(k1, 439.0 / 216.0 * dx), ScaleArray(k2, -8.0 * dx)), AddArrays(ScaleArray(k3, 3680.0 / 513.0 * dx), ScaleArray(k4, -845.0 / 4104.0 * dx)))));
                double[] k6 = model(x + 0.5 * dx, AddArrays(y, AddArrays(AddArrays(ScaleArray(k1, -8.0 / 27.0 * dx), ScaleArray(k2, 2.0 * dx)), AddArrays(ScaleArray(k3, -3544.0 / 2565.0 * dx), AddArrays(ScaleArray(k4, 1859.0 / 4104.0 * dx), ScaleArray(k5, -11.0 / 40.0 * dx))))));

                double[] y_new = new double[y.Length];
                double[] y_new_star = new double[y.Length];
                for (int i = 0; i < y.Length; i++) {
                    y_new[i] = y[i] + ( 25.0 / 216.0 * k1[i] + 1408.0 / 2565.0 * k3[i] + 2197.0 / 4104.0 * k4[i] - 0.2 * k5[i] ) * dx;
                    y_new_star[i] = y[i] + ( 16.0 / 135.0 * k1[i] + 6656.0 / 12825.0 * k3[i] + 28561.0 / 56430.0 * k4[i] - 9.0 / 50.0 * k5[i] + 2.0 / 55.0 * k6[i] ) * dx;
                }

                double error = 0;
                for (int i = 0; i < y.Length; i++) {
                    error += Math.Pow(y_new_star[i] - y_new[i], 2);
                }
                error = Math.Sqrt(error);

                errorTooLarge = true;
                if (error <= tol) {
                    y = y_new;
                    _actualFeedRate += _actualFeedRate * _appSettings.ReactorControlSettings.FeedControlSettings.FeedRateIncrement * dx/3600;
                    x += dx;
                    errorTooLarge = false;
                }

                dx *= Math.Pow(tol / ( error + 1e-10 ), 0.25);
            }
            x = xValues.Last();
            y = yValues.Last();
            if (x < xEnd) {
                dx = xEnd - x;
                _actualFeedRate += _actualFeedRate * _appSettings.ReactorControlSettings.FeedControlSettings.FeedRateIncrement * dx / 3600;
                double[] k1 = model(x, y);
                double[] k2 = model(x + 0.25 * dx, AddArrays(y, ScaleArray(k1, 0.25 * dx)));
                double[] k3 = model(x + 3.0 / 8.0 * dx, AddArrays(y, AddArrays(ScaleArray(k1, 3.0 / 32.0 * dx), ScaleArray(k2, 9.0 / 32.0 * dx))));
                double[] k4 = model(x + 12.0 / 13.0 * dx, AddArrays(y, AddArrays(AddArrays(ScaleArray(k1, 1932.0 / 2197.0 * dx), ScaleArray(k2, -7200.0 / 2197.0 * dx)), ScaleArray(k3, 7296.0 / 2197.0 * dx))));
                double[] k5 = model(x + dx, AddArrays(y, AddArrays(AddArrays(ScaleArray(k1, 439.0 / 216.0 * dx), ScaleArray(k2, -8.0 * dx)), AddArrays(ScaleArray(k3, 3680.0 / 513.0 * dx), ScaleArray(k4, -845.0 / 4104.0 * dx)))));
                
                double[] y_new = new double[y.Length];

                for (int i = 0; i < y.Length; i++) {
                    y_new[i] = y[i] + ( 25.0 / 216.0 * k1[i] + 1408.0 / 2565.0 * k3[i] + 2197.0 / 4104.0 * k4[i] - 0.2 * k5[i] ) * dx;
                    
                }
                xValues.Add(xEnd);
                yValues.Add((double[]) y_new.Clone());
            }
            return (xValues, yValues);
        }

        private static double[] AddArrays(double[] a, double[] b) {
            return a.Length != b.Length
                ? throw new ArgumentException("Arrays must be of the same length")
                : a.Zip(b, (av, bv) => av + bv).ToArray();
        }

        private static double[] ScaleArray(double[] a, double factor) {
            return a.Select((v) => v * factor).ToArray(); 
        }
        #endregion
    }

}
