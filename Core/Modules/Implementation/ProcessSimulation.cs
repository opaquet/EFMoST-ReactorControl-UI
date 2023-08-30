using System;
using System.Globalization;
using System.Reflection;
using System.Runtime;
using System.Security.Cryptography;
using Core.DataTypes;
using Core.Modules.Interfaces;

namespace Core.Modules
{
    internal class ProcessSimulation : IProcessSimulation, IDisposable {

        public event Action<string, int>? LogEvent;
        public event Action<string>? StartupEvent;

        public event Action? SimulationStepFinished;

        private IAppSettings _appSettings;
        private IDataHandler _dataStore;


        private double _acualFeedRate = 0;
        private double[] _simStateValues = new double[6];

        private List<double> _allSimTimesUntilNow = new List<double>();
        private List<double> _FutureSimTimes2Hours = new List<double>();

        private List<double[]> _allSimValuesUntilNow = new List<double[]>();
        private List<double[]> _FutureSimValues2Hours = new List<double[]>();

        private Timer? _simIntervalTimer;

        private double _tNow = 0;
        private double _tLast = 0;
        private DateTime _startTime = DateTime.Now;

        public double[] SimTime { get => _allSimTimesUntilNow.ToArray(); }
        public double[] SimTimeFuture { get => _FutureSimTimes2Hours.ToArray(); }

        public double[] SimBiomass { get => _allSimValuesUntilNow.Select(value => value[0]).ToArray(); }
        public double[] SimBiomassFuture { get => _FutureSimValues2Hours.Select(value => value[0]).ToArray(); }

        public double[] SimGlucose { get => _allSimValuesUntilNow.Select(value => value[1]).ToArray(); }
        public double[] SimGlucoseFuture { get => _FutureSimValues2Hours.Select(value => value[1]).ToArray(); }

        public double[] SimEthanol { get => _allSimValuesUntilNow.Select(value => value[2]).ToArray(); }
        public double[] SimEthanolFuture { get => _FutureSimValues2Hours.Select(value => value[2]).ToArray(); }

        public double[] SimOxygen { get => _allSimValuesUntilNow.Select(value => value[3]).ToArray(); }
        public double[] SimOxygenFuture { get => _FutureSimValues2Hours.Select(value => value[3]).ToArray(); }

        public double[] SimTemperature { get => _allSimValuesUntilNow.Select(value => value[4]).ToArray(); }
        public double[] SimTemperatureFuture { get => _FutureSimValues2Hours.Select(value => value[4]).ToArray(); }

        public double[] SimVolume { get => _allSimValuesUntilNow.Select(value => value[4]).ToArray(); }
        public double[] SimVolumeFuture { get => _FutureSimValues2Hours.Select(value => value[4]).ToArray(); }

        public void SimulateUntil(DateTime time) {

        }

        public ProcessSimulation(IAppSettings settings, IDataHandler dataHandler ) { 
            _appSettings = settings;
            _dataStore = dataHandler;
        }


        public void Dispose() {
            LogEvent?.Invoke("SimModule stopped!", 1);
        }

        public void Begin() {
            LogEvent?.Invoke("SimModule started!", 1);

            // copy initial values from settings
            _simStateValues = new double[] { 
                _appSettings.SimSettings.StartBio,
                _appSettings.SimSettings.StartSugar,
                0,
                0,
                303,
                _appSettings.SimSettings.StartV
            };

            _simIntervalTimer?.Dispose();
            StartupEvent?.Invoke("starting controlIntervalTimer...");
            _simIntervalTimer = new Timer(SimTimerCallback, null, 0, Convert.ToInt32(_appSettings.SimSettings.SimDeltaTime*1000));
        }

        private void SimTimerCallback(object? state) {

            _acualFeedRate = _dataStore.GetLastControllerDatum().Setpoints[3] / 3600;
            SimStep();

        }



