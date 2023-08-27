using Core;
using Core.DataTypes;
using Core.Devices;
using Core.Modules.Interfaces;
using ViewModel.ViewModels;

namespace ViewModel
{
    internal static class Factory {

        public static Manager CreateManagerInstance() {
            Manager manager = new();
            manager.Begin();
            return manager;
        }

        public static LogStringViewModel CreateLogViewModel(IEventLogger? log) {
            return (log == null)
                ? throw new NullReferenceException() 
                : new(log);
        }

        public static ChartViewModel CreateChartViewModel(Manager? manager, IReadOnlyList<ValueViewModel> measurementViewModels) {
            if (manager == null) { throw new NullReferenceException(); }
            ChartViewModel viewModels = new();
                foreach (var measurement in measurementViewModels) {
                viewModels.AddSeries(measurement.Name, "h", measurement.Unit, measurement.MinValue, measurement.MaxValue);
            }

            

            void handler(TControllerData ContollerData) {
                for (int i = 0; i < 14; i++) {
                    viewModels.AddPoint(new MeasurementPoint { X = (manager.StartTime - DateTime.Now).TotalHours, Y = ContollerData.Values[i] },i);
                    if (i < 6) {
                        viewModels.AddSetPoint(new MeasurementPoint { X = (manager.StartTime - DateTime.Now).TotalHours, Y = ContollerData.Setpoints[i] }, i);
                    }
                }
            }
            manager.ControllerDataUpdate += handler;
            _controllerDataUpdateHandlers.Add(handler);

            void handler3(TComputedValuesData ComputedData) {
                viewModels.AddPoint(new MeasurementPoint { X = (manager.StartTime - DateTime.Now).TotalHours, Y = ComputedData.Ethanol }, 14);
                viewModels.AddPoint(new MeasurementPoint { X = (manager.StartTime - DateTime.Now).TotalHours, Y = ComputedData.H2S }, 15);
            }
            manager.ComputedValuesDataUpdate += handler3;
            _computedDataUpdateHandlers.Add(handler3);

            void handler2(TGasSensorData GasSensorData) {
                viewModels.AddPoint(new MeasurementPoint { X = (manager.StartTime - DateTime.Now).TotalHours, Y = GasSensorData.Values.Average() }, 16);
            }
            manager.GasSensorDataUpdate += handler2;
            _gasSensorDataUpdateHandlers.Add(handler2);

            return viewModels;
        }

        public static DeviceViewModel CreateDeviceViewModel(IDevice? dev, IEventLogger? log) {
            return (dev == null)
                ? throw new NullReferenceException()
                : new(dev, CreateLogViewModel(log));
        }


        public static ValueViewModel CreateIndicatorDirectControlViewModel(Manager? manager) {
            if (manager == null) { throw new NullReferenceException(); }
            ValueViewModel viewModel = new(name: "Direktsteuerung" , isNameVisible: true, isValueVisible: false);
            void handler(TControllerData ContollerData) => viewModel.SetValue(ContollerData.DirectControl ? 2 : 0);
            manager.ControllerDataUpdate += handler;
            _controllerDataUpdateHandlers.Add(handler);
            return viewModel;
        }

        public static ValueViewModel CreateGasSenoserStatusValueViewModel(Manager? manager) {
            if (manager == null) { throw new NullReferenceException(); }
            ValueViewModel ValueViewModel = new(name: "GasSensor State");
            void handler(TGasSensorData GasSensorData) => ValueViewModel.SetValue(GasSensorData.SensorState);
            manager.GasSensorDataUpdate += handler;
            _gasSensorDataUpdateHandlers.Add(handler);
            return ValueViewModel;
        }

