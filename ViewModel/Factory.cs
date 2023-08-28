using Core;
using Core.DataTypes;
using Core.Devices;
using Core.Modules.Interfaces;
using ViewModel.ViewModels;

#pragma warning disable CS8602 // Dereferenzierung eines möglichen Nullverweises.
namespace ViewModel {
    internal static class Factory {

        public static Manager CreateManagerInstance() {
            Manager manager = new();
            return manager;
        }

        public static LogStringViewModel CreateLogViewModel(IEventLogger? log) {
            return log is null ? throw new NullReferenceException() : new(log);
        }

        public static ChartViewModel CreateChartViewModel(Manager? manager, IReadOnlyList<ValueViewModel> measurementViewModels) {
            EnsureNotNull(manager, nameof(manager));
            ChartViewModel chartViewModel = new();
            foreach (var measurement in measurementViewModels) {
                chartViewModel.AddSeries(measurement.Name, "h", measurement.Unit, measurement.MinValue, measurement.MaxValue);
            }
            for (int i = 0; i < 6; i++) {
                chartViewModel.AddSetPointSeries("", "", "");
            }

            void handler(TControllerData ContollerData) {

                double time = ( DateTime.Now - manager.StartTime ).TotalHours;
                for (int i = 0; i < 14; i++) {
                    chartViewModel.AddPoint(new MeasurementPoint { X = time, Y = ContollerData.Values[i] },i);
                    if (i < 6) {
                        chartViewModel.AddSetPoint(new MeasurementPoint { X = time, Y = ContollerData.Setpoints[i] }, i);
                    }
                }
            }
            SubscribeToControllerDataUpdate(manager, handler);

            void handler3(TComputedValuesData ComputedData) {
                chartViewModel.AddPoint(new MeasurementPoint { X = (manager.StartTime - DateTime.Now).TotalHours, Y = ComputedData.Ethanol }, 14);
                chartViewModel.AddPoint(new MeasurementPoint { X = (manager.StartTime - DateTime.Now).TotalHours, Y = ComputedData.H2S }, 15);
            }
            SubscribeToComputedDataUpdate(manager, handler3);

            void handler2(TGasSensorData GasSensorData) {
                chartViewModel.AddPoint(new MeasurementPoint { X = (manager.StartTime - DateTime.Now).TotalHours, Y = GasSensorData.Values.Average() }, 16);
            }
            SubscribeToGasSensorDataUpdate(manager, handler2);

            return chartViewModel;
        }

        public static DeviceViewModel CreateDeviceViewModel(IDevice? dev, IEventLogger? log) {
            return (dev == null)
                ? throw new NullReferenceException()
                : new(dev, CreateLogViewModel(log));
        }


        public static ValueViewModel CreateIndicatorDirectControlViewModel(Manager? manager) {
            EnsureNotNull(manager, nameof(manager));
            ValueViewModel valueViewModel = new(name: "Direktsteuerung" , isNameVisible: true, isValueVisible: false);
            void handler(TControllerData ContollerData) => valueViewModel.SetValue(ContollerData.DirectControl ? 2 : 0);
            SubscribeToControllerDataUpdate(manager, handler);
            return valueViewModel;
        }

        public static ValueViewModel CreateGasSenoserStatusValueViewModel(Manager? manager) {
            EnsureNotNull(manager, nameof(manager));
            ValueViewModel valueViewModel = new(name: "GasSensor State");
            void handler(TGasSensorData GasSensorData) => valueViewModel.SetValue(GasSensorData.SensorState);
            SubscribeToGasSensorDataUpdate(manager, handler);
            return valueViewModel;
        }

