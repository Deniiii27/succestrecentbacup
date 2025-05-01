using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using System;
using System.Threading.Tasks;
using System.IO;
using DataWizard.Core.Services;

namespace DataWizard.UI
{
    public sealed partial class MainWindow : Page
    {
        private string selectedExcelPath = "";
        private readonly string outputTextPath = @"C:\Project PBTGM\DataSample\hasil_output.txt"; // fix path sementara

        public MainWindow()
        {
            this.InitializeComponent();
        }

        private async void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add(".xlsx");

            // Dapatkan window handle dari root AppWindow
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Window); // <== Tambahkan property Window di App.xaml.cs
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                selectedExcelPath = file.Path;
                OutputBox.Text = $"File dipilih: {selectedExcelPath}";
            }
        }

        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(selectedExcelPath) || string.IsNullOrWhiteSpace(PromptBox.Text))
            {
                OutputBox.Text = "Harap pilih file dan masukkan prompt terlebih dahulu.";
                return;
            }

            OutputBox.Text = "Memproses data... Mohon tunggu.";

            string result = await PythonRunner.RunPythonScriptAsync(selectedExcelPath, outputTextPath, PromptBox.Text);

            if (File.Exists(outputTextPath))
            {
                string hasil = File.ReadAllText(outputTextPath);
                OutputBox.Text = hasil;
            }
            else
            {
                OutputBox.Text = $"Gagal: {result}";
            }
        }
    }
}
