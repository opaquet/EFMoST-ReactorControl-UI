using ScottPlot;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ViewModel;

namespace UI.UserControls {
    /// <summary>
    /// Interaktionslogik für XYGraph.xaml
    /// </summary>

    public partial class XYGraph : UserControl {
        MainViewModel? viewModel;
        public int SelectedValue { get; set; } = 0;

        public XYGraph() {
            InitializeComponent();
            DataContextChanged += GraphXY_DataContextChanged;
        }

        public void Dispose() {
            DataContextChanged -= GraphXY_DataContextChanged;
            if (viewModel == null) { return; }
            viewModel.PropertyChanged -= DC_PropertyChanged;
        }

        private void GraphXY_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e) {
            viewModel = DataContext as MainViewModel;
            if (viewModel == null) { return; }
            viewModel.PropertyChanged += DC_PropertyChanged;
        }

        private void DC_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
            if (viewModel == null) { return; }
            Dispatcher?.Invoke(() => Update(false));
        }

        public void Update(bool skipCheck) {
            if (viewModel == null) { return; }
            double[] xvals = viewModel.Chart.ValuesCollection[SelectedValue].XValues;
            int punkteAnzahl = 0;
            var plottables = XYPlot.Plot.GetPlottables();
            if (plottables.Any()) {
                var scatter = plottables.OfType<ScottPlot.Plottable.ScatterPlot>().FirstOrDefault();
                if (scatter != null) {
                    punkteAnzahl = scatter.Xs.Length;
                }
            }

            if (!skipCheck && punkteAnzahl == xvals.Length) {
                return;
            }

            double[] yvals = viewModel.Chart.ValuesCollection[SelectedValue].YValues;

            XYPlot.Plot.Clear();
            if (xvals.Length > 0) {
                XYPlot.Plot.AddScatter(xvals, yvals, label: "Messwert");
                if (SelectedValue < 6) {
                    XYPlot.Plot.AddScatter(viewModel.Chart.SetpointsCollection[SelectedValue].XValues, viewModel.Chart.SetpointsCollection[SelectedValue].YValues, label: "Setpoint");
                }
            }
            XYPlot.Plot.Legend(location: Alignment.UpperLeft);
            XYPlot.Plot.XAxis.Label(viewModel.Chart.ValuesCollection[SelectedValue].XLabel);
            XYPlot.Plot.YAxis.Label(viewModel.Chart.ValuesCollection[SelectedValue].YLabel);
            XYPlot.Plot.Title(viewModel.Chart.ValuesCollection[SelectedValue].Name);
            XYPlot.Plot.SetAxisLimitsY(viewModel.Chart.ValuesCollection[SelectedValue].MinValue, viewModel.Chart.ValuesCollection[SelectedValue].MaxValue);
            XYPlot.Refresh();
        }
    }

}
