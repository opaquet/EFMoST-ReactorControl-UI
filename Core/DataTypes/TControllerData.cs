using System;

namespace Core.DataTypes {
    /// <summary>
    /// Dataclass <b>ControllerDataType</b> is supposed to hold all state values sent by the controller
    /// </summary>
    public class TControllerData : ICloneable{
        public const int VALUES_SIZE = 18;
        public const int SETPOINTS_SIZE = 6;
        public const int AUTOMATIC_SIZE = 6;
        public const int ACTIVE_SIZE = 7;
        public const int ALARM_SIZE = 6;
        public const int DIGITAL_OUT_SIZE = 12;
        public const int ANALOG_OUT_SIZE = 4;

        [Flags]
        public enum ControllerErrors : ushort {
            None = 0,
            SerialOneInputBufferOverflow = 1 << 0,
            MeasurementValueCountTooLarge = 1 << 1,
            MeasurementValuesInvalidOrOutdated = 1 << 2,
            MeasurementIDInvalidOrUnkown = 1 << 3,
            InvalidBaudRateSelected = 1 << 4,
            StateTransitionError = 1 << 5,
            SeriaZeroInputBufferOverflow = 1 << 6,
            DisplayValueConversionError = 1 << 10
        }

        public DateTime TimeStamp { get; }
        public double[] Values { get; }
        public double[] Setpoints { get; }
        public bool[] Automatic { get; }
        public bool[] Active { get; }
        public bool[] Alarm { get; }
        public bool[] DigitalOut { get; }
        public ushort[] AnalogOut { get; }
        public bool DirectControl { get; }
        /// <summary>
        /// Device error code: each of the 16 bits indicates one possible error or warnig raised by the controller
        /// </summary>
        public ushort ErrorCode { get; }

        public TControllerData( double[]? values = null,
                                double[]? setpoints = null,
                                bool[]? automatic = null,
                                bool[]? active = null,
                                bool[]? alarm = null,
                                bool[]? digitalout = null,
                                ushort[]? analogout = null,
                                bool directcontrol = false,
                                ushort errorcode = ushort.MaxValue) {
            TimeStamp = DateTime.Now;
            Values = values ?? new double[VALUES_SIZE];
            Setpoints = setpoints ?? new double[SETPOINTS_SIZE];
            Automatic = automatic ?? new bool[AUTOMATIC_SIZE];
            Active = active ?? new bool[ACTIVE_SIZE];
            Alarm = alarm ?? new bool[ALARM_SIZE];
            AnalogOut = analogout ?? new ushort[ANALOG_OUT_SIZE];
            DigitalOut = digitalout ?? new bool[DIGITAL_OUT_SIZE];
            DirectControl = directcontrol;
            ErrorCode = errorcode;
        }

        public object Clone() {
            return new TControllerData((double[])Values.Clone(),
                                        (double[])Setpoints.Clone(),
                                        (bool[])Automatic.Clone(),
                                        (bool[])Active.Clone(),
                                        (bool[])Alarm.Clone(),
                                        (bool[])DigitalOut.Clone(),
                                        (ushort[])AnalogOut.Clone(),
                                        DirectControl,
                                        ErrorCode);
        }
    }
}
