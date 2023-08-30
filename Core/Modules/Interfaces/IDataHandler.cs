using System;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Core.DataTypes;

namespace Core.Modules.Interfaces
{
    public interface IDataHandler : IDisposable
    {
        event Action<TComputedValuesData> GasSensorCycleComplete;
        event Action<TControllerData.ControllerErrors> ControllerErrorReport;
        event Action<string, int> LogEvent;
        event Action<string> StartupEvent;

        int GetComputedValuesCount { get; }
        int GetControllerDataCount { get; }
        int GetGasSensorDataCount { get; }

        bool AddRawMesurementData(string message);
        bool AddControllerData(JsonObject controllerJSON);
        bool AddGassensorData(JsonObject gasSensorJSON);
        double[] GetAllControllerSetpoints(int Index);
        double[] GetAllControllerValues(int Index);
        int[] GetAllGasSensorCycleNumbers();
        double[] GetAllGasSensorValues(int Index);
        double[] GetLastCycleGasSensorValues(int Index, int Cycle);
        double[] GetAllEthanolValues();
        double[] GetAllH2SValues();
        TComputedValuesData GetComputedValueDatum(int Index);
        TControllerData GetControllerDatum(int Index);
        TGasSensorData GetGasSensorDatum(int Index);
        TComputedValuesData GetLastComputedValueDatum();
        TControllerData GetLastControllerDatum();
        TGasSensorData GetLastGasSensorDatum();
        void Begin();
        void SaveAllDataJson(string filename);
        void SaveRelayDataCSV();
        void SaveGasSensorDataCSV();
        void SaveControllerDataCSV();
        void SaveComputedDataCSV();
    }
}