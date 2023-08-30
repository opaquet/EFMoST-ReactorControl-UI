using Core.DataTypes;
using Core.Modules.Interfaces;

namespace Core.Modules
{
    internal class ReactorControl : IReactorControl, IDisposable {
        public event Action<string, int>? LogEvent;
        public event Action<string>? StartupEvent;
        public event Action? ControlCycleFinished;

        private Action<string>? _sendCommandToControllerMCU;

        private TReactorControlSettings _settings;
        private Timer? _controlIntervalTimer;
        private Timer? _pwmFeedControlOffTimer;
        private Timer? _pwmTempControlOffTimer;
        private IDataHandler _dataHandler;
        private DateTime _controlTimerLastCallback = DateTime.Now;
        private bool _feedRateRetracted = false;

        private double[] _vPIDErrors = { 0, 0, 0 };
        private bool _vPIDSaturated = false;
        private double _vPIDLastError = 0;

        private double[] _tempPIDErrors = { 0, 0, 0 };
        private bool _tempPIDSaturated = false;
        private double _tempPIDLastError = 0;

        // FeedControlDataexcangeMethods
        public void SetFeedRate(double value) => CalculatedFeedRateValue = value;


        // properties for calculated control values
        public double CalculatedFeedRateValue { get; set; } = 0;
        public double CalculatedVentilationValue { get; set; } = 0;
        public double CalculatedRPMValue { get; set; } = 0;
        public bool CalculatedTempValveSetting { get; set; } = false;
        public double CalculatedTempControlSeeting { get; set; } = 0;


        // properties for current reactor state, all read from the _dataHandler (coming from the plc)
        private double CurrentFeedRateValue { get => _dataHandler.GetLastControllerDatum().Values[MeasurementValue.Feedrate]; }
        private double CurrentVentilationValue { get => _dataHandler.GetLastControllerDatum().Values[MeasurementValue.Belüftung]; }
        private double ReactorTemperature { get => _dataHandler.GetLastControllerDatum().Values[MeasurementValue.Temperatur]; }
        private double ReactorTemperatureSetpoint { get => _dataHandler.GetLastControllerDatum().Setpoints[MeasurementValue.Temperatur]; }
        private double ReactorDissolvedOxygenConcentration { get => _dataHandler.GetLastControllerDatum().Values[MeasurementValue.gelSauerstoff]; }
        private double ReactorEthanolConcentration { get => _dataHandler.GetLastComputedValueDatum().Ethanol; }


        // properties for control and automation state settings, all read from the _dataHandler (coming from the plc)
        public bool IsFeedAutomationActive { get => _dataHandler.GetLastControllerDatum().Automatic[ControlVariable.Feed]; }
        public bool IsVentilationAutomationActive { get => _dataHandler.GetLastControllerDatum().Automatic[ControlVariable.Ventilation]; }
        public bool IsTemperatureAutomationActive { get => _dataHandler.GetLastControllerDatum().Automatic[ControlVariable.Temperature]; }
        public bool IsMixingAutomationActive { get => _dataHandler.GetLastControllerDatum().Automatic[ControlVariable.Rotation]; }
        public bool IsFeedControlActive { get => _dataHandler.GetLastControllerDatum().Active[ControlVariable.Feed]; }
        public bool IsVentilationControlActive { get => _dataHandler.GetLastControllerDatum().Active[ControlVariable.Ventilation]; }
        public bool IsTemperatureControlActive { get => _dataHandler.GetLastControllerDatum().Active[ControlVariable.Temperature]; }
        public bool IsMixingControlActive { get => _dataHandler.GetLastControllerDatum().Active[ControlVariable.Rotation]; }



        public ReactorControl(TReactorControlSettings reactorControlSettings, Action<string> sendCommandAction, IDataHandler dataHandler) {
            _settings = reactorControlSettings;
            _sendCommandToControllerMCU = sendCommandAction;
            _dataHandler = dataHandler;
        }

        public void Begin() {
            LogEvent?.Invoke("ControlModule started!", 1);
            StartupEvent?.Invoke("resetting controlIntervalTimer...");
            _controlIntervalTimer?.Dispose();
            StartupEvent?.Invoke("starting controlIntervalTimer...");
            _controlIntervalTimer = new Timer(ControlTimerCallback, null, 0, _settings.ReactorControlTimerIntervalMilliseconds);
        }

