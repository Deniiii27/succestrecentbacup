using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DataWizard.UI.Pages;

namespace DataWizard.UI
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            ContentFrame.Navigate(typeof(LoginPage));
        }
    }
}