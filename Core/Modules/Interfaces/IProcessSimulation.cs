using System;

namespace Core.Modules.Interfaces
{
    public interface IProcessSimulation : IDisposable
    {
        event Action<string, int> LogEvent;
        event Action<string> StartupEvent;
        void SimulateUntil(DateTime time);

        public void Begin();
    }
}