        public void Dispose() {
            LogEvent?.Invoke("ControlModule stopped!", 1);
            _controlIntervalTimer?.Dispose();
        }


        #region Control Logic
        /// <summary>
        /// This timer callback gets called every CONTROL_INTERVAL_MS milliseconds and computes the actual control 
        /// outputs for each of the (controllable) process state variable.
        /// </summary>
        private void ControlTimerCallback(object? state) {
            // delta t in seconds should be "ControlTimerInterval" but the timer is not very accurate...
            DateTime actualTime = DateTime.Now;
            double dt = (actualTime - _controlTimerLastCallback).TotalHours;
            _controlTimerLastCallback = actualTime;

            if (dt < 9/3600) { return ; } //skip the first call

            // exponential feed increase, but slow down if ethanol concnetration increases beyond critical threshold
            if (IsFeedAutomationActive) {
                if (CheckControlDeviation(CalculatedFeedRateValue, CurrentFeedRateValue)) {
                    //LogEvent?.Invoke($"Warning: Requested feedrate ({CalculatedFeedRateValue:N1} L/h) differs from reported feedrate ({CurrentFeedRateValue:N1} L/h) by more than 10 %!", 0);
                }
                UpdateFeedRateControlOutput(dt);
                SendFeedRateToController(CalculatedFeedRateValue); 
            }
            // pid ventilation control based on dissolved oxygen
            if (IsVentilationAutomationActive) {
                if (CheckControlDeviation(CalculatedVentilationValue, CurrentVentilationValue)) {
                    //LogEvent?.Invoke($"Warning: Requested ventilation ({CalculatedVentilationValue:N1} L/min) differs from reported ventilation ({CurrentFeedRateValue:N1} L/min) by more than 10 %!", 0);
                }
                UpdateVentilationControlOutput(dt);
                SendVentilationToController(CalculatedVentilationValue);
            }
            // simple 2 stage on-off cooling control to keep temperature at 30 °C
            if (IsTemperatureAutomationActive) {
                UpdateTemperatureControlOutput(dt);
                SendCoolingValveSeetingToController(CalculatedTempControlSeeting);
            }
            // pid control based on temperture gradient from top to bottom (keep a deltaT of 2.500 K)
            if (IsMixingAutomationActive) {
                // not implemented yet
            }
            try {
                ControlCycleFinished?.Invoke();
            } catch (Exception ex) { LogEvent?.Invoke($"Error updating UI: {ex.Message}", 0); }
        }


        private void UpdateTemperatureControlOutput(double dt) {
            _tempPIDErrors[0] = -1 * (_settings.TemperatureSetpoint - ReactorTemperature);
            _tempPIDErrors[1] += _tempPIDSaturated ? 0 : _tempPIDErrors[0] * dt;
            _tempPIDErrors[1] = Math.Max(0, _tempPIDErrors[1]); // integral term should not be less than zero
            _tempPIDErrors[2] = (_tempPIDLastError - _tempPIDErrors[0]) / dt;
            _tempPIDLastError = _tempPIDErrors[0];

            double PIDOutputValue = _tempPIDErrors[0] * _settings.TemperatureControlSettings.PID_kp
                                  + _tempPIDErrors[1] * _settings.TemperatureControlSettings.PID_ki
                                  + _tempPIDErrors[2] * _settings.TemperatureControlSettings.PID_kd;

            // clamp PID output to allowed range and set _tempPIDSaturated if clamping was necessary for AntiWindup
            CalculatedTempControlSeeting = Math.Max(0, Math.Min(PIDOutputValue, 100));
            _tempPIDSaturated = ( CalculatedTempControlSeeting != PIDOutputValue );
        }

        private void UpdateFeedRateControlOutput(double dt) {
            if (ReactorEthanolConcentration > _settings.FeedControlSettings.EthanolTargetConcentration) {
                if (!_feedRateRetracted) {
                    _feedRateRetracted = true;
                    CalculatedFeedRateValue *= _settings.FeedControlSettings.FeedRateRetractFactor;
                }
            } else {
                _feedRateRetracted = false;
                if (ReactorTemperature < _settings.TemperatureSetpoint) {
                    CalculatedFeedRateValue += CalculatedFeedRateValue * _settings.FeedControlSettings.FeedRateIncrement * dt;
                } else {
                    //weiß ich noch nicht, mache ertsmal nichts
                }
            }
            CalculatedFeedRateValue = Math.Min(CalculatedFeedRateValue, _settings.FeedControlSettings.MaxFeedRateLPerH);
        }

