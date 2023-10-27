using Core.DataTypes;
using System;

namespace Core.Modules.Interfaces
{
    public interface IReactorControl : IDisposable
    {
        event Action<string, int> LogEvent;
        event Action<string> StartupEvent;
        event Action ControlCycleFinished;
        bool IsFeedAutomationActive { get; }
        bool IsVentilationAutomationActive { get; }
        bool IsTemperatureAutomationActive { get; }
        bool IsMixingAutomationActive { get; }

        double CalculatedFeedRateValue { get; }
        double CalculatedVentilationValue { get; }
        double CalculatedRPMValue { get; }
        bool CalculatedTempValveSetting { get; }
        double CalculatedTempControlSeeting { get; }

        double[] PidParametersTemperature { get; }
        double[] PidParametersVentilation { get; }

        void SetSimlation(IProcessSimulation sim);
        void Begin();

        //outside communication
        void SetFeedRate(double value);

        void ResetTempPID();

        void ResetVentillationPID();
    }
}