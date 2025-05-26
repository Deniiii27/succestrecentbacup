using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using Supabase;
using Supabase.Gotrue;
using Postgrest.Models;
using Postgrest.Responses;

namespace DataWizard.UI.Services
{
    public class DatabaseService
    {
        private readonly Supabase.Client _supabaseClient;

        public DatabaseService()
        {
            var url = Environment.GetEnvironmentVariable("SUPABASE_URL") ?? "";
            var key = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY") ?? "";
            
            var options = new Supabase.SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = true
            };

            _supabaseClient = new Supabase.Client(url, key, options);
        }

        public async Task<(bool success, string error)> ValidateUserCredentialsAsync(string username, string password)
        {
            try
            {
                var session = await _supabaseClient.Auth.SignIn(username, password);
                if (session?.User != null)
                {
                    return (true, null);
                }
                return (false, "Invalid credentials");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<(bool success, string error)> CreateUserAsync(string username, string password, string email, string fullName)
        {
            try
            {
                var response = await _supabaseClient.Auth.SignUp(email, password);
                if (response?.User != null)
                {
                    return (true, null);
                }
                return (false, "Failed to create user");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<List<OutputFile>> GetRecentFilesAsync(string userId, int count = 4)
        {
            var files = new List<OutputFile>();
            try
            {
                var response = await _supabaseClient
                    .From<OutputFile>()
                    .Select("*, history!inner(*)")
                    .Match(new { history = new { user_id = userId } })
                    .Order("created_at", Postgrest.Constants.Ordering.Descending)
                    .Limit(count)
                    .Get();

                foreach (var file in response.Models)
                {
                    files.Add(file);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching recent files: {ex.Message}");
            }
            return files;
        }

        public async Task<List<Folder>> GetUserFoldersAsync(string userId, int count = 4)
        {
            var folders = new List<Folder>();
            try
            {
                var response = await _supabaseClient
                    .From<Folder>()
                    .Select("*")
                    .Match(new { user_id = userId })
                    .Order("updated_at", Postgrest.Constants.Ordering.Descending)
                    .Limit(count)
                    .Get();

                foreach (var folder in response.Models)
                {
                    folders.Add(folder);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching folders: {ex.Message}");
            }
            return folders;
        }

        public async Task<List<ChartData>> GetFileTypeStatsAsync(string userId)
        {
            var stats = new List<ChartData>();
            try
            {
                var response = await _supabaseClient
                    .From<InputFileTypeStats>()
                    .Select("*")
                    .Match(new { user_id = userId })
                    .Get();

                foreach (var stat in response.Models)
                {
                    stats.Add(new ChartData
                    {
                        Label = stat.FileType,
                        Value = stat.UsageCount
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching file type stats: {ex.Message}");
            }
            return stats;
        }

        public async Task<string> GetUserPreferredFormatAsync(string userId)
        {
            try
            {
                var response = await _supabaseClient
                    .From<OutputFormatPreference>()
                    .Select("format")
                    .Match(new { user_id = userId })
                    .Single()
                    .Get();

                return response?.Format ?? "Excel";
            }
            catch
            {
                return "Excel";
            }
        }

        public async Task SaveUserPreferredFormatAsync(string userId, string format)
        {
            try
            {
                await _supabaseClient
                    .From<OutputFormatPreference>()
                    .Upsert(new { user_id = userId, format = format, updated_at = DateTime.UtcNow })
                    .Execute();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving user format preference: {ex.Message}");
            }
        }

        public async Task<int> LogHistoryAsync(string userId, int inputFileTypeId, int outputFormatId, string prompt, string processType)
        {
            try
            {
                var response = await _supabaseClient
                    .From<History>()
                    .Insert(new History
                    {
                        UserId = userId,
                        InputFileTypeId = inputFileTypeId,
                        OutputFormatId = outputFormatId,
                        PromptText = prompt,
                        ProcessType = processType,
                        ProcessDate = DateTime.UtcNow
                    })
                    .Single()
                    .Execute();

                return response?.Id ?? -1;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error logging history: {ex.Message}");
                return -1;
            }
        }

        public async Task UpdateHistoryProcessingTimeAsync(string historyId, int processingTimeMs)
        {
            try
            {
                await _supabaseClient
                    .From<History>()
                    .Update(new { processing_time = processingTimeMs })
                    .Match(new { id = historyId })
                    .Execute();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating history processing time: {ex.Message}");
            }
        }

        public async Task UpdateHistoryStatusAsync(string historyId, bool isSuccess, int processingTimeMs)
        {
            try
            {
                await _supabaseClient
                    .From<History>()
                    .Update(new { is_success = isSuccess, processing_time = processingTimeMs })
                    .Match(new { id = historyId })
                    .Execute();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating history status: {ex.Message}");
            }
        }

        public async Task LogOutputFileAsync(string historyId, string fileName, string filePath, long fileSize)
        {
            try
            {
                await _supabaseClient
                    .From<OutputFile>()
                    .Insert(new OutputFile
                    {
                        HistoryId = historyId,
                        Name = fileName,
                        Path = filePath,
                        Size = fileSize,
                        CreatedAt = DateTime.UtcNow
                    })
                    .Execute();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error logging output file: {ex.Message}");
            }
        }

        public async Task<int> GetFileTypeId(string typeName)
        {
            try
            {
                var response = await _supabaseClient
                    .From<FileType>()
                    .Select("id")
                    .Match(new { name = typeName })
                    .Single()
                    .Get();

                return response?.Id ?? await GetFileTypeId("OTHER");
            }
            catch
            {
                return await GetFileTypeId("OTHER");
            }
        }

        public async Task<int> GetOutputFormatId(string formatName)
        {
            try
            {
                var response = await _supabaseClient
                    .From<OutputFormat>()
                    .Select("id")
                    .Match(new { name = formatName })
                    .Single()
                    .Get();

                return response?.Id ?? 1;
            }
            catch
            {
                return 1;
            }
        }

        public async Task<List<HistoryItem>> GetRecentHistoryAsync(string userId, int count)
        {
            var historyList = new List<HistoryItem>();
            try
            {
                var response = await _supabaseClient
                    .From<History>()
                    .Select("*, file_types(*), output_formats(*)")
                    .Match(new { user_id = userId })
                    .Order("process_date", Postgrest.Constants.Ordering.Descending)
                    .Limit(count)
                    .Get();

                foreach (var item in response.Models)
                {
                    historyList.Add(new HistoryItem
                    {
                        HistoryId = item.Id,
                        InputType = item.FileType?.Name ?? "Unknown",
                        OutputFormat = item.OutputFormat?.Name ?? "Unknown",
                        ProcessDate = item.ProcessDate,
                        ProcessingTime = item.ProcessingTime ?? 0,
                        IsSuccess = item.IsSuccess ?? true,
                        ProcessType = item.ProcessType
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetRecentHistoryAsync: {ex}");
            }
            return historyList;
        }
    }

    // Model classes to match Supabase schema
    public class OutputFile : BaseModel
    {
        public string Id { get; set; }
        public string HistoryId { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public long Size { get; set; }
        public DateTime CreatedAt { get; set; }
        public string FolderId { get; set; }
    }

    public class History : BaseModel
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public int InputFileTypeId { get; set; }
        public int OutputFormatId { get; set; }
        public DateTime ProcessDate { get; set; }
        public int? ProcessingTime { get; set; }
        public string PromptText { get; set; }
        public string ProcessType { get; set; }
        public bool? IsSuccess { get; set; }
        public FileType FileType { get; set; }
        public OutputFormat OutputFormat { get; set; }
    }

    public class FileType : BaseModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class OutputFormat : BaseModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class Folder : BaseModel
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class OutputFormatPreference : BaseModel
    {
        public string UserId { get; set; }
        public string Format { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class InputFileTypeStats : BaseModel
    {
        public string UserId { get; set; }
        public string FileType { get; set; }
        public int UsageCount { get; set; }
    }

    public class ChartData
    {
        public string Label { get; set; }
        public int Value { get; set; }
    }

    public class HistoryItem
    {
        public string HistoryId { get; set; }
        public string InputType { get; set; }
        public string OutputFormat { get; set; }
        public DateTime ProcessDate { get; set; }
        public int ProcessingTime { get; set; }
        public bool IsSuccess { get; set; }
        public string ProcessType { get; set; }
    }
}