        private void UpdateVentilationControlOutput(double dt) {
            _vPIDErrors[0] = _settings.VentilationControlSettings.OxygenTargetConcentration - ReactorDissolvedOxygenConcentration;
            _vPIDErrors[1] += _vPIDSaturated ? 0 : _vPIDErrors[0] * dt;
            _vPIDErrors[1] = Math.Max(0, _vPIDErrors[1]); // integral term should not be less than zero
            _vPIDErrors[2] = (_vPIDLastError - _vPIDErrors[0]) / dt;
            _vPIDLastError = _vPIDErrors[0];

            double PIDOutputValue = _vPIDErrors[0] * _settings.VentilationControlSettings.PID_kp
                                  + _vPIDErrors[1] * _settings.VentilationControlSettings.PID_ki
                                  + _vPIDErrors[2] * _settings.VentilationControlSettings.PID_kd;

            // clamp PID output to allowed range and set _vPIDSaturated if clamping was necessary for AntiWindup
            CalculatedVentilationValue = Math.Max(0, Math.Min(PIDOutputValue, _settings.VentilationControlSettings.MaxVentilationRate));
            _vPIDSaturated = ( CalculatedVentilationValue != PIDOutputValue );
        }

        /// <summary>
        /// Checks if there is a deviation in desired and reported control value larger than the specified allowed 
        /// control deviation.
        /// </summary>
        /// <param name="desiredValue">The desired control input that was sent to the controller/plc</param>
        /// <param name="currentValue">The current control input that was reported by the controller/plc</param>
        /// <param name="allowedDeviation">The allowed deviation, Default=0.1 (or 10 %)</param>
        /// <returns>result: is the control deviation larger than allowed?</returns>
        private static bool CheckControlDeviation(double desiredValue, double currentValue, double allowedDeviation = 0.1) {
            return Math.Abs(desiredValue - currentValue) / desiredValue > allowedDeviation;
        }
        #endregion

        #region feedcontrol related methods
        /// <summary>
        /// Send the calculated feedrate values to the pump (or to plc which in turn controls the pump accordingly).
        /// Send the value directly if it is higher than 5% of the possible input range. If it is lower, switch to a 
        /// more accurate PWM control of the pump. 
        /// </summary>
        /// <param name="feedrateLPerH">calulated desired feedrate or pump speed in L/h</param>
        /// <remarks>
        /// It uses sligthly more than 5% as for PWM to avoid almost 100% duty cycle the resulting rapid on/off switching 
        /// of the pump. Cycle time is 10 seconds, duty cycle is calculated accoringly and handled by a separate timer.
        /// </remarks>
        private void SendFeedRateToController(double feedrateLPerH) {
            if (!IsFeedAutomationActive) { return; }
            if (_sendCommandToControllerMCU == null) { LogEvent?.Invoke($"Error sending Command: _sendCommandToControllerMCU delegate is null!", 0); return; }
            // convert actual feedrate int a 10bit dac compatible int value
            double feedrateWithTemperatureLimit = ApplyMaxTemperatureFeedThrottling(feedrateLPerH, ReactorTemperature, _settings.TemperatureMaxValue, _settings.TemperatureSetpoint);
            double feedrate10bitValue = CalculateActualFeedrate(feedrateWithTemperatureLimit);
            if (feedrate10bitValue < _settings.ControlPWMLowThresholdValue) {
                // Compute duty-cycle/on-time based on Input
                // Use sligthtly higher pumpspeed and reduce duty cycle accordingly
                int actualPWMfeedrateoutput = Convert.ToInt32(_settings.ControlPWMLowThresholdValue / _settings.ControlPWMMaxDutyCycle);
                double onTimeFactor = _settings.ControlPWMMaxDutyCycle * _settings.ReactorControlTimerIntervalMilliseconds;
                double onTimeFraction = feedrate10bitValue / actualPWMfeedrateoutput;

                int onTime = (int)( onTimeFactor * onTimeFraction );
                //turn controlinput on
                _sendCommandToControllerMCU("!SA(4,1)\n");
                _sendCommandToControllerMCU($"!SP(3,{actualPWMfeedrateoutput})\n");
                _sendCommandToControllerMCU("!AI\n");
                // Timer to turn controlinput off after duty cycle
                _pwmFeedControlOffTimer?.Dispose();
                _pwmFeedControlOffTimer = new Timer(PwmFeedControlOffTimerCallback, null, onTime, Timeout.Infinite);
            } else {
                // turn feed control to active if it is set to off for whatever reason
                if (!IsFeedControlActive) {
                    _sendCommandToControllerMCU("!SA(4,1)\n");
                }
                _sendCommandToControllerMCU($"!SP(3,{Convert.ToInt32(feedrate10bitValue)})\n");
                _sendCommandToControllerMCU("!AI\n");
            }
        }

