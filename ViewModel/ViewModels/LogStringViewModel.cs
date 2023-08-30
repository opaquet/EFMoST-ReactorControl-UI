using Core.Modules.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViewModel.ViewModels
{
    public class LogStringViewModel : BaseViewModel, INotifyPropertyChanged, IDisposable {

        public string LogString { get; private set; } = string.Empty;

        public int EntryCount { get; private set; } = 0;

        private IEventLogger _logger;
        private Action _logAction;

        public LogStringViewModel(IEventLogger logger, int centerWidth ) {

            _logger = logger;
            LogString = logger.LogString(50, 10, centerWidth);
            _logAction = () => {
                LogString = logger.LogString(50, 10, centerWidth);
                EntryCount = logger.Count;
            };
            logger.LogUpdated += _logAction;
        }

        public override void Dispose() {
            _logger.LogUpdated -= _logAction;
            GC.SuppressFinalize(this);
        }
    }
}
