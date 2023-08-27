using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;

namespace UI.CommandExtensions
{
    public static class EventToCommandExtensions {
        public static ICommand GetMouseDownCommand(DependencyObject obj) {
            return (ICommand) obj.GetValue(MouseDownCommandProperty);
        }

        public static void SetMouseDownCommand(DependencyObject obj, ICommand value) {
            obj.SetValue(MouseDownCommandProperty, value);
        }

        public static readonly DependencyProperty MouseDownCommandProperty =
            DependencyProperty.RegisterAttached("MouseDownCommand", typeof(ICommand), typeof(EventToCommandExtensions), new UIPropertyMetadata(null, OnMouseDownCommandChanged));

        private static void OnMouseDownCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is UIElement element) {
                element.MouseDown += (sender, args) => {
                    var command = GetMouseDownCommand(d);
                    if (command != null && command.CanExecute(args)) { 
                        Task.Run(() => Console.Beep(1800, 50));
                        command.Execute(args);
                    }
                };
            }
        }
    }
}