        /// <summary>
        /// Throttle the glucose feed down if the tempreature increases beyond the setpoint. Reduce it to zero if 
        /// maximum temperatur is exceeded to stop the poor cells from killing themselfes due to overheating.
        /// </summary>
        /// <param name="inVal">The desired feedrate in L/h</param>
        /// <param name="actualTemperature">The current temperature of the reactor in °C</param>
        /// <param name="maxTemperature">The absolute maximum temperature that should not be exceeded in °C</param>
        /// <param name="setTemperature">The temperature setpoint (normal operating temperature) in °C</param>
        /// <returns></returns>
        private static double ApplyMaxTemperatureFeedThrottling(double inVal, double actualTemperature, double maxTemperature, double setTemperature) {
            // Determine the effective temperature for calculations (anything less that setTemperature would result in an increase of the feedrate which we do not want)
            double T = Math.Max(actualTemperature, setTemperature);
            // Calculate the temperature range over which throttling occurs
            double deltaT = maxTemperature - setTemperature;
            if (deltaT == 0) return 0;
            // Calculate the reduction factor based on the effective temperature, make sure it is always between 0 and 1
            double reductionFactor = Math.Max(0, Math.Min(1, (maxTemperature - T) / deltaT));
            return inVal * reductionFactor;
        }

        /// <summary>
        /// Non-linear conversion from actual calibrated feedrate in L/h to the controlinput, which must be in 
        /// in a range of 0 - 1023 for the 10 bit DAC on the controller. 
        /// </summary>
        /// <param name="feedrateLPerH">Feedrate in L/h ranging from 0 - 200</param>
        /// <returns>Feedrate as controlinput from 0 - 1023</returns>
        /// <remarks>
        /// We use a 5th order polynomial to approximately reverse the original value conversion from the 10-bit value to feedrate.
        /// The original conversion was a 3rd order polynomial, which is not directly reversible.
        /// </remarks>
        private double CalculateActualFeedrate(double feedrateLPerH) {
            // precomputes the powers of feedrateLPerH
            double inVal2 = feedrateLPerH * feedrateLPerH;
            double inVal3 = inVal2 * feedrateLPerH;
            double inVal4 = inVal3 * feedrateLPerH;
            double inVal5 = inVal4 * feedrateLPerH;
            double result = 3.32020e-10 * inVal5
                          - 1.32269e-7 * inVal4
                          + 2.09790e-5 * inVal3
                          - 1.02291e-3 * inVal2
                          + 3.65301e-1 * feedrateLPerH
                          - 1.80187e-1;
            // Scale the result to range of a 10-bit DAC (0 - 1023).
            double controlInput = (result * 10.23);
            controlInput = Math.Max(_settings.ControlRange.Min(), Math.Min(_settings.ControlRange.Max(), controlInput));
            return controlInput;
        }


        /// <summary>
        /// Feedrate PWM timer callback to turn off feed pump after the duty cycle time, when in PWM mode.
        /// </summary>
        /// <param name="state"></param>
        private void PwmFeedControlOffTimerCallback(object? state) {
            if (_sendCommandToControllerMCU == null) { return; }
            _sendCommandToControllerMCU("!SA(4,0)\n");
            _sendCommandToControllerMCU($"!SP(3,0)\n");
            _sendCommandToControllerMCU("!AI\n");
            try {
                ControlCycleFinished?.Invoke();
            } catch (Exception ex) { LogEvent?.Invoke($"Error updating UI: {ex.Message}", 0); }
        }
        #endregion

