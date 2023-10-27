using System;

namespace Core.Modules.Interfaces
{
    public interface IProcessSimulation : IDisposable
    {
        event Action<string, int> LogEvent;
        event Action<string> StartupEvent;
        event Action SimulationStepFinished;


        IReadOnlyList<double> SimTimeValues { get; }
        IReadOnlyList<double> SimTimePredictions { get; }
        IReadOnlyList<double[]> SimStateValues { get; }
        IReadOnlyList<double[]> SimStatePredictions { get; }

        double Biomass { get; set; }
        double Sugar { get; set; }
        double Ethanol { get; set; }
        double Oxygen { get; set; }
        double Temperature { get; set; }
        double Volume { get; set; }
        double DeltaTime { get; }
        double TotalFeedVolume { get; }
        void Begin();
        void Reset();
    }
}