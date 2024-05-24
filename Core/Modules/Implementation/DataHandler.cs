using Core.DataTypes;
using Core.Modules.Interfaces;
using System;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Core.Modules
{
    /// <summary>
    /// Class <b>DataHandler</b>. This contains methods and fields to evalate and store all data from the gas sensor and controller
    /// </summary>
    internal class DataHandler : IDataHandler, IDisposable {

        public event Action<TComputedValuesData>? GasSensorCycleComplete;
        public event Action<TControllerData.ControllerErrors>? ControllerErrorReport;
        public event Action<string,int>? LogEvent;
        public event Action<string>? StartupEvent;

        private List<TGasSensorData> _gasSensorData = new();
        private List<TComputedValuesData> _computedValues = new();
        private List<TControllerData> _controllerData = new();
        private List<TRawData> _relayData = new();
        private int _lastGasSensorCycle = 0;
        private Timer? _SaveTimer;
        private object _listAccessLock = new object();
        private object _fileAccessLock = new object();

        private IFileWriter _FileWriterGasSensorData;
        private IFileWriter _FileWriterControllerData;
        private IFileWriter _FileWriterRelayData;
        private IFileWriter _FileWriterComputedData;

        private TComputedValuesData _LastComputedDatum = new();
        private TControllerData _LastControllerDatum = new();
        private TGasSensorData _LastGasSensorDatum = new();

        #region data retrieval (properties and methods)
        public int GetGasSensorDataCount => _gasSensorData.Count; 
        public int GetControllerDataCount => _controllerData.Count; 
        public int GetComputedValuesCount => _computedValues.Count;
        public double[] GetAllEthanolValues() => ExtractValuesFromComputedData(data => data.Ethanol);
        public double[] GetAllH2SValues() => ExtractValuesFromComputedData(data => data.H2S);
        public double[] GetAllControllerValues(int Index) => _controllerData.Select(data => data.Values[Index]).ToArray();
        public double[] GetAllControllerSetpoints(int Index) => _controllerData.Select(data => data.Setpoints[Index]).ToArray();
        public double[] GetAllGasSensorValues(int Index) => _gasSensorData.Select(data => data.Values[Index]).ToArray();
        public double[] GetLastCycleGasSensorValues(int Index, int Cycle) => _gasSensorData.Where(data => data.MeasurementCycle == Cycle)
                                                                                           .Select(data => data.Values[Index])
                                                                                           .ToArray();
        public int[] GetAllGasSensorCycleNumbers() => _gasSensorData.Select(data => data.MeasurementCycle).ToArray();
        public TComputedValuesData GetComputedValueDatum(int index) => GetDatum(_computedValues, index);
        public TGasSensorData GetGasSensorDatum(int index) => GetDatum(_gasSensorData, index);
        public TControllerData GetControllerDatum(int index) => GetDatum(_controllerData, index);
        public TComputedValuesData GetLastComputedValueDatum() => _LastComputedDatum;
        public TGasSensorData GetLastGasSensorDatum() => _LastGasSensorDatum;
        public TControllerData GetLastControllerDatum() => _LastControllerDatum;
        #endregion

        #region data retrieval helper methods
        private static T GetDatum<T>(List<T> dataList, int index) where T : ICloneable {
            if (index < 0) { index = dataList.Count + index; }
            if (index >= dataList.Count) { index = dataList.Count - 1; }
            return (T)dataList[index].Clone();
        }
        private double[] ExtractValuesFromComputedData(Func<TComputedValuesData, double> selector) => _computedValues.Select(selector).ToArray();
        #endregion

        public DataHandler(IFileWriter fileWriterGas, IFileWriter fileWriterController, IFileWriter fileWriterRelay, IFileWriter fileWriterComputedData) {
            _gasSensorData.Add(new TGasSensorData());
            _computedValues.Add(new TComputedValuesData());
            _controllerData.Add(new TControllerData());
            _FileWriterGasSensorData = fileWriterGas;
            _FileWriterControllerData = fileWriterController;
            _FileWriterRelayData = fileWriterRelay;
            _FileWriterComputedData = fileWriterComputedData;
        }

        public void Begin() {
            LogEvent?.Invoke("DataHandler started!", 1);
            _SaveTimer?.Dispose();
            _SaveTimer = new Timer(SaveTimerCallback, null, 0, 600000);
        }

        private void SaveTimerCallback(object? state) {
            SaveGasSensorDataCSV();
            SaveControllerDataCSV();
            SaveRelayDataCSV();
            SaveComputedDataCSV();
        }


        public void Dispose() {
            try {
                string desktopFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                SaveGasSensorDataCSV();
                SaveControllerDataCSV();
                SaveRelayDataCSV();
                SaveComputedDataCSV();
            } catch (Exception ex) {
                LogEvent?.Invoke($"Error during Dispose: {ex.Message}",0);
            }
            _SaveTimer?.Dispose();
            LogEvent?.Invoke("DataHandler stopped", 1);
        }

        #region load and save data methods
        public void SaveAllDataJson(string filename) {
            try {
                string jsonData = string.Empty;
                lock (_listAccessLock) {
                    var dataToSave = new {
                        GasSensorData = _gasSensorData.Skip(1),
                        ComputedValues = _computedValues.Skip(1),
                        ControllerData = _controllerData.Skip(1),
                        RawMeasurementData = _relayData
                    };
                    var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
                    jsonData = JsonSerializer.Serialize(dataToSave, jsonOptions);
                }
                lock (_fileAccessLock) {
                    File.WriteAllText(filename, jsonData);
                }
                LogEvent?.Invoke($"Data saved successfully to {filename}.", 1);
            } catch (Exception ex) {
                LogEvent?.Invoke($"Error: Error saving data to {filename}. Error: {ex.Message}", 0);
            }
        }

        public void SaveRelayDataCSV() {
            try {
                StringBuilder csvContent = new();
                // Header for GasSensorData
                csvContent.AppendLine("TimeStamp; Relayed message");
                // Actual Data
                lock (_listAccessLock) {
                    foreach (TRawData data in _relayData) {
                        csvContent.AppendLine($"{data.TimeStamp};{data.Message}");
                    }
                }
                lock (_fileAccessLock) {
                    _FileWriterRelayData.WriteLines(csvContent);
                }
                LogEvent?.Invoke($"Relay data saved successfully to {_FileWriterRelayData.FileName}.", 1);
            } catch (Exception ex) {
                LogEvent?.Invoke($"Error: Error saving relay data to {_FileWriterRelayData.FileName}. Error: {ex.Message}", 0);
            }
        }

        public void SaveGasSensorDataCSV() {
            try {
                StringBuilder csvContent = new();
                // Header for GasSensorData
                csvContent.AppendLine("TimeStamp;ID;S1;S2;S3;S4;S5;S6;S7;S8;S9;Env1;Env2;Env3;ValveSetting;MeasurementCycle;SensorTime;SensorState;SensorStateTime");
                // Actual Data
                lock (_listAccessLock) {
                    foreach (TGasSensorData data in _gasSensorData.Skip(1)) {
                        string values = string.Join(";", data.Values.Select(v => v.ToString(CultureInfo.InvariantCulture)));
                        string environment = string.Join(";", data.Enviroment.Select(v => v.ToString(CultureInfo.InvariantCulture)));
                        csvContent.AppendLine($"{data.TimeStamp};{data.ID};{values};{environment};{data.ValveSetting};{data.MeasurementCycle};{data.SensorTime};{data.SensorState};{data.SensorStateTime}");
                    }
                }
                lock (_fileAccessLock) {
                    _FileWriterGasSensorData.WriteLines(csvContent);
                }
                LogEvent?.Invoke($"GasSensor data saved successfully to {_FileWriterGasSensorData.FileName}.", 1);
            } catch (Exception ex) {
                LogEvent?.Invoke($"Error: Error saving GasSensor data to {_FileWriterGasSensorData.FileName}. Error: {ex.Message}", 0);
            }
        }

        public void SaveControllerDataCSV() {
            try {
                StringBuilder csvContent = new();
                // Header for ControllerData
                lock (_listAccessLock) {
                    int maxValuesCount = _controllerData.Last().Values.Length;
                    int maxSetpointsCount = _controllerData.Last().Setpoints.Length;
                    int maxAutomaticCount = _controllerData.Last().Automatic.Length;
                    int maxActiveCount = _controllerData.Last().Active.Length;
                    int maxAlarmCount = _controllerData.Last().Alarm.Length;
                    int maxAnalogOutCount = _controllerData.Last().AnalogOut.Length;
                    int maxDigitalOutCount = _controllerData.Last().DigitalOut.Length;
                    csvContent.AppendLine($"TimeStamp;" +
                                            $"{string.Join(";", Enumerable.Range(1, maxValuesCount).Select(i => $"Values{i}"))};" +
                                            $"{string.Join(";", Enumerable.Range(1, maxSetpointsCount).Select(i => $"Setpoints{i}"))};" +
                                            $"{string.Join(";", Enumerable.Range(1, maxAutomaticCount).Select(i => $"Auto{i}"))};" +
                                            $"{string.Join(";", Enumerable.Range(1, maxActiveCount).Select(i => $"Active{i}"))};" +
                                            $"{string.Join(";", Enumerable.Range(1, maxAlarmCount).Select(i => $"Alarm{i}"))};" +
                                            $"{string.Join(";", Enumerable.Range(1, maxAnalogOutCount).Select(i => $"AnalogOut{i}"))};" +
                                            $"{string.Join(";", Enumerable.Range(1, maxDigitalOutCount).Select(i => $"DigitalOut{i}"))};" +
                                            $"DirectControl;" +
                                            $"ErrorCode");
                    // Actual Data
                    foreach (TControllerData data in _controllerData.Skip(1)) {
                        string values = string.Join(";", data.Values.Select(v => v.ToString(CultureInfo.InvariantCulture)));
                        string setpoints = string.Join(";", data.Setpoints.Select(v => v.ToString(CultureInfo.InvariantCulture)));
                        string automatic = string.Join(";", data.Automatic);
                        string active = string.Join(";", data.Active);
                        string alarm = string.Join(";", data.Alarm);
                        string analogOut = string.Join(";", data.AnalogOut);
                        string digitalOut = string.Join(";", data.DigitalOut.Select(b => b ? "1" : "0")); // Convert bool to 1 or 0
                        csvContent.AppendLine($"{data.TimeStamp};{values};{setpoints};{automatic};{active};{alarm};{analogOut};{digitalOut};{data.DirectControl};{data.ErrorCode}");
                    }
                }
                lock (_fileAccessLock) {
                    _FileWriterControllerData.WriteLines(csvContent);
                }
                LogEvent?.Invoke($"Controller data saved successfully to {_FileWriterControllerData.FileName}.",1);
            } catch (Exception ex) {
                LogEvent?.Invoke($"Error: Error saving controller data to {_FileWriterControllerData.FileName}. Error: {ex.Message}",0);
            }
        }

        public void SaveComputedDataCSV() {
            try {
                StringBuilder csvContent = new();
                // Header for ComputedValues
                csvContent.AppendLine("TimeStamp;Ethanol;H2S");
                // Actual Data
                lock (_listAccessLock) {
                    foreach (var data in _computedValues.Skip(1)) {
                        csvContent.AppendLine($"{data.TimeStamp};{data.Ethanol.ToString(CultureInfo.InvariantCulture)};{data.H2S.ToString(CultureInfo.InvariantCulture)}");
                    }
                }
                lock (_fileAccessLock) {
                    _FileWriterComputedData.WriteLines(csvContent);
                }
                LogEvent?.Invoke($"Computed GasSensor data saved successfully to {_FileWriterComputedData.FileName}.", 1);
            } catch (Exception ex) {
                LogEvent?.Invoke($"Error: Error saving computed GasSensor data to {_FileWriterComputedData.FileName}. Error: {ex.Message}", 0);
            }
        }
        #endregion

        #region data addition metods to add to dataStore and helper methods

        /// <summary>
        /// Adds a raw measurement as string to the _rawMeasurementData list using data from the provided JsonObject.
        /// </summary>
        /// <param name="controllerJSON">The JsonObject containing the relayed raw data.</param>
        /// <returns>True if the data was successfully added, otherwise false.</returns>
        public bool AddRawMesurementData(string message) {
            try {
                lock (_listAccessLock) {
                    _relayData.Add(new TRawData(message));
                }
                return true;
            } catch (Exception e) {
                LogEvent?.Invoke($"Error: Unable to parse message relay -> {e.Message}", 0);
                return false;
            }
        } 

        /// <summary>
        /// Adds a ControllerDatum object to the _controllerData list using data from the provided JsonObject.
        /// </summary>
        /// <param name="controllerJSON">The JsonObject containing controller data.</param>
        /// <returns>True if the data was successfully added, otherwise false.</returns>
        public bool AddControllerData(JsonObject controllerJSON) {
            if (!controllerJSON.ContainsKey("M")) {
                LogEvent?.Invoke("Error: Unable to create ControllerDatum: Missing 'M' in JSONObject.",0);
                return false;
            }
            try {
                ushort errorCode = controllerJSON["Error"]?.GetValue<ushort>() ?? 255;
                int[] measurementValues = ExtractArrayValues<int>(controllerJSON, "M", TControllerData.VALUES_SIZE);
                int[] setpointValues = ExtractArrayValues<int>(controllerJSON["Ctrl"]?.AsObject(), "SP", TControllerData.SETPOINTS_SIZE);
                ushort[] controllerAnalogOutputs = ExtractArrayValues<ushort>(controllerJSON["Ctrl"]?["Out"]?.AsObject(), "A", TControllerData.ANALOG_OUT_SIZE);
                bool[] controllerDigitalOutputs = ExtractBoolArrayFromInt(controllerJSON["Ctrl"]?["Out"]?.AsObject(), "D", TControllerData.DIGITAL_OUT_SIZE);
                bool[] controllerAlarms = ExtractBoolArrayFromInt(controllerJSON["Ctrl"]?.AsObject(), "Alarm", TControllerData.ALARM_SIZE);
                bool[] controllerControlActive = ExtractBoolArrayFromInt(controllerJSON["Ctrl"]?.AsObject(), "Act", TControllerData.ACTIVE_SIZE);
                bool[] controllerAutomationActive = ExtractBoolArrayFromInt(controllerJSON["Ctrl"]?.AsObject(), "Auto", TControllerData.AUTOMATIC_SIZE);

                TControllerData NewData = new(
                                    values: ConvertControllerValues(measurementValues),
                                    setpoints: ConvertControllerSetpoints(setpointValues),
                                    automatic: controllerAutomationActive,
                                    active: ConvertActiveValues(controllerControlActive),
                                    alarm: controllerAlarms,
                                    analogout: controllerAnalogOutputs,
                                    digitalout: controllerDigitalOutputs,
                                    directcontrol: controllerJSON["Ctrl"]?["Out"]?["dctrl"]?.GetValue<int>() == 1,
                                    errorcode: errorCode
                                    );
                lock (_listAccessLock) {
                    _controllerData.Add(NewData);
                    _LastControllerDatum = NewData;
                }
                if (errorCode != 0 ) {
                    LogEvent?.Invoke($"Warning: Controller reported errors: {(TControllerData.ControllerErrors)errorCode}", 0);
                    ControllerErrorReport?.Invoke((TControllerData.ControllerErrors)errorCode);
                }
            } catch (Exception e) {
                LogEvent?.Invoke($"Error: Unable to parse ControllerDatum -> {e.Message}", 0);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Adds a GasSensorDatum object to the _gasSensorData list using data from the provided JsonObject.
        /// </summary>
        /// <param name="gasSensorJSON">The JsonObject containing gas sensor data.</param>
        /// <returns>True if the data was successfully added, otherwise false.</returns>
        public bool AddGassensorData(JsonObject gasSensorJSON) {
            if (!gasSensorJSON.ContainsKey("mId")) {
                LogEvent?.Invoke("Error: Unable to create GasSensorDatum: Missing 'mId' in JSONObject.", 0);
                return false;
            }
            try {
                double[] vals = ExtractArrayValues<double>(gasSensorJSON, "Sen", TGasSensorData.VALUES_SIZE);
                double[] env = ExtractArrayValues<double>(gasSensorJSON, "Env", TGasSensorData.ENVIROMENT_SIZE);
                
                TGasSensorData newData = new(
                            id: gasSensorJSON["mId"]?.GetValue<int>(),
                            values: (double[])vals.Clone(),
                            enviroment: (double[])env.Clone(),
                            valvesetting: gasSensorJSON["Val"]?.GetValue<byte>(),
                            measurementcycle: gasSensorJSON["Sys"]?["Clc"]?.GetValue<int>(),
                            sensortime: gasSensorJSON["Sys"]?["t"]?.GetValue<int>(),
                            sensorstate: gasSensorJSON["Sys"]?["sId"]?.GetValue<byte>(),
                            sensorstatetime: gasSensorJSON["Sys"]?["st"]?.GetValue<int>()
                            );
                lock (_listAccessLock) {
                    _gasSensorData.Add(newData);
                    _LastGasSensorDatum = newData;
                }

            } catch (Exception e) {
                LogEvent?.Invoke($"Error: Unable to parse GasSensorDatum -> {e.Message}", 0);
                return false;
            }
            if (IsGasSensorCycleComplete()) {
                TComputedValuesData? EvaluationResult = EavluateCycleData();
                if (EvaluationResult != null) {
                    _computedValues.Add(EvaluationResult);
                    _LastComputedDatum = EvaluationResult;
                    GasSensorCycleComplete?.Invoke(EvaluationResult.Clone() as TComputedValuesData);
                }
            } 
            return true;
        }

        /// <summary>
        /// Helper method to extract array values from a given JsonObject into a proper C# array
        /// </summary>
        /// <typeparam name="T">type of data</typeparam>
        /// <param name="jsonObject">JsonObject containig the array</param>
        /// <param name="key">Json key pointing to the array in the JsonObject</param>
        /// <param name="length">expected lenght of the array</param>
        /// <returns>the extracted array as <b>T[]</b></returns>
        private static T[] ExtractArrayValues<T>(JsonObject? jsonObject, string key, int length) {
            T[] result = new T[length];
            if (jsonObject?.ContainsKey(key) ?? false) {
                var jsonArray = jsonObject[key]?.AsArray();
                if (jsonArray?.Count != length) {
                    throw new ArgumentException($"Lenght of <T>JsonArray {key} ({jsonArray?.Count}) does not match expected lenght ({length} of <T> array)!");
                }
                for (int i = 0; i < length; i++) {
                    result[i] = jsonArray[i].GetValue<T>();
                }
            }
            return result;
        }

        /// <summary>
        /// Helper method to extract boolean array values from a given JsonObject with an int array        
        /// </summary>
        /// <param name="jsonObject">JsonObject containig the int array</param>
        /// <param name="key">Json key pointing to the int array in the JsonObject</param>
        /// <param name="length">expected lenght of the array</param>
        /// <returns>the extracted bool array as <b>bool[]</b></returns>
        private static bool[] ExtractBoolArrayFromInt(JsonObject? jsonObject, string key, int length) {
            bool[] result = new bool[length];
            if (jsonObject?.ContainsKey(key) ?? false) {
                var jsonArray = jsonObject[key]?.AsArray();
                if (jsonArray?.Count != length) {
                    throw new ArgumentException($"Lenght of <int>JsonArray \"{key}\" ({jsonArray?.Count}) does not match expected lenght ({length}) of <bool> array!");
                }

                for (int i = 0; i < length; i++) {
                    result[i] = (jsonArray[i]?.GetValue<int>() ?? 0) == 1;
                }
            }
            return result;
        }
        #endregion

        #region gasSensor cycle detection and according data evaluation methods
        /// <summary>
        /// Method <b>IsGasSensorCycleComplete</b> checks if the last element of the ControllerData List belongs to a 
        /// new measurement cycle, by checking if there is an increment in the MeasurementCycle Property. This can be used 
        /// to trigger a cycle evaluation procedure for example. 
        /// </summary>
        public bool IsGasSensorCycleComplete() {
            if (_gasSensorData.Count < 3)
                {
                return false;
            }
            TGasSensorData lastElement = GetGasSensorDatum(-1);
            TGasSensorData secondLastElement = GetGasSensorDatum(-2);
            if (lastElement.MeasurementCycle > secondLastElement.MeasurementCycle) {
                _lastGasSensorCycle = secondLastElement.MeasurementCycle;
                return true;
            } else {
                return false;
            }
        }

        /// <summary>
        /// Fetches the last gassensor cycle measurement data and calculates the heigth of 
        /// the peaks for each of the 9 gas sensors by finding the maximum and minimum values 
        /// and calculating the difference.
        /// </summary>
        /// <returns>9 peak height values as <b>double[]</b></returns>
        private double[] GetLastCycleGasSensorPeakHeights() {
            //fetch last cycle data
            List<double[]> LastCycleGasSensorData = new();
            for (int i = 0; i < 9; i++) {
                LastCycleGasSensorData.Add(GetLastCycleGasSensorValues(i, _lastGasSensorCycle));
            }

            // determine max and min values and their positions in the according signal 
            IEnumerable<double> maxValues = LastCycleGasSensorData.Select(values => values.Max());
            IEnumerable<double> minValues = LastCycleGasSensorData.Select(values => values.Min());

            //determine the peak heights
            return maxValues.Zip(minValues, (max, min) => max - min).ToArray(); 
        }

        /// <summary>
        /// Computes the Ethanol concentration in the medium from the offgas of the reactor. 
        /// It uses the 9 peak heights of the individual gassensors. There are a lot of hardcoded calibration parameters in here, 
        /// maybe consider moving them into a user editable ressources text file?
        /// </summary>
        /// <param name="peakHeights">properly calculated peak heights of the 9 individual gassensors</param>
        /// <returns>Ethanol concentration in the liquid culture medium in g/L as <b>double</b></returns>
        private static double CalculateEthanolFromPeakHeights(double[] peakHeights) {
            if (peakHeights == null || peakHeights.Length != 9)
                {
                throw new ArgumentException("peakHeight must contain 9 elements and must not be null!");
            }

            // mean and stdandard deviation values for mean centering and normalizing the peakheights (in the context of the original calibration data) 
            double[] xmean = { 419.962962962963, 4320.6877394636, 464.583014048531, 1199.23627075351, 1947.29054916986, 1423.60600255428, 1215.5938697318, 4783.55044699872 };
            double[] xstd = { 286.756587510063, 1999.52479662924, 221.291975615196, 506.995209270786, 1132.13343087447, 701.432843858327, 668.672427384668, 2298.72129943355 };

            double[] meanCentered = peakHeights.Zip(xmean, (pHeight, xm) => pHeight - xm).ToArray();
            double[] normalized = meanCentered.Zip(xstd, (mCentered, xs) => mCentered / xs).ToArray();

            // Loading values for data projection into latent variable space
            double[] loadings = { 0.000278830889892849, -0.0001186180119868, 0.00085561116417757, 0.000252705273984966, 0.000669659446262931, -0.000115565357383454, -0.000123132779218994, 6.49453843878399e-05 };
            double Scores = normalized.Zip(loadings, (f, load) => f * load).Sum();

            // coefficients/parameters for (non)linear regression from the latent variables to the actual ethanol concentration
            double[] parameters = { 0.697695734815521, 536.806245267187, 122372.306520375 };

            parameters[0] = 0;

            double EthanolConcentration = parameters[0] + parameters[1] * Scores + parameters[2] * Scores * Scores;
            EthanolConcentration *= 2.25;
            EthanolConcentration = (EthanolConcentration < 0) ? 0 : EthanolConcentration; // ignore values smaller 0

            return EthanolConcentration;
        }

        /// <summary>
        /// Computes the H2S concentration in the offgas form the 9 peak heights of the individual gassensors
        /// This calculation is not fully implemented yet, as proper calibration was not yet performed, so this is just a rough approximation!
        /// </summary>
        /// <param name="peakHeights">properly calculated peak heights of the 9 individual gassensors</param>
        /// <returns>H2S gas concentration in ppm as <b>double</b></returns>
        private static double CalculateH2SFromPeakHeights(double[] peakHeights) {
            if (peakHeights == null || peakHeights.Length != 9)
                {
                throw new ArgumentException("peakHeight must contain 9 elements and must not be null!");
            }

            // calculate the average peakheight of the 8 first non-H2S related signals for comparison
            double avgeragePeakHeight = peakHeights.Take(8).Average();

            // H2S is estimated by comparin ist sensor signsl(peakheigth) against the other 8 signals
            double H2S = peakHeights[8] / avgeragePeakHeight * 10.0 - 10.0;
            H2S = (H2S < 0) ? 0 : H2S; // ignore values smaller 0

            return H2S;
        }

        /// <summary>
        /// After one gassensor measurement cycle is complete, evaluate the collected data and calculate/estimate
        /// Ethanol and H2S values.
        /// </summary>
        /// <returns>Estimated Ethanol and H2S values as <b>DataTypeComputedValues</b></returns>
        private TComputedValuesData? EavluateCycleData() {
            double ethanol = 0;
            double h2s = 0;
            try {
                double[] PeakHeights = GetLastCycleGasSensorPeakHeights();
                ethanol = CalculateEthanolFromPeakHeights(PeakHeights);
                h2s = CalculateH2SFromPeakHeights(PeakHeights);
            } catch (Exception ex) {
                LogEvent?.Invoke($"Error: GasSensor cycle evaluation error -> {ex.Message}", 0);
                return null;
            }
            return new TComputedValuesData(ethanol,h2s);
        }
        #endregion

        #region controller data value conversion helper methods
        /// <summary>
        /// Converts the raw integer measurement values from the controller to proper physical values.
        /// It also changes the order of elements, so that it is the same as for the UI elements
        /// </summary>
        private static double[] ConvertControllerValues(int[] controllerValues) {
            if (controllerValues.Length != 18) { 
                throw new ArgumentOutOfRangeException(nameof(controllerValues),"ControllerValues must contain 18 elements"); 
            }

            double[] cValues = ConvertControllerSetpoints(controllerValues[0..6]);

            return cValues.Concat(new double[] {
                controllerValues[12] / 10.0,
                controllerValues[10] / 10.0,
                controllerValues[15] / 10.0,
                controllerValues[4] / 10.0,
                controllerValues[8] / 10.0,
                controllerValues[9] / 10.0,
                controllerValues[7],
                controllerValues[6]
            }).ToArray();
        }

        /// <summary>
        /// Converts the raw integer setpoint data from the controller to actual physical values. 
        /// </summary>
        private static double[] ConvertControllerSetpoints(int[] controllerSetpoints) {

            const double MotorRpmConversionFactor = 3.84 / 10.24;
            const double AirflowConversionFactor = 4.5 / 10.24;

            if (controllerSetpoints.Length != 6) { 
                throw new ArgumentOutOfRangeException(nameof(controllerSetpoints), "Setpoints must contain 6 values"); 
            }

            double x = controllerSetpoints[3] / 10.24;

            return new double[] {
                ((int)(controllerSetpoints[0] * 8.04 + 125.8) / 10) * 10, // linear conversion from pressure difference to fluid level, also rounding to the next 10 L.
                controllerSetpoints[1] * MotorRpmConversionFactor, // linear conversion based on rough motor rpm measurements.
                controllerSetpoints[2] * AirflowConversionFactor, // linear conversion based on rough airflow measurements.
                ( x * 3 - x * x * 0.0076116 - x * x * x * 1.8243e-5 ), // Non-linear (polynomial) conversion derived from manual calibration data.
                controllerSetpoints[4] / 10.0,  // Temperature converion. Just divide by 10 since the PLC uses Integers to send fixed point values.
                controllerSetpoints[5] / 10.0  // Concentration converion. Just divide by 10 since the PLC uses Integers to send fixed point values.
            };
        }

        /// <summary>
        /// Converts the Active States from the Controller (of which there are 7) into the 6 general variables that state 
        /// whether one of the 6 control shemes are actually active (be it in manual or automatic mode)
        /// This is necessary as for the motor control, there are two values indicating if the motor is turning clockwise or counterclockwise
        /// </summary>
        private static bool[] ConvertActiveValues(bool[] activeValues) {
            if (activeValues.Length != 7) { 
                throw new ArgumentOutOfRangeException(nameof(activeValues), "Control active signal must contain 7 values"); 
            }

            return new bool[] {
                activeValues[0],
                activeValues[1] || activeValues[2], // value 1 indicates cw rotaion, value 2 ccw rotation. We dont care about the direction
                activeValues[3],
                activeValues[4],
                activeValues[5],
                activeValues[6]
            };
        }
        #endregion
    }
}
