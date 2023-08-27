namespace Core.DataTypes {
    /// <summary>
    /// Names of the elements/variables in the Controller Values Array
    /// </summary>
    public static class MeasurementValue {
        public const int Flüllstand = 0;
        public const int Drehzahl = 1;
        public const int Belüftung = 2;
        public const int Feedrate = 3;
        public const int Temperatur = 4;
        public const int Konzentrierung = 5;
        public const int pH = 6;
        public const int gelSauerstoff = 7;
        public const int Sauerstoff = 8;
        public const int Temp1 = 9;
        public const int Temp2 = 10;
        public const int Temp3 = 11;
        public const int DruckDeckel = 12;
        public const int DruckBoden = 13;
        public const int Ethanol = 14;
        public const int H2S = 15;
        public const int GasSensor = 16;
        public const int frei = 17;
    }

    public static class ControlVariable {
        public const int Level = 0;
        public const int Rotation = 1;
        public const int Ventilation = 2;
        public const int Feed = 3;
        public const int Temperature = 4;
        public const int Concentration = 5;
    }

    /// <summary>
    /// Names of the elements/variables in the Controller Setpoint Array
    /// </summary>
    public static class SetpointValue {
        public const int Flüllstand = 0;
        public const int Drehzahl = 1;
        public const int Belüftung = 2;
        public const int Feedrate = 3;
        public const int Temperatur = 4;
        public const int Konzentrierung = 5;
    }

    /// <summary>
    /// Names of the elements/variables in the GesSensor Enviroment Array
    /// </summary>
    public static class EnviromentValue {
        public const int Temperatur = 0;
        public const int Luftfeuchte = 1;
        public const int Druck = 2;
    }
}
