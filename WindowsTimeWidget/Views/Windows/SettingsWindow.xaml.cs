using System.Windows;
using WindowsTimeWidget.Models;
using WindowsTimeWidget.ViewModels;

namespace WindowsTimeWidget.Views.Windows
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly SettingsViewModel _vm;
        public WidgetSettings? Result { get; private set; }

        public SettingsWindow(SettingsViewModel vm)
        {
            InitializeComponent();

            _vm = vm;
            DataContext = vm;

            _vm.SaveRequested += settings =>
            {
                Result = settings;
                DialogResult = true;
            };
            _vm.CloseRequested += () =>
            {
                if (DialogResult is null) DialogResult = false;
            };
        }
    }
}
