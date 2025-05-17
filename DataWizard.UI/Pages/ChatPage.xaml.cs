using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using System;
using System.IO;
using System.Threading.Tasks;
using DataWizard.Core.Services;
using System.Collections.Generic;
using DataWizard.UI.Services;
using System.Diagnostics; // Untuk Stopwatch
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI.Text;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Shapes;
using System.Linq;
using System.Data.SqlClient;

namespace DataWizard.UI.Pages
{
    public sealed partial class ChatPage : Page
    {
        private string selectedFilePath = "";
        private readonly string outputTextPath = @"C:\Project PBTGM\DataSample\hasil_output.txt";
        private readonly DatabaseService _dbService;
        private int _currentUserId = 1; // Temporary hardcoded user ID for testing
        private Stopwatch _processTimer; // Untuk mengukur waktu proses

        public ChatPage()
        {
            this.InitializeComponent();
            _dbService = new DatabaseService();
            PromptBox.TextChanged += PromptBox_TextChanged;
            LoadUserPreferences();
            _processTimer = new Stopwatch();
        }

        // ... (kode lainnya tetap sama sampai RunButton_Click)
	private async void LoadUserPreferences()
{
    try
    {
        string preferredFormat = await _dbService.GetUserPreferredFormatAsync(_currentUserId);

        // Reset format buttons
        WordFormatButton.Style = Resources["DefaultFormatButtonStyle"] as Style;
        ExcelFormatButton.Style = Resources["DefaultFormatButtonStyle"] as Style;

        // Set preferred format
        if (preferredFormat == "word")
        {
            WordFormatButton.Style = Resources["SelectedFormatButtonStyle"] as Style;
            OutputFormatBox.SelectedIndex = 2;
        }
        else // Default to Excel
        {
            ExcelFormatButton.Style = Resources["SelectedFormatButtonStyle"] as Style;
            OutputFormatBox.SelectedIndex = 1;
        }
    }
    catch (Exception ex)
    {
        // Silently fail and use default format
        ExcelFormatButton.Style = Resources["SelectedFormatButtonStyle"] as Style;
        OutputFormatBox.SelectedIndex = 1;
    }
}

private async Task ShowDialogAsync(string title, string content)
{
    ContentDialog dialog = new ContentDialog
    {
        Title = title,
        Content = content,
        CloseButtonText = "OK",
        XamlRoot = this.XamlRoot
    };
    await dialog.ShowAsync();
}

private void PromptBox_TextChanged(object sender, TextChangedEventArgs e)
{
    CharCountText.Text = $"{PromptBox.Text.Length}/1000";
}

private async Task<bool> SelectFileAsync()
{
    var picker = new FileOpenPicker();
    picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
    picker.FileTypeFilter.Add(".xlsx");
    picker.FileTypeFilter.Add(".xls");
    picker.FileTypeFilter.Add(".csv");
    picker.FileTypeFilter.Add(".docx");
    picker.FileTypeFilter.Add(".pdf");
    picker.FileTypeFilter.Add(".png");
    picker.FileTypeFilter.Add(".jpg");
    picker.FileTypeFilter.Add(".jpeg");

    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Window);
    WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

    var file = await picker.PickSingleFileAsync();
    if (file != null)
    {
        selectedFilePath = file.Path;
        OutputBox.Text = $"File dipilih: {selectedFilePath}";
        return true;
    }
    return false;
}

private async void SelectFileButton_Click(object sender, RoutedEventArgs e)
{
    await SelectFileAsync();
}

        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            string prompt = PromptBox.Text.Trim();
            string outputFormat = (OutputFormatBox.SelectedItem as ComboBoxItem)?.Content?.ToString().ToLower() ?? "txt";
            string mode = (ModeBox.SelectedItem as ComboBoxItem)?.Content?.ToString().ToLower() ?? "file";

            if ((mode != "prompt-only" && string.IsNullOrWhiteSpace(selectedFilePath)) || string.IsNullOrWhiteSpace(prompt))
            {
                await ShowDialogAsync("Validation Error", "Harap pilih file (kecuali prompt-only) dan masukkan prompt terlebih dahulu.");
                return;
            }

            // Mulai mengukur waktu proses
            _processTimer.Restart();

            // Simpan preferensi format pengguna
            await _dbService.SaveUserPreferredFormatAsync(_currentUserId, outputFormat);

            WelcomePanel.Visibility = Visibility.Collapsed;
            AnswerBox.Visibility = Visibility.Visible;
            OutputBox.Text = "Memproses data... Mohon tunggu.";

            // Dapatkan tipe file input sebagai ID integer
            int inputFileTypeId = !string.IsNullOrEmpty(selectedFilePath) ?
                await _dbService.GetFileTypeId(System.IO.Path.GetExtension(selectedFilePath).TrimStart('.').ToUpper()) :
                await _dbService.GetFileTypeId("PROMPT");

            // Dapatkan output format ID
            int outputFormatId = outputFormat == "word" ?
                await _dbService.GetOutputFormatId("Word") :
                await _dbService.GetOutputFormatId("Excel");

