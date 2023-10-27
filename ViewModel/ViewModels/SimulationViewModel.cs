
using Core.Modules.Interfaces;

namespace ViewModel.ViewModels  {
    public class SimulationViewModel : BaseViewModel {

        public string[] Names { get; } = new string[] { "Biomasse", "Zucker", "Ethanol", "gel. Sauerstoff", "Temperatur", "Füllstand"};

        public event Action? SimUpdate;

        private IProcessSimulation _processSim;

        public double DeltaTime { get => _processSim.DeltaTime; }

        public double[] Time { get => Reduce(_processSim.SimTimeValues); }
        public double[] TimeFuture { get => Reduce(_processSim.SimTimePredictions); }

        public double TotalFeedVolume { get => _processSim.TotalFeedVolume; }

        public void Reset() {
            _processSim.Reset();
        }

        public List<double[]> State {
            get {
                double[][] allValues = _processSim.SimStateValues.ToArray();
                return new List<double[]> {
                    Reduce(allValues.Select(value => value[0]).ToList()),
                    Reduce(allValues.Select(value => value[1]).ToList()),
                    Reduce(allValues.Select(value => value[2]).ToList()),
                    Reduce(allValues.Select(value => value[3] * 1000).ToList()),
                    Reduce(allValues.Select(value => value[4] - 273.15).ToList()),
                    Reduce(allValues.Select(value => value[5]).ToList())
                };
            }
        }
        public List<double[]> StateFuture {
            get {
                double[][] allValues = _processSim.SimStatePredictions.ToArray();
                return new List<double[]> {
                    Reduce(allValues.Select(value => value[0]).ToList()),
                    Reduce(allValues.Select(value => value[1]).ToList()),
                    Reduce(allValues.Select(value => value[2]).ToList()),
                    Reduce(allValues.Select(value => value[3] * 1000).ToList()),
                    Reduce(allValues.Select(value => value[4] - 273.15).ToList()),
                    Reduce(allValues.Select(value => value[5]).ToList())
                };
            }
        }

        public ValueViewModel Biomass { get; private set; }
        public ValueViewModel Sugar { get; private set; }
        public ValueViewModel Ethanol { get; private set; }
        public ValueViewModel Oxygen { get; private set; }
        public ValueViewModel Temperature { get; private set; }
        public ValueViewModel Volume { get; private set; }


        private T[] Reduce<T>(IReadOnlyList<T> vals) {
            if (vals.Count <= 2000) {
                return vals.ToArray();
            } else {
                double schrittweite = (double) vals.Count / 2000;
                return Enumerable.Range(0, 2000).Select(i => vals[(int) ( i * schrittweite )]).ToArray();
            }
        }


        public SimulationViewModel(IProcessSimulation procSim, List<ValueViewModel> valueViewModels) {
            _processSim = procSim;
            _processSim.SimulationStepFinished += () => SimUpdate?.Invoke();
            Biomass = valueViewModels[0];
            Sugar = valueViewModels[1];
            Ethanol = valueViewModels[2];
            Oxygen = valueViewModels[3];
            Temperature = valueViewModels[4];
            Volume = valueViewModels[5];
        }
    }
}