        public static IReadOnlyList<ValueViewModel> CreateControlSettingValueViewModels(Manager? manager) {
            EnsureNotNull(manager, nameof(manager));
            EnsureNotNull(manager?.reactorControl, nameof(manager.reactorControl));
            EnsureNotNull(manager?.settings, nameof(manager.settings));
            List<ValueViewModel> valueViewModels = new() {
                //allgemein
                new(name:"Temperatur Soll",unit:"°C",maxValue:50, decimalPlaces : 1, 
                    value: manager.settings.ReactorControlSettings.TemperatureMaxValue, changeAllowed:true,
                    baseValueUpdateAction: (v) => manager.settings.ReactorControlSettings.TemperatureMaxValue = v),
                new(name:"Temperatur Max",unit:"°C",maxValue:50, decimalPlaces: 1, 
                    value: manager.settings.ReactorControlSettings.TemperatureSetpoint, changeAllowed:true,
                    baseValueUpdateAction: (v) => manager.settings.ReactorControlSettings.TemperatureSetpoint = v),
                //feedregelung
                new(name:"Inkrement",unit:"/h",maxValue:1, decimalPlaces: 2, 
                    value: manager.settings.ReactorControlSettings.FeedControlSettings.FeedRateIncrement, changeAllowed:true,
                    baseValueUpdateAction: (v) => manager.settings.ReactorControlSettings.FeedControlSettings.FeedRateIncrement = v),
                new(name:"Ethanol Soll",unit:"g/L",maxValue:20, decimalPlaces: 1, 
                    value: manager.settings.ReactorControlSettings.FeedControlSettings.EthanolTargetConcentration, changeAllowed:true,
                    baseValueUpdateAction: (v) => manager.settings.ReactorControlSettings.FeedControlSettings.EthanolTargetConcentration = v),
                new(name:"Ausgabe",unit:"/h",maxValue:1, decimalPlaces: 2, 
                    value: manager.reactorControl.CalculatedFeedRateValue, changeAllowed:true,
                    baseValueUpdateAction: (v) => manager.reactorControl.SetFeedRate(v)),
                //belüftungsregelung
                new(name:"dO Sollwert",unit:"mg/L",maxValue:25, decimalPlaces: 1, 
                    value: manager.settings.ReactorControlSettings.VentilationControlSettings.OxygenTargetConcentration, changeAllowed:true,
                    baseValueUpdateAction: (v) => manager.settings.ReactorControlSettings.VentilationControlSettings.OxygenTargetConcentration = v),
                new(name:"kP",unit:"",maxValue:1000, decimalPlaces: 2, 
                    value: manager.settings.ReactorControlSettings.VentilationControlSettings.PID_kp, changeAllowed:true,
                    baseValueUpdateAction: (v) => manager.settings.ReactorControlSettings.VentilationControlSettings.PID_kp = v),
                new(name:"kI",unit:"",maxValue:1000, decimalPlaces: 2, 
                    value: manager.settings.ReactorControlSettings.VentilationControlSettings.PID_ki, changeAllowed:true,
                    baseValueUpdateAction: (v) => manager.settings.ReactorControlSettings.VentilationControlSettings.PID_ki = v),
                new(name:"kD",unit:"",maxValue:1000, decimalPlaces: 2, 
                    value : manager.settings.ReactorControlSettings.VentilationControlSettings.PID_kd, changeAllowed:true,
                    baseValueUpdateAction: (v) => manager.settings.ReactorControlSettings.VentilationControlSettings.PID_kd = v),
                new(name:"Ausgabe",unit:"L/min",maxValue:100,decimalPlaces: 1, 
                    value: manager.reactorControl.CalculatedVentilationValue, changeAllowed:false),
                //tempregelung
                new(name:"kP",unit:"",maxValue:1000, decimalPlaces: 2, 
                    value: manager.settings.ReactorControlSettings.TemperatureControlSettings.PID_kp, changeAllowed:true,
                    baseValueUpdateAction: (v) => manager.settings.ReactorControlSettings.TemperatureControlSettings.PID_kp = v),
                new(name:"kI",unit:"",maxValue:1000, decimalPlaces: 2, 
                    value : manager.settings.ReactorControlSettings.VentilationControlSettings.PID_ki, changeAllowed:true,
                    baseValueUpdateAction: (v) => manager.settings.ReactorControlSettings.TemperatureControlSettings.PID_kp = v),
                new(name:"kD",unit:"",maxValue:1000, decimalPlaces: 2, 
                    value : manager.settings.ReactorControlSettings.VentilationControlSettings.PID_kd, changeAllowed:true,
                    baseValueUpdateAction: (v) => manager.settings.ReactorControlSettings.TemperatureControlSettings.PID_kp = v),
                new(name:"Ausgabe",unit:"%",maxValue:100,decimalPlaces: 1, 
                    value: manager.reactorControl.CalculatedTempControlSeeting, changeAllowed:false),
                new(name:"Ventil",unit:"",maxValue : 1, decimalPlaces : 0, 
                    value: manager.reactorControl.CalculatedTempValveSetting ? 1 : 0, changeAllowed:false),
                //drehzahlregelung
                new(name:"Ausgabe",unit:"U/min",maxValue:500,decimalPlaces: 0, 
                    value: manager.reactorControl.CalculatedRPMValue, changeAllowed:false)
            };

            // setup Events to react when the values have been changed by the core logic
            Action handler = () => { valueViewModels[0].SetValue(manager.settings.ReactorControlSettings.TemperatureMaxValue); };
            SubscribeToControlCycleUpdate(manager, handler);
            handler = () => { valueViewModels[1].SetValue(manager.settings.ReactorControlSettings.TemperatureSetpoint); };
            SubscribeToControlCycleUpdate(manager, handler);
            handler = () => { valueViewModels[4].SetValue(manager.reactorControl.CalculatedFeedRateValue); };
            SubscribeToControlCycleUpdate(manager, handler);
            handler = () => { valueViewModels[9].SetValue(manager.reactorControl.CalculatedVentilationValue); };
            SubscribeToControlCycleUpdate(manager, handler);
            handler = () => { valueViewModels[13].SetValue(manager.reactorControl.CalculatedTempControlSeeting); };
            SubscribeToControlCycleUpdate(manager, handler);
            handler = () => { valueViewModels[14].SetValue(manager.reactorControl.CalculatedTempValveSetting ? 1 : 0); };
            SubscribeToControlCycleUpdate(manager, handler);
            handler = () => { valueViewModels[15].SetValue(manager.reactorControl.CalculatedRPMValue); };
            SubscribeToControlCycleUpdate(manager, handler);

            return valueViewModels;
        }

