
using Core.Modules.Interfaces;

namespace ViewModel.ViewModels  {
    public class SimulationViewModel : BaseViewModel {

        private IProcessSimulation _processSim;

        public double[] Time { get => reduce(_processSim.SimTime); }
        public double[] TimeFuture { get => reduce(_processSim.SimTimeFuture); }

        public double[] Biomass { get => reduce(_processSim.SimBiomass); }
        public double[] BiomassFuture { get => reduce(_processSim.SimBiomassFuture); }

        public double[] Sugar { get => reduce(_processSim.SimGlucose); }
        public double[] SugarFuture { get => reduce(_processSim.SimGlucoseFuture); }

        public double[] Ethanol { get => reduce(_processSim.SimEthanol); }
        public double[] EthanolFuture { get => reduce(_processSim.SimEthanolFuture); }

        private double[] reduce(double[] vals) {
            if (vals.Length <= 2000) {
                return vals;
            } else {
                double schrittweite = (double) vals.Length / 2000;
                return Enumerable.Range(0, 2000).Select(i => vals[(int) ( i * schrittweite )]).ToArray();
            }
        }


        public SimulationViewModel(IProcessSimulation procSim) {
            _processSim = procSim;

        }
    }
}
