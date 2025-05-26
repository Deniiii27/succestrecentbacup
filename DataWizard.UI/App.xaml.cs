using Microsoft.UI.Xaml;
using DataWizard.Core.Services;
using System;
using System.IO;

namespace DataWizard.UI
{
    public partial class App : Application
    {
        public static Window Window { get; private set; }

        public App()
        {
            this.InitializeComponent();
            InitializeSupabase();
        }

        private void InitializeSupabase()
        {
            try
            {
                var envPath = Path.Combine(AppContext.BaseDirectory, ".env");
                if (File.Exists(envPath))
                {
                    foreach (var line in File.ReadAllLines(envPath))
                    {
                        var parts = line.Split('=', 2);
                        if (parts.Length == 2)
                        {
                            var key = parts[0].Trim();
                            var value = parts[1].Trim();
                            
                            if (key == "SUPABASE_URL")
                                SupabaseConfig.Url = value;
                            else if (key == "SUPABASE_ANON_KEY")
                                SupabaseConfig.AnonKey = value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error or show message
                System.Diagnostics.Debug.WriteLine($"Error initializing Supabase: {ex.Message}");
            }
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            Window = new MainWindow();
            Window.Activate();
        }
    }
}