            public static IReadOnlyList<ValueViewModel> CreateSetPointValueViewModels(Manager? manager) {
            EnsureNotNull(manager, nameof(manager));
            // generate List of Viewmodels
            List<ValueViewModel> valueViewModels = new() {
                new(name:"Füllstand",unit:"L",maxValue:1200),
                new(name:"Drehzahl",unit:"U/min",maxValue:350),
                new(name:"Belüftung",unit:"L/min",maxValue:500),
                new(name:"Feedrate",unit:"L/h",maxValue:200),
                new(name:"Temperatur",unit:"°C",maxValue:50),
                new(name:"Konzentrierung",unit:"%",maxValue:110),
            };
            // setup update Events
            for (int i = 0; i < valueViewModels.Count; i++) {
                int localI = i;
                void handler(TControllerData ContollerData) => valueViewModels[localI].SetValue(ContollerData.Setpoints[localI]);
                SubscribeToControllerDataUpdate(manager, handler);
            }
            return valueViewModels;
        }

        public static IReadOnlyList<ValueViewModel> CreateMeasurementValueViewModels(Manager? manager) {
            EnsureNotNull(manager, nameof(manager));
            // generate List of Viewmodels
            List<ValueViewModel> valueViewModels = new() {
                new(decimalPlaces : 0, maxValue : 1200,  name : "Füllstand", unit : "L"),
                new(decimalPlaces : 0, maxValue : 350,   name : "Drehzahl", unit : "U/min"),
                new(decimalPlaces : 0, maxValue : 500,   name : "Belüftung", unit : "L/min"),
                new(decimalPlaces : 1, maxValue : 200,   name : "Feedrate", unit : "L/h"),
                new(decimalPlaces : 1, maxValue : 50,    name : "Temperatur", unit : "°C"),
                new(decimalPlaces : 0, maxValue : 110,   name : "Konzentrierung", unit : "%"),
                new(decimalPlaces : 1, maxValue : 8,     name : "pH", unit : "",minValue:2),
                new(decimalPlaces : 1, maxValue : 25,    name : "gelSauerstoff", unit : "mg/L"),
                new(decimalPlaces : 1, maxValue : 25,    name : "Sauerstoff", unit : "%"),
                new(decimalPlaces : 1, maxValue : 50,    name : "Temp1", unit : "°C"),
                new(decimalPlaces : 1, maxValue : 50,    name : "Temp2", unit : "°C"),
                new(decimalPlaces : 1, maxValue : 50,    name : "Temp3", unit : "°C"),
                new(decimalPlaces : 0, maxValue : 1000,  name : "DruckDeckel", unit : "mbar"),
                new(decimalPlaces : 0, maxValue : 1000,  name : "DruckBoden", unit : "mbar"),
                new(decimalPlaces : 2, maxValue : 150,   name : "Ethanol", unit : "g/L"),
                new(decimalPlaces : 0, maxValue : 150,   name : "H2S",unit:"ppm"),
                new(decimalPlaces : 0, maxValue : 16384, name : "GasSensor",unit:"u"),
                new(decimalPlaces : 0, name : "frei"),
            };
            // setup update Events
            for (int i = 0; i < valueViewModels.Count; i++) {
                int localI = i;
                void handler(TControllerData ContollerData) => valueViewModels[localI].SetValue(ContollerData.Values[localI]);
                SubscribeToControllerDataUpdate(manager, handler);
            }

            void handler2(TComputedValuesData ComputedData) => valueViewModels[15].SetValue(ComputedData.Ethanol);
            SubscribeToComputedDataUpdate(manager, handler2);

            void handler3(TComputedValuesData ComputedData) => valueViewModels[16].SetValue(ComputedData.H2S);
            SubscribeToComputedDataUpdate(manager, handler3);

            void handler4(TGasSensorData GasSensorData) => valueViewModels[17].SetValue(GasSensorData.Values.Average());
            SubscribeToGasSensorDataUpdate(manager, handler4);

            return valueViewModels;
        }

