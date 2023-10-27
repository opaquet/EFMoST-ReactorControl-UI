namespace Core.DataTypes {
    public class TSimulationSettings {
        public const double GAS_CONSTANT = 8.314;
        public const double GLUCOSE_MOLAR_WEIGTH = 180.15;
        public const double WATER_SPECIFIC_HEAT = 4.18;

        public double ReactionHeatOx { get; set; } = -2805;
        public double ReactionHeatRed { get; set; } = -218;
        public double ReactionHeatBiomass { get; set; } = 13.6;
        public double HeatTransferCoeffienctSteelAir { get; set; } = 0.0343;
        public double SimDeltaTime { get; set; } = 10; // s -> duration of sim steps
        public double StartBio { get; set; } = 1; // g/L 
        public double StartSugar { get; set; } = 0; // g/L
        public double StartV { get; set; } = 450; // L
        public double StartTemp { get; set; } = 300; // L
        public double MaxV { get; set; } = 1200; // L
        public double MueMaxOx { get; set; } = 0.17 / 3600;
        public double MueMaxRed { get; set; } = 0.30 / 3600;
        public double MueMaxEth { get; set; } = 0.05 / 3600;
        public double MonodConstant { get; set; } = 0.01;
        public double YieldGOx { get; set; } = 0.53;
        public double YieldGRed { get; set; } = 0.20;
        public double YieldERed { get; set; } = 0.51;
        public double RequiredOxygenOx { get; set; } = 0.76;
        public double RequiredOxygenRed { get; set; } = 0.14;
        public double ArrheniusEA { get; set; } = 54700;
        public double ArrheniusA0 { get; set; } = 2.7e9;
        public double CoolantTemp { get; set; } = 273.15 + 2.5;
        public double EnviromentTemp { get; set; } = 273.15 + 18;
        public double FeedTemp { get; set; } = 273.15 + 18;
        public double CoolantFlow { get; set; } = 1200 / 3600; // L/s
        public double AirFlow { get; set; } = 0.5; // m/s
        public double ReactorRadius { get; set; } = 0.5; // m
        public double FeedSugarConcentration { get; set; } = 800; // g/L
    }

}