        #region ventilation related methods
        /// <summary>
        /// Send the calculated ventilation values to the proportional vir valve (or to plc which in turn controls the valve accordingly).
        /// </summary>
        /// <param name="ventilationLPerMin">Calulated desired ventilation or air valve position in L/min</param>
        private void SendVentilationToController(double VentilationLPerMin) {
            if (!IsVentilationAutomationActive) { return; }
            if (_sendCommandToControllerMCU == null) { LogEvent?.Invoke($"Error sending Command: _sendCommandToControllerMCU delegate is null!", 0); return; }
            int ventilation10bitValue = CalculateActualVentilation(VentilationLPerMin);
            if (!IsVentilationControlActive) { 
                _sendCommandToControllerMCU("!SA(3,1)\n");
            }
            _sendCommandToControllerMCU($"!SP(2,{ventilation10bitValue})\n");
            _sendCommandToControllerMCU("!AI\n");
        }

        /// <summary>
        /// linear conversion from actual calibrated ventilation value in L/min to the controlinput, which must be in 
        /// in a range of 0 - 1023 for the 10 bit DAC on the controller. 
        /// </summary>
        /// <param name="ventilationLPerMin">Ventilation in L/min ranging from 0 - 400</param>
        /// <returns>Ventilation as controlinput from 0-1023</returns>
        /// <remarks>
        /// We use a simple linear conversion here as the valve postion is directly proprtional to air flow
        /// </remarks>
        private static int CalculateActualVentilation(double ventilationLPerMin) {
            return (int)Math.Round(ventilationLPerMin / 4.5 * 10.24);
        }
        #endregion

        #region tempcontrol related methods
        /// <summary>
        /// Tempeature control is done by opening and closing the two coolant valves in a PWM fashion... 
        /// The valves are so slow to react (5 minutes to fully open) that we can use this to emulate an actual proportional valve control
        /// </summary>
        private void SendCoolingValveSeetingToController(double valveProportionalValue) {
            if (!IsTemperatureAutomationActive) { return; }
            if (_sendCommandToControllerMCU == null) { LogEvent?.Invoke($"Error sending Command: _sendCommandToControllerMCU delegate is null!", 0); return; }

            double onTimeFraction = valveProportionalValue / 100;

            int onTime = (int) ( onTimeFraction * _settings.ReactorControlTimerIntervalMilliseconds);
            //turn controlinput on if off
            if (!IsTemperatureControlActive) {
                _sendCommandToControllerMCU("!SA(5,1)\n");
            }

            // open valves (by setting setpoint low) 
            if (onTime < 500) { //if duty cycle is less than 5% dont bother turning on
                if (ReactorTemperatureSetpoint != 40) {// but first check if valve is closed. close valve if it is open...
                    _sendCommandToControllerMCU($"!SP(4,400)\n");
                    _sendCommandToControllerMCU("!AI\n");
                }

                CalculatedTempValveSetting = false;
                return;
            }
            _sendCommandToControllerMCU($"!SP(4,0)\n");
            _sendCommandToControllerMCU("!AI\n");
            CalculatedTempValveSetting = true;

            // Timer to close valves (by setting setpoint high) after duty cycle
            if (onTime > 9500) { return; } //if duty cycle is more than 95% dont bother turning off
            _pwmTempControlOffTimer?.Dispose();
            _pwmTempControlOffTimer = new Timer(PwmTempControlOffTimerCallback, null, onTime, Timeout.Infinite);
        }

        /// <summary>
        /// Temp PWM timer callback to close coolant valves after the duty cycle time.
        /// </summary>
        /// <param name="state"></param>
        private void PwmTempControlOffTimerCallback(object? state) {
            if (_sendCommandToControllerMCU == null) { return; }
            _sendCommandToControllerMCU($"!SP(4,400)\n");
            _sendCommandToControllerMCU("!AI\n");
            CalculatedTempValveSetting = false;
            try {
                ControlCycleFinished?.Invoke();
            } catch (Exception ex) { LogEvent?.Invoke($"Error updating UI: {ex.Message}", 0); }
        }
        #endregion
    }
}
