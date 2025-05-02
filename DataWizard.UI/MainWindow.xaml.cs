using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using System;
using System.IO;
using System.Threading.Tasks;
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

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Window);
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

            if (result == "Success" && File.Exists(outputTextPath))
            {
                string hasil = File.ReadAllText(outputTextPath);
                OutputBox.Text = hasil;

                // Cek file hasil parsing
                string parsedExcelPath = PythonRunner.GetParsedExcelPath(outputTextPath);
                if (File.Exists(parsedExcelPath))
                {
                    OutputBox.Text += $"\n\n✔ File hasil parsing tersimpan di:\n{parsedExcelPath}";
                }
                else
                {
                    OutputBox.Text += "\n\n⚠ File hasil parsing tidak ditemukan.";
                }
            }
            else
            {
                OutputBox.Text = $"Gagal: {result}";
            }
        }
    }
}