            // Catat ke history sebelum proses dimulai
            int historyId = await _dbService.LogHistoryAsync(
                _currentUserId,
                inputFileTypeId,
                outputFormatId,
                prompt,
                mode);

            string result = await PythonRunner.RunPythonScriptAsync(
                mode == "prompt-only" ? "none" : selectedFilePath,
                outputTextPath,
                prompt,
                outputFormat,
                mode
            );

            // Hentikan timer dan dapatkan waktu proses
            _processTimer.Stop();
            int processingTimeMs = (int)_processTimer.ElapsedMilliseconds;

            string outputFileName = string.Empty;
            string outputFilePath = string.Empty;

            if (result == "Success" && File.Exists(outputTextPath))
            {
                string hasil = File.ReadAllText(outputTextPath);
                OutputBox.Text = hasil;

                string parsedExcelPath = PythonRunner.GetParsedExcelPath(outputTextPath);

                if (File.Exists(parsedExcelPath))
                {
                    outputFilePath = parsedExcelPath;
                    outputFileName = System.IO.Path.GetFileName(parsedExcelPath);
                    ResultFileText.Text = outputFileName;
                }
                else if (outputFormat == "excel")
                {
                    OutputBox.Text += "\n\nFile hasil parsing Excel tidak ditemukan.";
                }

                // Update history dengan waktu proses
                await _dbService.UpdateHistoryProcessingTimeAsync(historyId, processingTimeMs);

                // Jika ada file output, simpan ke tabel OutputFile
                if (!string.IsNullOrEmpty(outputFilePath))
                {
                    FileInfo fileInfo = new FileInfo(outputFilePath);
                    await _dbService.LogOutputFileAsync(
                        historyId,
                        outputFileName,
                        outputFilePath,
                        fileInfo.Length);
                }
            }
            else
            {
                OutputBox.Text = $"Gagal: {result}";
                // Jika gagal, update history dengan status gagal
                await _dbService.UpdateHistoryStatusAsync(historyId, false, processingTimeMs);
            }
        }

        // ... (kode lainnya tetap sama)
        private async void FileToFileButton_Click(object sender, RoutedEventArgs e)
{
    ModeBox.SelectedIndex = 0;
    await SelectFileAsync();
}

private async void PromptToFileButton_Click(object sender, RoutedEventArgs e)
{
    ModeBox.SelectedIndex = 2;
    await ShowDialogAsync("Reminder", "Please select your output format (Word or Excel) before proceeding.");
    PromptBox.Focus(FocusState.Programmatic);
}

