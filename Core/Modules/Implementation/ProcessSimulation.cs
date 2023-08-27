using System.Globalization;
using Core.Modules.Interfaces;

namespace Core.Modules
{
    internal class ProcessSimulation : IProcessSimulation, IDisposable {
        public event Action<string, int>? LogEvent;
        public event Action<string>? StartupEvent;

        public void SimulateUntil(DateTime time) {

        }

        public ProcessSimulation() { 
        
        }


        public void Dispose() {
            LogEvent?.Invoke("SimModule stopped!", 1);
        }

        public void Begin() {
            LogEvent?.Invoke("SimModule started!", 1);


            double[] initValues = { 20, 0, 1 };
            double[] parameters = { 0.2, 0.058, 0.167, 0.5, 0.33 };

            double[] X;
            double[][] Y;

            (X, Y) = EulerSolver((x, y) => SimModel(x, y, parameters), initValues, xEnd: 24, dx: 0.1);
            SaveToCSV(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "result1.csv"), X, Y);

            (X, Y) = RK4Solver((x, y) => SimModel(x, y, parameters), initValues, xEnd: 24, dx: 0.1);
            SaveToCSV(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "result2.csv"), X, Y);

            (X, Y) = RK45Solver((x, y) => SimModel(x, y, parameters), initValues, xEnd: 24);
            SaveToCSV(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "result3.csv"), X, Y);
        }

        private static void SaveToCSV(string filename, double[] xValues, double[][] yValues) {
            if (xValues.Length != yValues.Length) {
                throw new ArgumentException("The xValues and yValues lengths must match.");
            }

            using StreamWriter writer = new(filename);
            // Write the header
            writer.Write("x;");
            for (int i = 0; i < yValues[0].Length; i++) {
                writer.Write($"y_{i}");
                if (i != yValues[0].Length - 1) {
                    writer.Write(";");
                }
            }
            writer.WriteLine();

            // Write the data
            for (int i = 0; i < xValues.Length; i++) {
                writer.Write(xValues[i].ToString(CultureInfo.InvariantCulture) + ";");
                for (int j = 0; j < yValues[i].Length; j++) {
                    writer.Write(yValues[i][j].ToString(CultureInfo.InvariantCulture));
                    if (j != yValues[i].Length - 1) {
                        writer.Write(";");
                    }
                }
                writer.WriteLine();
            }
        }



        private static double[] SimModel(double _, double[] y, double[] parameters) {
            double[] dy = new double[3];
            if (dy.Length != y.Length) {
                throw new ArgumentException($"Lenght of y must be {dy.Length}! It is {y.Length}...");
            }

            if (parameters.Length != 5) {
                throw new ArgumentException($"Lenght of parameters must be 5! It is {parameters.Length}...");
            }
            double mumax1 = parameters[0];
            double mumax2 = parameters[1];
            double yieldGX = parameters[2];
            double yieldGE = parameters[3];
            double yieldEX = parameters[4];

            double G = y[0];
            double E = y[1];
            double X = y[2];

            double m1 = mumax1 * G / ( G + 0.5 );
            double m2 = mumax2 * E / ( E + 0.5 ) * ( ( mumax1 - m1 ) / mumax1 );

            dy[0] = - X * m1 / yieldGX;
            dy[1] = + X * m1 / yieldGE - X * m2 / yieldEX;
            dy[2] = + X * m1           + X * m2;

            return dy;
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
        private static (double[], double[][]) EulerSolver(Func<double, double[], double[]> model, double[] initalValues, double xEnd = 100, double dx = 0.1, double xStart = 0 ) {
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
            return (xValues.ToArray(), yValues.ToArray());
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
        private static (double[], double[][]) RK4Solver(Func<double, double[], double[]> model, double[] initialValues, double xEnd = 100, double dx = 0.1, double xStart = 0) {
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

            return (xValues.ToArray(), yValues.ToArray());
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
        private static (double[], double[][]) RK45Solver(Func<double, double[], double[]> model, double[] initialValues, double xEnd = 100, double tol = 1e-6, double xStart = 0) {
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

            return (xValues.ToArray(), yValues.ToArray());
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