        public static IReadOnlyList<ValueViewModel> CreateGasSenoserEnviromentValueViewModels(Manager? manager) {
            EnsureNotNull(manager, nameof(manager));
            List<ValueViewModel> valueViewModels = new() {
                new(name:"Temperatur",unit:"°C",maxValue:50),
                new(name:"Luftfeuchte",unit:"%",maxValue:100),
                new(name:"Druck",unit:"mbar",maxValue:2000),
            };
            // setup update Events
            for (int i = 0; i < valueViewModels.Count; i++) {
                int localI = i;
                void handler(TGasSensorData GasSensorData) => valueViewModels[localI].SetValue(GasSensorData.Enviroment[localI]);
                SubscribeToGasSensorDataUpdate(manager, handler);
            }
            return valueViewModels;
        }

        public static IReadOnlyList<ValueViewModel> CreateAnalogOutValueViewModels(Manager? manager) {
            EnsureNotNull(manager, nameof(manager));
            // generate List of Viewmodels
            List<ValueViewModel> valueViewModels = new() {
                new(name:"A1",isNameVisible:true,maxValue:4096,decimalPlaces : 0, information:"Rührer/Rotationsfilter Drehzahl"),
                new(name:"A2",isNameVisible:true,maxValue:4096,decimalPlaces : 0, information:"Dosierpumpe Geschwindigkeit"),
                new(name:"A3",isNameVisible:true,maxValue:4096,decimalPlaces : 0, information:"Prop-Ventil Belüftung"),
                new(name:"A4",isNameVisible:true,maxValue:4096,decimalPlaces : 0, information:"Reserve"),
            };
            // setup update Events
            for (int i = 0; i < valueViewModels.Count; i++) {
                int localI = i;
                void handler(TControllerData ContollerData) => valueViewModels[localI].SetValue(ContollerData.AnalogOut[localI]);
                SubscribeToControllerDataUpdate(manager, handler);
            }
            return valueViewModels;
        }

