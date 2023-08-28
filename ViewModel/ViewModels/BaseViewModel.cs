using PropertyChanged;
using System.ComponentModel;

namespace ViewModel.ViewModels
{

    [AddINotifyPropertyChangedInterface]
    public abstract class BaseViewModel : INotifyPropertyChanged, IDisposable {
        public event PropertyChangedEventHandler? PropertyChanged = (sender, e) => { };
        protected void InvokePropertyChanged(object sender, PropertyChangedEventArgs e) {
            PropertyChanged?.Invoke(sender, e);
        }

        public virtual void Dispose() {
            PropertyChanged = null;
            GC.SuppressFinalize(this);
        }
    }
}