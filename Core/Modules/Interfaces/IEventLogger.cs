using System;

namespace Core.Modules.Interfaces
{
    public interface IEventLogger : IDisposable
    {
        event Action LogUpdated;
        int Count { get; }
        int MaxLength { get; set; }
        void Log(string Msg, string Label = "", int LogLevel = 0);
        void Append(string Msg, string Label = "", int LogLevel = 0);
        void Clear();
        string LogString(int amount = 50, int MaxLogLevel = 0);
    }
}