        public static IReadOnlyList<ValueViewModel> CreateDigitalOutIndicatorViewModels(Manager? manager) {
            EnsureNotNull(manager, nameof(manager));
            // generate List of Viewmodels
            List<ValueViewModel> valueViewModels = new() {
                new( name : "D1",  isNameVisible : true, isValueVisible : false, information : "K10: Motorventil"),
                new( name : "D2",  isNameVisible : true, isValueVisible : false, information : "K11: Magnetventil Belüftung"),
                new( name : "D3",  isNameVisible : true, isValueVisible : false, information : "K12: Magnetventil Konzentrierung"),
                new( name : "D4",  isNameVisible : true, isValueVisible : false, information : "K13: Regelventil Belüftung"),
                new( name : "D5",  isNameVisible : true, isValueVisible : false, information : "K14: Magnetventil Wasserzulauf"),
                new( name : "D6",  isNameVisible : true, isValueVisible : false, information : "K15: Magnetventil Kühlwasser Boden"),
                new( name : "D7",  isNameVisible : true, isValueVisible : false, information : "K16: Magnetventil Kühlwasser Mantel"),
                new( name : "D8",  isNameVisible : true, isValueVisible : false, information : "K17: Vorschaltventil für Prop-Ventil"),
                new( name : "D9",  isNameVisible : true, isValueVisible : false, information : "K18: FU Rechtslauf Signal"),
                new( name : "D10", isNameVisible : true, isValueVisible : false, information : "K19: FU Linkslauf Signal"),
                new( name : "D11", isNameVisible : true, isValueVisible : false, information : "K20: Schaumbekämpung Umwälzpumpe"),
                new( name : "D12", isNameVisible : true, isValueVisible : false, information : "K21: Reserve")
            };
            // setup update Events
            for (int i = 0; i < valueViewModels.Count; i++) {
                int localI = i;
                void handler(TControllerData ContollerData) => valueViewModels[localI].SetValue(ContollerData.DigitalOut[localI] ? 2 : 0);
                SubscribeToControllerDataUpdate(manager, handler);
            }
            return valueViewModels;
        }

        public static IReadOnlyList<ValueViewModel> CreateAutomationIndicatorViewModels(Manager? manager) {
            EnsureNotNull(manager, nameof(manager));
            // generate List of Viewmodels
            List<ValueViewModel> valueViewModels = new() {
                new(name:"Füllstand", isNameVisible: false, isValueVisible: false),
                new(name:"Drehzahl", isNameVisible: false, isValueVisible: false),
                new(name:"Belüftung", isNameVisible: false, isValueVisible: false),
                new(name:"Feedrate", isNameVisible: false, isValueVisible: false),
                new(name:"Temperatur", isNameVisible: false, isValueVisible: false),
                new(name:"Konzentrierung", isNameVisible: false, isValueVisible: false),
            };
            // setup update Events
            for (int i = 0; i < valueViewModels.Count; i++) {
                int localI = i;
                void handler(TControllerData ContollerData) => valueViewModels[localI].SetValue(ContollerData.Automatic[localI] ? 1 : 0);
                SubscribeToControllerDataUpdate(manager, handler);
            }
            return valueViewModels;
        }

