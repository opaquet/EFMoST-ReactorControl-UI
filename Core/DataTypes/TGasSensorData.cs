namespace Core.DataTypes {

    /// <summary>
    /// Dataclass <b>GasSensorDataType</b> is supposed to hold all state values sent by the gas sensor
    /// </summary>
    public class TGasSensorData : ICloneable{
        public const int VALUES_SIZE = 9;
        public const int ENVIROMENT_SIZE = 3;

        public int ID { get; }
        public DateTime TimeStamp { get; }
        public double[] Values { get; }
        public double[] Enviroment { get; }
        public byte ValveSetting { get; }
        public int MeasurementCycle { get; }
        public byte SensorState { get; }
        public int SensorStateTime { get; }
        public int SensorTime { get; }
        public TGasSensorData(int? id = 0, double[]? values = null, double[]? enviroment = null, byte? valvesetting = 0, int? measurementcycle = 0, byte? sensorstate = 0, int? sensorstatetime = 0, int? sensortime = 0) {
            TimeStamp = DateTime.Now;
            ID = id ?? 0;
            Values = values ?? new double[VALUES_SIZE];
            Enviroment = enviroment ?? new double[ENVIROMENT_SIZE];
            ValveSetting = valvesetting ?? 0;
            MeasurementCycle = measurementcycle ?? 0;
            SensorState = sensorstate ?? 0;
            SensorStateTime = sensorstatetime ?? 0;
            SensorTime = sensortime ?? 0;
        }

        public object Clone() {
            return new TGasSensorData(ID, (double[])Values.Clone(), (double[])Enviroment.Clone(), ValveSetting, MeasurementCycle, SensorState, SensorStateTime, SensorTime);
        }

    }
}
