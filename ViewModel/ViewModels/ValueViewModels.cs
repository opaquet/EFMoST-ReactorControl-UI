using System.Windows.Input;
using ViewModel.Commands;

namespace ViewModel.ViewModels {
    public enum ValueChangeType {
        AddAndSubtract,
        MultiplyAndDivide
    }
    public class ValueViewModel : BaseViewModel, IValueViewModel<double> {
        public double Value { get; protected set; }
        public bool IsValueVisible { get; protected set; }
        public string Name { get; protected set; }
        public string Unit { get; protected set; }
        public bool IsNameVisible { get; protected set; }
        public string Information { get; protected set; }
        public double MaxValue { get; private set; }
        public double MinValue { get; private set; }
        public string ValueString { get => Math.Round(Value, DecimalPlaces).ToString($"F{DecimalPlaces}"); }
        public string IndicatingColor {
            get {
                return Value switch {
                    < 0 => "#ff0000",
                    < 1 => "#fafafa",
                    < 2 => "#f0f040",
                    < 3 => "#ff8040",
                    _ => "#ffffff"
                };
            }
        }

        public string IndicatingColorBool {
            get {
                return Value switch {
                    0 => "#fafafa",
                    _ => "#ff8040"
                };
            }
        }
        public int DecimalPlaces { get; private set; }
        public bool IsIndicating { get => Value > 1; }
        public ValueChangeType ChangeType { get; private set; }
        public double ChangeAmount { get; private set; }
        public bool IsChangeAllowed { get; private set; }
        public void SetValue(double value) {
            if (Value != value) { Value = value; }
            _changeBaseValue?.Invoke(Value);
        }
        public void Increase() {
            switch (ChangeType) {
                case ValueChangeType.AddAndSubtract:
                    Value += ChangeAmount;
                    break;
                case ValueChangeType.MultiplyAndDivide:
                    Value *= ChangeAmount;
                    break;
            }
            _changeBaseValue?.Invoke(Value);
        }
        public void Decrease() {
            switch (ChangeType) {
                case ValueChangeType.AddAndSubtract:
                    Value -= ChangeAmount;
                    break;
                case ValueChangeType.MultiplyAndDivide:
                    Value /= ChangeAmount;
                    break;
            }
            _changeBaseValue?.Invoke(Value);
        }

        private Action<double>? _changeBaseValue;

        public ValueViewModel(double value = 0, bool isValueVisible = true, string name = "Name", string unit = "", bool isNameVisible = false, string information = "", 
                               double maxValue = 100, double minValue = 0, ValueChangeType changeType = ValueChangeType.MultiplyAndDivide, double changeAmount = 1, 
                               bool changeAllowed = false, int decimalPlaces = 0, Action<double>? baseValueUpdateAction = null) {
            MaxValue = maxValue;
            MinValue = minValue;
            ChangeType = changeType;
            ChangeAmount = changeAmount;
            IsChangeAllowed = changeAllowed;
            DecimalPlaces = decimalPlaces;
            Value = value;
            IsValueVisible = isValueVisible;
            Name = name ?? string.Empty;
            Unit = unit ?? string.Empty;
            IsNameVisible = isNameVisible;
            Information = information ?? string.Empty;
            _changeBaseValue = baseValueUpdateAction;
        }
    }
}