        public static IReadOnlyList<ValueViewModel> CreateControlSettingValueViewModels(Manager? manager) {
            if (manager == null) { throw new NullReferenceException(); }
            if (manager.reactorControl == null) { throw new NullReferenceException(); }
            if (manager.settings == null) { throw new NullReferenceException(); }
            List<ValueViewModel> ValueViewModels = new() {

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
            Action handler = () => { ValueViewModels[0].SetValue(manager.settings.ReactorControlSettings.TemperatureMaxValue); };
            manager.reactorControl.ControlCycleFinished += handler;
            ControlCycleUpdateHandlers.Add(handler);

            handler = () => { ValueViewModels[1].SetValue(manager.settings.ReactorControlSettings.TemperatureSetpoint); };
            manager.reactorControl.ControlCycleFinished += handler;
            ControlCycleUpdateHandlers.Add(handler);

            handler = () => { ValueViewModels[4].SetValue(manager.reactorControl.CalculatedFeedRateValue); };
            manager.reactorControl.ControlCycleFinished += handler;
            ControlCycleUpdateHandlers.Add(handler);

            handler = () => { ValueViewModels[9].SetValue(manager.reactorControl.CalculatedVentilationValue); };
            manager.reactorControl.ControlCycleFinished += handler;
            ControlCycleUpdateHandlers.Add(handler);

            handler = () => { ValueViewModels[13].SetValue(manager.reactorControl.CalculatedTempControlSeeting); };
            manager.reactorControl.ControlCycleFinished += handler;
            ControlCycleUpdateHandlers.Add(handler);

            handler = () => { ValueViewModels[14].SetValue(manager.reactorControl.CalculatedTempValveSetting ? 1 : 0); };
            manager.reactorControl.ControlCycleFinished += handler;
            ControlCycleUpdateHandlers.Add(handler);

            handler = () => { ValueViewModels[15].SetValue(manager.reactorControl.CalculatedRPMValue); };
            manager.reactorControl.ControlCycleFinished += handler;
            ControlCycleUpdateHandlers.Add(handler);


            return ValueViewModels;
        }

            public static IReadOnlyList<ValueViewModel> CreateSetPointValueViewModels(Manager? manager) { 
            if (manager == null) { throw new NullReferenceException(); }
            // generate List of Viewmodels
            List<ValueViewModel> ValueViewModels = new() {
                new(name:"Füllstand",unit:"L",maxValue:1200),
                new(name:"Drehzahl",unit:"U/min",maxValue:350),
                new(name:"Belüftung",unit:"L/min",maxValue:500),
                new(name:"Feedrate",unit:"L/h",maxValue:200),
                new(name:"Temperatur",unit:"°C",maxValue:50),
                new(name:"Konzentrierung",unit:"%",maxValue:110),
            };
            // setup update Events
            for (int i = 0; i < ValueViewModels.Count; i++) {
                int localI = i;
                void handler(TControllerData ContollerData) {
                    ValueViewModels[localI].SetValue(ContollerData.Setpoints[localI]);
                }
                manager.ControllerDataUpdate += handler;
                _controllerDataUpdateHandlers.Add(handler);
            }
            return ValueViewModels;
        }

        public static IReadOnlyList<ValueViewModel> CreateMeasurementValueViewModels(Manager? manager) {
            if (manager == null) { throw new NullReferenceException(); }
            // generate List of Viewmodels
            List<ValueViewModel> ValueViewModels = new() {
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
            for (int i = 0; i < ValueViewModels.Count; i++) {
                int localI = i;
                void handler(TControllerData ContollerData) => ValueViewModels[localI].SetValue(ContollerData.Values[localI]);
                manager.ControllerDataUpdate += handler;
                _controllerDataUpdateHandlers.Add(handler);
            }

            void handler2(TComputedValuesData ComputedData) => ValueViewModels[15].SetValue(ComputedData.Ethanol);
            manager.ComputedValuesDataUpdate += handler2;
            _computedDataUpdateHandlers.Add(handler2);

            void handler3(TComputedValuesData ComputedData) => ValueViewModels[16].SetValue(ComputedData.H2S);
            manager.ComputedValuesDataUpdate += handler3;
            _computedDataUpdateHandlers.Add(handler3);

            void handler4(TGasSensorData GasSensorData) => ValueViewModels[17].SetValue(GasSensorData.Values.Average());
            manager.GasSensorDataUpdate += handler4;
            _gasSensorDataUpdateHandlers.Add(handler4);

            return ValueViewModels;
        }

        public static IReadOnlyList<ValueViewModel> CreateGasSenoserEnviromentValueViewModels(Manager? manager) {
            if (manager == null) { throw new NullReferenceException(); }
            List<ValueViewModel> ValueViewModels = new() {
                new(name:"Temperatur",unit:"°C",maxValue:50),
                new(name:"Luftfeuchte",unit:"%",maxValue:100),
                new(name:"Druck",unit:"mbar",maxValue:2000),
            };
            // setup update Events
            for (int i = 0; i < ValueViewModels.Count; i++) {
                int localI = i;
                void handler(TGasSensorData GasSensorData) => ValueViewModels[localI].SetValue(GasSensorData.Enviroment[localI]);
                manager.GasSensorDataUpdate += handler;
                _gasSensorDataUpdateHandlers.Add(handler);
            }
            return ValueViewModels;
        }

        public static IReadOnlyList<ValueViewModel> CreateAnalogOutValueViewModels(Manager? manager) {
            if (manager == null) { throw new NullReferenceException(); }
            // generate List of Viewmodels
            List<ValueViewModel> ValueViewModels = new() {
                new(name:"A1",isNameVisible:true,maxValue:4096,decimalPlaces : 0, information:"Rührer/Rotationsfilter Drehzahl"),
                new(name:"A2",isNameVisible:true,maxValue:4096,decimalPlaces : 0, information:"Dosierpumpe Geschwindigkeit"),
                new(name:"A3",isNameVisible:true,maxValue:4096,decimalPlaces : 0, information:"Prop-Ventil Belüftung"),
                new(name:"A4",isNameVisible:true,maxValue:4096,decimalPlaces : 0, information:"Reserve"),
            };
            // setup update Events
            for (int i = 0; i < ValueViewModels.Count; i++) {
                int localI = i;
                void handler(TControllerData ContollerData) => ValueViewModels[localI].SetValue(ContollerData.AnalogOut[localI]);
                manager.ControllerDataUpdate += handler;
                _controllerDataUpdateHandlers.Add(handler);
            }
            return ValueViewModels;
        }

        public static IReadOnlyList<ValueViewModel> CreateDigitalOutIndicatorViewModels(Manager? manager) {
            if (manager == null) { throw new NullReferenceException(); }
            // generate List of Viewmodels
            List<ValueViewModel> ValueViewModels = new() {
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
            for (int i = 0; i < ValueViewModels.Count; i++) {
                int localI = i;
                void handler(TControllerData ContollerData) => ValueViewModels[localI].SetValue(ContollerData.DigitalOut[localI] ? 1 : 0);
                manager.ControllerDataUpdate += handler;
                _controllerDataUpdateHandlers.Add(handler);
            }
            return ValueViewModels;
        }

        public static IReadOnlyList<ValueViewModel> CreateAutomationIndicatorViewModels(Manager? manager) {
            if (manager == null) { throw new NullReferenceException(); }
            // generate List of Viewmodels
            List<ValueViewModel> ValueViewModels = new() {
                new(name:"Füllstand", isNameVisible: false, isValueVisible: false),
                new(name:"Drehzahl", isNameVisible: false, isValueVisible: false),
                new(name:"Belüftung", isNameVisible: false, isValueVisible: false),
                new(name:"Feedrate", isNameVisible: false, isValueVisible: false),
                new(name:"Temperatur", isNameVisible: false, isValueVisible: false),
                new(name:"Konzentrierung", isNameVisible: false, isValueVisible: false),
            };
            // setup update Events
            for (int i = 0; i < ValueViewModels.Count; i++) {
                int localI = i;
                void handler(TControllerData ContollerData) => ValueViewModels[localI].SetValue(ContollerData.Automatic[localI] ? 1 : 0);
                manager.ControllerDataUpdate += handler;
                _controllerDataUpdateHandlers.Add(handler);
            }
            return ValueViewModels;
        }

        public static IReadOnlyList<ValueViewModel> CreateControlActiveIndicatorViewModels(Manager? manager) {
            if (manager == null) { throw new NullReferenceException(); }
            // generate List of Viewmodels
            List<ValueViewModel> ValueViewModels = new() {
                new(name:"C", isNameVisible:true, isValueVisible:false),
                new(name:"C", isNameVisible:true, isValueVisible:false),
                new(name:"C", isNameVisible:true, isValueVisible:false),
                new(name:"C", isNameVisible:true, isValueVisible:false),
                new(name:"C", isNameVisible:true, isValueVisible:false),
                new(name:"C", isNameVisible:true, isValueVisible:false),
            };
            // setup update Events
            for (int i = 0; i < ValueViewModels.Count; i++) {
                int localI = i;
                void handler(TControllerData ContollerData) => ValueViewModels[localI].SetValue(ContollerData.Active[localI] ? 1 : 0);
                manager.ControllerDataUpdate += handler;
                _controllerDataUpdateHandlers.Add(handler);
            }
            return ValueViewModels;
        }

        public static IReadOnlyList<ValueViewModel> CreateAlarmIndicatorViewModels(Manager? manager) {
            if (manager == null) { throw new NullReferenceException(); }
            // generate List of Viewmodels
            List<ValueViewModel> ValueViewModels = new() {
                new(name:"A", isNameVisible:true, isValueVisible:false),
                new(name:"A", isNameVisible:true, isValueVisible:false),
                new(name:"A", isNameVisible:true, isValueVisible:false),
                new(name:"A", isNameVisible:true, isValueVisible:false),
                new(name:"A", isNameVisible:true, isValueVisible:false),
                new(name:"A", isNameVisible:true, isValueVisible:false),
            };
            // setup update Events
            for (int i = 0; i < ValueViewModels.Count; i++) {
                int localI = i;
                void handler(TControllerData ContollerData) => ValueViewModels[localI].SetValue(ContollerData.Alarm[localI] ? 1 : 0);
                manager.ControllerDataUpdate += handler;
                _controllerDataUpdateHandlers.Add(handler);
            }
            return ValueViewModels;
        }


        public static void UnsubscribeFromAllEvents(Manager? manager) {
            if (manager == null) { throw new NullReferenceException(); }
            if (manager.dataStore == null) { throw new NullReferenceException(); }
            if (manager.reactorControl == null) { throw new NullReferenceException(); }

            foreach (var handler in _controllerDataUpdateHandlers) {
                manager.ControllerDataUpdate -= handler;
            }
            foreach (var handler in _computedDataUpdateHandlers) {
                manager.dataStore.GasSensorCycleComplete -= handler;
            }
            foreach (var handler in _gasSensorDataUpdateHandlers) {
                manager.GasSensorDataUpdate -= handler;
            }
            foreach (var handler in ControlCycleUpdateHandlers) {
                manager.reactorControl.ControlCycleFinished -= handler;
            }
        }


        // Lists to keep track of event subscriptions
        private static List<Action<TControllerData>> _controllerDataUpdateHandlers = new();
        private static List<Action<TComputedValuesData>> _computedDataUpdateHandlers = new();
        private static List<Action<TGasSensorData>> _gasSensorDataUpdateHandlers = new();
        private static List<Action> ControlCycleUpdateHandlers = new();
    }
}