        private void SimStep() {

            _tNow = (DateTime.Now - _startTime).TotalSeconds;

            // solve from last step to now
            (List<double> X, List<double[]> Y) = RK45Solver( model: SimModel, 
                                                     initialValues: _simStateValues, 
                                                     xStart: _tLast, 
                                                     xEnd: _tNow, 
                                                     tol: 1e-10);

            _simStateValues = (double[])Y[Y.Count - 1].Clone();
            _allSimTimesUntilNow.AddRange(X);
            _allSimValuesUntilNow.AddRange(Y);

            // solve from now to 2 hours in the future
            (_FutureSimTimes2Hours, _FutureSimValues2Hours) = RK45Solver(model: SimModel,
                                         initialValues: _simStateValues,
                                         xStart: _tNow,
                                         xEnd: _tNow + 2 * 3600,
                                         tol: 1e-10);

            _tLast = _tNow;
            SimulationStepFinished?.Invoke();
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


        private double[] SimModel(double x, double[] y) {
            if (y.Length != 6) { throw new ArgumentException($"Lenght of y must be 6! It is {y.Length}..."); }

            double X  = y[0];
            double G  = y[1];
            double E  = y[2];
            double O2 = y[3];
            double T  = y[4];
            double V  = y[5];

            double Ar = Arrhenius(T) * Sigmoid(T, 273.15 + 39, .5);

            double m_ox = Monod(G, _appSettings.SimSettings.MueMaxOx) * Ar * Sigmoid(G, 0.05, 1000);
            double m_red = Monod(G, _appSettings.SimSettings.MueMaxRed) * Ar * ( 1 - Sigmoid(G, 0.05, 1000) );
            double m_eth = Monod(E, _appSettings.SimSettings.MueMaxEth) * Ar * ( 1 - Sigmoid(G, 0.05, 100) );

            double act_cool = 1 - Sigmoid(T, 30 + 273.15, 20);


            double RE = V * X * ( m_ox / _appSettings.SimSettings.YieldGOx * ( -_appSettings.SimSettings.ReactionHeatOx ) 
                                + m_red / _appSettings.SimSettings.YieldGRed * ( -_appSettings.SimSettings.ReactionHeatRed ) 
                                + ( m_ox + m_red + m_eth ) * ( -_appSettings.SimSettings.ReactionHeatBiomass ) );


            double dX = X * ( m_ox + m_red + m_eth ) - _acualFeedRate / V * X;
            double dG = -X * ( m_ox / _appSettings.SimSettings.YieldGOx + m_red / _appSettings.SimSettings.YieldGRed ) + _acualFeedRate / V * ( _appSettings.SimSettings.FeedSugarConcentration - G );
            double dE = X * ( m_red / _appSettings.SimSettings.YieldGRed - m_eth / _appSettings.SimSettings.YieldGRed ) - _acualFeedRate / V * E;
            double dO2 = -X * ( _appSettings.SimSettings.RequiredOxygenOx * m_ox + _appSettings.SimSettings.RequiredOxygenRed * m_red ) - _acualFeedRate / V * O2;
            double dT = _acualFeedRate / V * ( _appSettings.SimSettings.FeedTemp - T ) + RE / TSimulationSettings.WATER_SPECIFIC_HEAT / V + ( _appSettings.SimSettings.EnviromentTemp - T ) * CoolingSurface(V) * _appSettings.SimSettings.HeatTransferCoeffienctSteelAir / TSimulationSettings.WATER_SPECIFIC_HEAT / V + act_cool * _appSettings.SimSettings.CoolantFlow * TSimulationSettings.WATER_SPECIFIC_HEAT * ( _appSettings.SimSettings.CoolantTemp - T ) / TSimulationSettings.WATER_SPECIFIC_HEAT / V;
            double dV = _acualFeedRate;

            return new double[] { dX, dG, dE, dO2, dT, dV };
        }

        #region ODE Solvers
        /// <summary>
        /// Simple euler style ODE solver. Good enough for simple models if dx is small enough
        /// </summary>
        /// <param name="model">A Delegate or Func object containing the model to solve. It must accept a double(x) and a double array(y) and return a double array(dy)</param>
        /// <param name="initalValues">The initial values that should be used when solving the ODE</param>
        /// <param name="xStart">Start x value (or time), default = 0</param>
        /// <param name="xEnd">End x value (or time), default = 100</param>
        /// <param name="dx">The used delta x (the smaller, the better), default = 0.1</param>
        /// <returns>A tuple of two arrays, the first containing the x values and the second containint arrays of y values (one for each x value)</returns>
        private static (List<double>, List<double[]>) EulerSolver(Func<double, double[], double[]> model, double[] initalValues, double xEnd = 100, double dx = 0.1, double xStart = 0 ) {
            List<double> xValues = new();
            List<double[]> yValues = new();
            double[] dy;
            double[] y = (double[])initalValues.Clone();
            for (double x = xStart; x <= xEnd; x += dx) {
                xValues.Add(x);
                yValues.Add((double[]) y.Clone());

                dy = model(x, y);

                y = AddArrays(ScaleArray(dy, dx), y);

            }
            return (xValues, yValues);
        }

        /// <summary>
        /// Runge-Kutta (RK4) ODE solver. Good enough for most simple tasks
        /// </summary>
        /// <param name="model">A Delegate or Func object containing the model to solve. It must accept a double(x) and a double array(y) and return a double array(dy)</param>
        /// <param name="initalValues">The initial values that should be used when solving the ODE</param>
        /// <param name="xStart">Start x value (or time), default = 0</param>
        /// <param name="xEnd">End x value (or time), default = 100</param>
        /// <param name="dx">The used delta x (the smaller, the better), default = 0.1</param>
        /// <returns>A tuple of two arrays, the first containing the x values and the second containint arrays of y values (one for each x value)</returns>
        private static (List<double>, List<double[]>) RK4Solver(Func<double, double[], double[]> model, double[] initialValues, double xEnd = 100, double dx = 0.1, double xStart = 0) {
            List<double> xValues = new();
            List<double[]> yValues = new();

            double[] y = (double[]) initialValues.Clone();

            for (double x = xStart; x < xEnd; x += dx) {
                xValues.Add(x);
                yValues.Add((double[]) y.Clone());

                double[] k1 = model(x, y);
                double[] k2 = model(x + 0.5 * dx, AddArrays(y, ScaleArray(k1, 0.5 * dx)));
                double[] k3 = model(x + 0.5 * dx, AddArrays(y, ScaleArray(k2, 0.5 * dx)));
                double[] k4 = model(x + dx, AddArrays(y, ScaleArray(k3, dx)));

                for (int i = 0; i < y.Length; i++) {
                    y[i] += ( k1[i] + 2 * k2[i] + 2 * k3[i] + k4[i] ) * dx / 6.0;
                }
            }

            return (xValues, yValues);
        }


        /// <summary>
        /// Variable step size ODE solver based on Dormand-Prince pair (RK4 and RK4) to determine step size. Fast solver suitable for almost all models.
        /// </summary>
        /// <param name="model">A Delegate or Func object containing the model to solve. It must accept a double(x) and a double array(y) and return a double array(dy)</param>
        /// <param name="initalValues">The initial values that should be used when solving the ODE</param>
        /// <param name="xStart">Start x value (or time), default = 0</param>
        /// <param name="xEnd">End x value (or time), default = 100</param>
        /// <param name="dx">The used delta x (the smaller, the better), default = 0.1</param>
        /// <returns>A tuple of two arrays, the first containing the x values and the second containint arrays of y values (one for each x value)</returns>
        private static (List<double>, List<double[]>) RK45Solver(Func<double, double[], double[]> model, double[] initialValues, double xEnd = 100, double tol = 1e-6, double xStart = 0) {
            List<double> xValues = new();
            List<double[]> yValues = new();

            double[] y = (double[]) initialValues.Clone();
            double dx = 0.1;  // Initial step size
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
                    x += dx;
                    errorTooLarge = false;
                }

                dx *= Math.Pow(tol / ( error + 1e-10 ), 0.25);
            }

            return (xValues, yValues);
        }
        
        
        private static double[] AddArrays(double[] a, double[] b) {
            if (a.Length != b.Length) {
                throw new ArgumentException("Arrays must be of the same length");
            }

            double[] result = new double[a.Length];
            for (int i = 0; i < a.Length; i++) {
                result[i] = a[i] + b[i];
            }
            return result;
        }

        private static double[] ScaleArray(double[] a, double factor) {
            double[] result = new double[a.Length];
            for (int i = 0; i < a.Length; i++) {
                result[i] = a[i] * factor;
            }
            return result;
        }
        #endregion
    }

}
