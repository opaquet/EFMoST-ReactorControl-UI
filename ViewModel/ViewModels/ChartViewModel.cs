using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ViewModel.ViewModels {
    public class MeasurementPoint {
        public double X { get; set; }
        public double Y { get; set; }
    }

    public class MeasurementSeries : BaseViewModel, INotifyPropertyChanged {
        private readonly object _lockObject = new object();

        public string Name { get; set; } = string.Empty;
        public string XLabel { get; set; } = "x";
        public string YLabel { get; set; } = "y";
        public double MaxValue { get; set; } = 100;
        public double MinValue { get; set; } = 0;

        public ObservableCollection<MeasurementPoint> Points { get; private set; } = new ObservableCollection<MeasurementPoint>();
        public void AddPoint(MeasurementPoint point) {
            lock (_lockObject) {
                Points.Add(point);
            }
        }
        public int Count { get => Points.Count; }
        public double[] XValues {
            get {
                if (Count <= 2000) {
                    lock (_lockObject) {
                        return Points.Select(obj => obj.X).ToArray();
                    }
                } else {
                    double schrittweite = (double)Count / 2000;
                    lock (_lockObject) {
                        return Enumerable.Range(0, 2000).Select(i => Points[(int)(i * schrittweite)].X).ToArray();
                    }
                }
            }
        }
        public double[] YValues {
            get {
                if (Count <= 2000) {
                    lock (_lockObject) {
                        return Points.Select(obj => obj.Y).ToArray();
                    }
                } else {
                    double schrittweite = (double)Count / 2000;
                    lock (_lockObject) {
                        return Enumerable.Range(0, 2000).Select(i => Points[(int)(i * schrittweite)].Y).ToArray();
                    }
                }
            }
        }
        public void Clear() {
            Points.Clear();
        }
    }

    public class ChartViewModel : BaseViewModel, INotifyPropertyChanged {
        public List<MeasurementSeries> ValuesCollection { get; set; } = new List<MeasurementSeries>();
        public List<MeasurementSeries> SetpointsCollection { get; set; } = new List<MeasurementSeries>();
        public void AddSeries(string name, string xAxisLabel, string yAxisLabel, double minValue, double maxValue) => ValuesCollection.Add(new MeasurementSeries { Name = name, XLabel = xAxisLabel, YLabel = yAxisLabel, MinValue = minValue, MaxValue = maxValue });
        public void AddSetPointSeries(string name, string xAxisLabel, string yAxisLabel) => SetpointsCollection.Add(new MeasurementSeries { Name = name, XLabel = xAxisLabel, YLabel = yAxisLabel });
        public void AddPoint(MeasurementPoint point, int seriesIndex) => ValuesCollection[seriesIndex].AddPoint(point);
        public void AddSetPoint(MeasurementPoint point, int seriesIndex) => SetpointsCollection[seriesIndex].AddPoint(point);
        public void ClearPoints(int seriesIndex) => ValuesCollection[seriesIndex].Clear();
        public void ClearSetPoints(int seriesIndex) => SetpointsCollection[seriesIndex].Clear();
    }
}
