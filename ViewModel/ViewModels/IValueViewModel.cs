using System.ComponentModel;
using System.Windows.Input;

namespace ViewModel.ViewModels {
    public interface IValueViewModel<T> : IDisposable, INotifyPropertyChanged {
        T Value { get; }
        T MaxValue { get; }
        T MinValue { get; }
        string ValueString { get; }
        bool IsValueVisible { get; }
        string IndicatingColor { get; }
        bool IsIndicating { get; }
        string Name { get; }
        string Unit { get; }
        bool IsNameVisible { get; }
        string Information { get; }
        void SetValue(T value);
    }
}