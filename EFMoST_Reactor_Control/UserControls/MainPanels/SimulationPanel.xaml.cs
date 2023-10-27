using ScottPlot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using ViewModel;

namespace UI.UserControls.MainPanels {
    /// <summary>
    /// Interaktionslogik für SimulationPanel.xaml
    /// </summary>
    public partial class SimulationPanel : UserControl {
        MainViewModel? viewModel;

        public SimulationPanel() {
            InitializeComponent();
            DataContextChanged += SimulationPanel_DataContextChanged;
        }

        public void Dispose() {
            DataContextChanged -= SimulationPanel_DataContextChanged;
            if (viewModel == null || viewModel.Simulation == null) { return; }
            viewModel.Simulation.SimUpdate -= Simulation_SimUpdate;
        }

        private void SimulationPanel_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e) {
            viewModel = DataContext as MainViewModel;
            if (viewModel == null || viewModel.Simulation == null) { return; }
            viewModel.Simulation.SimUpdate += Simulation_SimUpdate;
        }

        private void Simulation_SimUpdate() {
            try {
                UpdatePlots();
                UpdateText();
            } catch { }
        }

        private double dt = 0, t = 0;

        private void UpdatePlots() {
            try {

                if (viewModel == null || viewModel.Simulation == null) { return; }
                double[] xvals = viewModel.Simulation.Time;

                if (xvals.Length >= 2) {
                    dt = xvals[xvals.Length - 1] - xvals[xvals.Length - 2];
                    t = xvals[xvals.Length - 1];
                }

                if (xvals.Length == 0) { return; }
                double[] xvalsP = viewModel.Simulation.TimeFuture;
                xvals = xvals.Select(v => v / 3600).ToArray();
                xvalsP = xvalsP.Select(v => v / 3600).ToArray();

                List<double[]> yvals = new(viewModel.Simulation.State);
                List<double[]> yvalsP = new(viewModel.Simulation.StateFuture);

                XYPlot1.Plot.Clear();
                XYPlot1.Plot.AddScatter(xvals, yvals[0], label: viewModel.Simulation.Names[0], color: Color.Red);
                XYPlot1.Plot.AddScatter(xvals, yvals[3], label: viewModel.Simulation.Names[3], color: Color.Black);
                XYPlot1.Plot.AddScatter(xvalsP, yvalsP[0], color: Color.Red, markerShape: MarkerShape.none);
                XYPlot1.Plot.AddScatter(xvalsP, yvalsP[3], color: Color.Black, markerShape: MarkerShape.none);
                XYPlot1.Plot.Legend(location: Alignment.LowerCenter);
                XYPlot1.Plot.XAxis.Label("Zeit /h");
                XYPlot1.Plot.YAxis.Label("Konzentration");

                XYPlot2.Plot.Clear();
                XYPlot2.Plot.AddScatter(xvals, yvals[1], label: viewModel.Simulation.Names[1], color: Color.Red);
                XYPlot2.Plot.AddScatter(xvals, yvals[2], label: viewModel.Simulation.Names[2], color: Color.Black);
                XYPlot2.Plot.AddScatter(xvalsP, yvalsP[1], color: Color.Red, markerShape: MarkerShape.none);
                XYPlot2.Plot.AddScatter(xvalsP, yvalsP[2], color: Color.Black, markerShape: MarkerShape.none);
                XYPlot2.Plot.Legend(location: Alignment.LowerCenter);
                XYPlot2.Plot.XAxis.Label("Zeit /h");
                XYPlot2.Plot.YAxis.Label("Konzentration");

                XYPlot3.Plot.Clear();
                XYPlot3.Plot.AddScatter(xvals, yvals[4], label: viewModel.Simulation.Names[4], color: Color.Red);
                XYPlot3.Plot.AddScatter(xvalsP, yvalsP[4], color: Color.Red, markerShape: MarkerShape.none);

                XYPlot3.Plot.XAxis.Label("Zeit /h");
                XYPlot3.Plot.YAxis.Label("Temperatur °C");

                XYPlot1.Configuration.Pan = false;
                XYPlot1.Configuration.Zoom = false;

                XYPlot2.Configuration.Pan = false;
                XYPlot2.Configuration.Zoom = false;

                XYPlot3.Configuration.Pan = false;
                XYPlot3.Configuration.Zoom = false;


                Dispatcher.Invoke(() => {
                    XYPlot1.Refresh();
                    XYPlot2.Refresh();
                    XYPlot3.Refresh();
                });
            } catch (Exception ex) {
                Console.WriteLine(ex);
            }
        }

        private void Reset_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            Task.Run(() => Console.Beep(1800, 50));
            viewModel?.Simulation?.Reset();
        }

        private void UpdateText() {
            try {
                Dispatcher.Invoke(() => {
                    labelDt.Text = $"dt = {( (viewModel?.Simulation?.DeltaTime ?? 0) * 1000 ):F0} ms";
                    labelTotalt.Text = $"total t = {t:F0} s";

                    labelFeedG.Text = $"Feed Gesamtzucker = {( (viewModel?.Simulation?.TotalFeedVolume ?? 0 ) * 800 ):F0} g";
                    labelFeedV.Text = $"Feed Gesamtvolumen = {(viewModel?.Simulation?.TotalFeedVolume ?? 0):F1} L";
                });
            } catch { }
        }
    }
}