        public static IReadOnlyList<ValueViewModel> CreateControlActiveIndicatorViewModels(Manager? manager) {
            EnsureNotNull(manager, nameof(manager));
            // generate List of Viewmodels
            List<ValueViewModel> valueViewModels = new() {
                new(name:"C", isNameVisible:true, isValueVisible:false),
                new(name:"C", isNameVisible:true, isValueVisible:false),
                new(name:"C", isNameVisible:true, isValueVisible:false),
                new(name:"C", isNameVisible:true, isValueVisible:false),
                new(name:"C", isNameVisible:true, isValueVisible:false),
                new(name:"C", isNameVisible:true, isValueVisible:false),
            };
            // setup update Events
            for (int i = 0; i < valueViewModels.Count; i++) {
                int localI = i;
                void handler(TControllerData ContollerData) => valueViewModels[localI].SetValue(ContollerData.Active[localI] ? 1 : 0);
                SubscribeToControllerDataUpdate(manager, handler);
            }
            return valueViewModels;
        }

        public static IReadOnlyList<ValueViewModel> CreateAlarmIndicatorViewModels(Manager? manager) {
            EnsureNotNull(manager, nameof(manager));
            // generate List of Viewmodels
            List<ValueViewModel> valueViewModels = new() {
                new(name:"A", isNameVisible:true, isValueVisible:false),
                new(name:"A", isNameVisible:true, isValueVisible:false),
                new(name:"A", isNameVisible:true, isValueVisible:false),
                new(name:"A", isNameVisible:true, isValueVisible:false),
                new(name:"A", isNameVisible:true, isValueVisible:false),
                new(name:"A", isNameVisible:true, isValueVisible:false),
            };
            // setup update Events
            for (int i = 0; i < valueViewModels.Count; i++) {
                int localI = i;
                void handler(TControllerData ContollerData) => valueViewModels[localI].SetValue(ContollerData.Alarm[localI] ? 1 : 0);
                SubscribeToControllerDataUpdate(manager, handler);
            }
            return valueViewModels;
        }


        private static void SubscribeToControllerDataUpdate(Manager? manager, Action<TControllerData> handler) {
            manager.ControllerDataUpdate += handler;
            _controllerDataUpdateHandlers.Add(handler);
        }

        private static void SubscribeToControlCycleUpdate(Manager? manager, Action handler) {
            manager.reactorControl.ControlCycleFinished += handler;
            _controlCycleUpdateHandlers.Add(handler);
        }
        private static void SubscribeToGasSensorDataUpdate(Manager? manager, Action<TGasSensorData> handler) {
            manager.GasSensorDataUpdate += handler;
            _gasSensorDataUpdateHandlers.Add(handler);
        }
        private static void SubscribeToComputedDataUpdate(Manager? manager, Action<TComputedValuesData> handler) {
            manager.ComputedValuesDataUpdate += handler;
            _computedDataUpdateHandlers.Add(handler);
        }

        public static void UnsubscribeFromAllEvents(Manager? manager) {
            EnsureNotNull(manager, nameof(manager));
            EnsureNotNull(manager?.dataStore, nameof(manager.dataStore));
            EnsureNotNull(manager?.reactorControl, nameof(manager.reactorControl));

            _controllerDataUpdateHandlers.ForEach((handler) => manager.ControllerDataUpdate -= handler);
            _computedDataUpdateHandlers.ForEach((handler) => manager.dataStore.GasSensorCycleComplete -= handler);
            _gasSensorDataUpdateHandlers.ForEach((handler) => manager.GasSensorDataUpdate -= handler);
            _controlCycleUpdateHandlers.ForEach((handler) => manager.reactorControl.ControlCycleFinished -= handler);
        }

        private static void EnsureNotNull(object? value, string name) {
            if (value == null) { throw new ArgumentNullException(name, $"{name} cannot be null."); }
        }


        // Lists to keep track of event subscriptions
        private static List<Action<TControllerData>> _controllerDataUpdateHandlers = new();
        private static List<Action<TComputedValuesData>> _computedDataUpdateHandlers = new();
        private static List<Action<TGasSensorData>> _gasSensorDataUpdateHandlers = new();
        private static List<Action> _controlCycleUpdateHandlers = new();
    }
    #pragma warning restore CS8602 // Dereferenzierung eines möglichen Nullverweises.
}
