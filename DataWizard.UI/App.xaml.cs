using Microsoft.UI.Xaml;

namespace DataWizard.UI
{
    public partial class App : Application
    {
        public static Window Window { get; private set; }

        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            Window = new MainWindow();
            Window.Activate();
        }
    }
}