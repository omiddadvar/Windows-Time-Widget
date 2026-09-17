using System.Windows;
using System.Windows.Input;
using WindowsTimeWidget.ViewModels;

namespace WindowsTimeWidget.Views.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm;

        public MainWindow(MainViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = vm;
            _vm.Start();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_vm.Settings.WindowLeft is double left && _vm.Settings.WindowTop is double top)
            {
                Left = left;
                Top = top;
            }
            else
            {
                var wa = SystemParameters.WorkArea;
                Left = wa.Right - Width - 200;
                Top = wa.Top + 30;
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                try { DragMove(); }
                catch { }
            }
        }

        private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            OpenSettings();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _vm.SaveSettings();
            Close();
        }

        private void OpenSettings()
        {
            var settingsWindow = App.Services.GetService(typeof(SettingsWindow)) as SettingsWindow;
            if (settingsWindow is null) return;

            settingsWindow.Owner = this;
            settingsWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            if (settingsWindow.ShowDialog() == true && settingsWindow.Result is { } updated)
            {
                _vm.ApplySettings(updated);
            }
        }

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _vm.Settings.WindowLeft = Left;
            _vm.Settings.WindowTop = Top;
            _vm.SaveSettings();
            _vm.Stop();
            _vm.Dispose();
        }
        private void Window_LocationChanged(object sender, EventArgs e)
        {
            _vm.Settings.WindowLeft = Left;
            _vm.Settings.WindowTop = Top;
            _vm.SaveSettings();
        }
    }
}
