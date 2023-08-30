using System;

namespace Core.Modules.Interfaces
{
    public interface IProcessSimulation : IDisposable
    {
        event Action<string, int> LogEvent;
        event Action<string> StartupEvent;
        event Action SimulationStepFinished;


        double[] SimTime { get; }
        double[] SimTimeFuture { get; }
        double[] SimBiomass { get; }
        double[] SimBiomassFuture { get; }
        double[] SimGlucose { get ; }
        double[] SimGlucoseFuture { get; }
        double[] SimEthanol { get; }
        double[] SimEthanolFuture { get; }
        double[] SimOxygen { get; }
        double[] SimOxygenFuture { get; }
        double[] SimTemperature { get; }
        double[] SimTemperatureFuture { get; }
        double[] SimVolume { get; }
        double[] SimVolumeFuture { get; }


        public void Begin();
    }
}