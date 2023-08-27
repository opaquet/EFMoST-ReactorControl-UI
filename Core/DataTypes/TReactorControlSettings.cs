namespace Core.DataTypes {
    public class TFeedControlSettings {
        public double InitialFeedRate { get; set; } = 2.0;
        public double FeedRateIncrement { get; set; } = 0.22;
        public double MaxFeedRateLPerH { get; set; } = 200.0;
        public double FeedRateRetractFactor { get; set; } = 0.95;
        public double EthanolTargetConcentration { get; set; } = 1.0;
    }

    public class TVentilationControlSettings {
        public double PID_kp { get; set; } = 5.0;
        public double PID_ki { get; set; } = 20.0;
        public double PID_kd { get; set; } = 0.0;
        public double OxygenTargetConcentration { get; set; } = 5.5;
        public double MaxVentilationRate { get; set; } = 400.0;
    }

    public class TTemperatureControlSettings {
        public double PID_kp { get; set; } = 5.0;
        public double PID_ki { get; set; } = 20.0;
        public double PID_kd { get; set; } = 0.0;
    }

    public class TRPMControlSettings {
        // not implemented yet
    }

    public enum ProcessControlDataSource {
        Measurement,
        Simulation
    }

    public class TReactorControlSettings {
        public TFeedControlSettings FeedControlSettings { get; set; } = new TFeedControlSettings();
        public TVentilationControlSettings VentilationControlSettings { get; set; } = new TVentilationControlSettings();
        public TTemperatureControlSettings TemperatureControlSettings { get; set; } = new TTemperatureControlSettings();
        public TRPMControlSettings RPMControlSettings { get; set; } = new TRPMControlSettings();
        public ProcessControlDataSource AutomaticProcessControlDataSource { get; set; } = ProcessControlDataSource.Measurement;
        public double TemperatureSetpoint { get; set; } = 30;
        public double TemperatureMaxValue { get; set; } = 33;
        public int[] ControlRange { get; set; } = { 0, 1023 };
        public double ControlPWMLowThresholdValue { get => (ControlRange[1] - ControlRange[0]) * ControlPWMLowThresholdPercent + ControlRange[0]; }
        public double ControlPWMLowThresholdPercent { get; set; } = 0.05;
        public double ControlPWMMaxDutyCycle { get; set; } = 0.90; // maximum effective pwm duty cycle, to avoid rapid switching of devices
        public int ReactorControlTimerIntervalMilliseconds { get; set; } = 10000;
    }
}