private async void OcrToFileButton_Click(object sender, RoutedEventArgs e)
{
    ModeBox.SelectedIndex = 1;
    await SelectFileAsync();
}

        private async void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug.WriteLine($"Attempting to load history for user {_currentUserId}...");

                var historyData = await _dbService.GetRecentHistoryAsync(_currentUserId, 10);
                Debug.WriteLine($"Successfully retrieved {historyData.Count} history items");

                // ... [kode untuk menampilkan dialog tetap sama] ...
                var stackPanel = new StackPanel { Spacing = 10 };

                if (historyData.Count == 0)
                {
                    stackPanel.Children.Add(new TextBlock
                    {
                        Text = $"Belum ada riwayat konversi untuk user ID: {_currentUserId}",
                        FontSize = 14,
                        TextWrapping = TextWrapping.Wrap
                    });
                }
                else
                {
                    // Add header
                    stackPanel.Children.Add(new TextBlock
                    {
                        Text = "Riwayat Konversi Terakhir",
                        FontWeight = FontWeights.Bold,
                        FontSize = 16,
                        Margin = new Thickness(0, 0, 0, 10)
                    });

                    // Add each history item
                    foreach (var history in historyData)
                    {
                        Debug.WriteLine($"Processing history item: {history.HistoryId}");

                        // Create container for each history item
                        var itemContainer = new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 12,
                            Margin = new Thickness(0, 4, 0, 4)
                        };

                        // Add icon based on output format
                        var formatIcon = new Microsoft.UI.Xaml.Controls.Image
                        {
                            Width = 28,
                            Height = 28,
                            Source = history.OutputFormat == "Word" ?
                                new BitmapImage(new Uri("ms-appx:///Assets/Microsoft Word 2024.png")) :
                                new BitmapImage(new Uri("ms-appx:///Assets/Microsoft Excel 2025.png"))
                        };
                        itemContainer.Children.Add(formatIcon);

                        // Add conversion info
                        var conversionInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

                        conversionInfo.Children.Add(new TextBlock
                        {
                            Text = $"{history.InputType} ? {history.OutputFormat}",
                            FontSize = 14,
                            FontWeight = history.IsSuccess ? FontWeights.Normal : FontWeights.SemiBold,
                            Foreground = history.IsSuccess ?
                                new SolidColorBrush(Microsoft.UI.Colors.Black) :
                                new SolidColorBrush(Microsoft.UI.Colors.Red)
                        });

                        conversionInfo.Children.Add(new TextBlock
                        {
                            Text = $"{history.ProcessDate:dd/MM/yyyy HH:mm} • {history.ProcessingTime}ms",
                            FontSize = 12,
                            Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray)
                        });

                        itemContainer.Children.Add(conversionInfo);
                        stackPanel.Children.Add(itemContainer);

                        // Add separator (except after last item)
                        if (history != historyData.Last())
                        {
                            stackPanel.Children.Add(new Rectangle
                            {
                                Height = 1,
                                Fill = new SolidColorBrush(Microsoft.UI.Colors.LightGray),
                                Margin = new Thickness(0, 8, 0, 8)
                            });
                        }
                    }
                }

                // 3. Create and show dialog
                var dialog = new ContentDialog
                {
                    Title = "Riwayat Konversi",
                    Content = new ScrollViewer
                    {
                        Content = stackPanel,
                        VerticalScrollMode = ScrollMode.Auto,
                        HorizontalScrollMode = ScrollMode.Disabled,
                        MaxHeight = 500,
                        Padding = new Thickness(0, 0, 10, 0) // Add right padding for scrollbar
                    },
                    CloseButtonText = "Tutup",
                    XamlRoot = this.Content.XamlRoot,
                    DefaultButton = ContentDialogButton.Close
                };

                // 4. Add debug info to output window
                Debug.WriteLine("Showing history dialog");
                await dialog.ShowAsync();
                Debug.WriteLine("History dialog closed");
            }
            catch (SqlException sqlEx)
            {
                Debug.WriteLine($"SQL Error loading history: {sqlEx.ToString()}");
                await ShowDialogAsync("Database Error",
                    $"Terjadi kesalahan database:\n{sqlEx.Message}\n\n" +
                    $"Kode Error: {sqlEx.Number}\n" +
                    $"Silakan hubungi administrator.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"General Error loading history: {ex.ToString()}");
                await ShowDialogAsync("Error",
                    $"Gagal memuat riwayat:\n{ex.Message}\n\n" +
                    $"User ID: {_currentUserId}\n" +
                    $"Silakan cek log untuk detail lebih lanjut.");
            }
        }
        private void CheckCurrentUserId()
        {
            Debug.WriteLine($"Current User ID: {_currentUserId}");
            // Atau tampilkan di UI sementara
            OutputBox.Text = $"Current User ID: {_currentUserId}";
        }
        private async void OutputFormatButton_Click(object sender, RoutedEventArgs e)
{
    Button clickedButton = sender as Button;
    string format = clickedButton.Tag.ToString();

    WordFormatButton.Style = Resources["DefaultFormatButtonStyle"] as Style;
    ExcelFormatButton.Style = Resources["DefaultFormatButtonStyle"] as Style;

    clickedButton.Style = Resources["SelectedFormatButtonStyle"] as Style;
    OutputFormatBox.SelectedIndex = format == "word" ? 2 : 1;

    // Save user's format preference
    await _dbService.SaveUserPreferredFormatAsync(_currentUserId, format);
}

private void RefreshPromptButton_Click(object sender, RoutedEventArgs e)
{
    PromptBox.Text = "";
    selectedFilePath = "";
    OutputBox.Text = "";
    OutputFormatBox.SelectedIndex = 0;
    ModeBox.SelectedIndex = 0;

    WordFormatButton.Style = Resources["DefaultFormatButtonStyle"] as Style;
    ExcelFormatButton.Style = Resources["DefaultFormatButtonStyle"] as Style;

    WelcomePanel.Visibility = Visibility.Visible;
    AnswerBox.Visibility = Visibility.Collapsed;
}
private void HomeButton_Click(object sender, RoutedEventArgs e)
{
    // Navigate to the HomePage
    this.Frame.Navigate(typeof(DataWizard.UI.Pages.HomePage));
}

private async void AddAttachmentButton_Click(object sender, RoutedEventArgs e)
{
    await SelectFileAsync();
}

private async void UseImageButton_Click(object sender, RoutedEventArgs e)
{
    ModeBox.SelectedIndex = 1;
    await SelectFileAsync();
}

private async void SaveFileButton_Click(object sender, RoutedEventArgs e)
{
    var savePicker = new FileSavePicker();
    savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
    savePicker.FileTypeChoices.Add("Excel Files", new List<string>() { ".xlsx" });
    savePicker.FileTypeChoices.Add("Word Documents", new List<string>() { ".docx" });
    savePicker.SuggestedFileName = ResultFileText.Text;

    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Window);
    WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

    var file = await savePicker.PickSaveFileAsync();
    if (file != null)
    {
        try
        {
            // Get the current folder ID (you'll need to implement this based on your UI)
            int currentFolderId = 1; // Temporary hardcoded folder ID for testing

            // Save file reference to database
            await _dbService.SaveFileToFolderAsync(_currentUserId, currentFolderId,
                file.Name, file.Path);

            OutputBox.Text = $"File saved to: {file.Path}";
        }
        catch (Exception ex)
        {
            await ShowDialogAsync("Error", $"Error saving file: {ex.Message}");
        }
    }
}

    }
}