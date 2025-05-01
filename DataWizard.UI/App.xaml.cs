using Microsoft.UI.Xaml;

namespace DataWizard.UI
{
    public partial class App : Application
    {
        public static Window Window { get; private set; } // Global window reference

        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            Window = new Window();
            Window.Content = new MainWindow(); // MainWindow sekarang adalah Page
            Window.Activate();
        }
    }
}
