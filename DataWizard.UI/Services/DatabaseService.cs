using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;

namespace DataWizard.UI.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService()
        {
            _connectionString = "Server=DESKTOP-01G7KT1\\SQLEXPRESS;Database=Quicklisticks;Trusted_Connection=True;";
        }

        public async Task<(bool success, string error)> ValidateUserCredentialsAsync(string username, string password)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand("sp_UserLogin", connection))
                    {
                        command.CommandType = System.Data.CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@Username", username);
                        command.Parameters.AddWithValue("@Password", password);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return (true, null);
                            }
                        }
                    }
                }
                return (false, "Invalid username or password");
            }
            catch (Exception ex)
            {
                return (false, $"Database error: {ex.Message}");
            }
        }

        public async Task<(bool success, string error)> CreateUserAsync(string username, string password, string email, string fullName)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var checkCommand = new SqlCommand(
                        "SELECT COUNT(*) FROM [User] WHERE Username = @Username OR Email = @Email",
                        connection))
                    {
                        checkCommand.Parameters.AddWithValue("@Username", username);
                        checkCommand.Parameters.AddWithValue("@Email", email);

                        int exists = (int)await checkCommand.ExecuteScalarAsync();
                        if (exists > 0)
                        {
                            return (false, "Username or email already exists");
                        }
                    }

                    using (var command = new SqlCommand(
                        "INSERT INTO [User] (Username, Password, Email, FullName, CreatedDate) " +
                        "VALUES (@Username, @Password, @Email, @FullName, GETDATE())",
                        connection))
                    {
                        command.Parameters.AddWithValue("@Username", username);
                        command.Parameters.AddWithValue("@Password", password);
                        command.Parameters.AddWithValue("@Email", email);
                        command.Parameters.AddWithValue("@FullName", fullName ?? string.Empty);

                        await command.ExecuteNonQueryAsync();
                        return (true, null);
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, $"Database error: {ex.Message}");
            }
        }

        public async Task<List<OutputFile>> GetRecentFilesAsync(int userId, int count = 4)
        {
            var files = new List<OutputFile>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(
                        @"SELECT TOP (@Count) FileID, FileName, FilePath, FileSize, CreatedDate 
                          FROM OutputFile of
                          INNER JOIN History h ON of.HistoryID = h.HistoryID
                          WHERE h.UserID = @UserID
                          ORDER BY of.CreatedDate DESC", connection))
                    {
                        command.Parameters.AddWithValue("@Count", count);
                        command.Parameters.AddWithValue("@UserID", userId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                files.Add(new OutputFile
                                {
                                    FileId = reader.GetInt32(0),
                                    FileName = reader.GetString(1),
                                    FilePath = reader.GetString(2),
                                    FileSize = reader.GetInt64(3),
                                    CreatedDate = reader.GetDateTime(4)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching recent files: {ex.Message}");
            }
            return files;
        }

        public async Task<List<Folder>> GetUserFoldersAsync(int userId, int count = 4)
        {
            var folders = new List<Folder>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(
                        @"SELECT TOP (@Count) FolderID, FolderName, CreatedDate 
                          FROM Folder 
                          WHERE UserID = @UserID
                          ORDER BY LastModifiedDate DESC", connection))
                    {
                        command.Parameters.AddWithValue("@Count", count);
                        command.Parameters.AddWithValue("@UserID", userId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                folders.Add(new Folder
                                {
                                    FolderId = reader.GetInt32(0),
                                    FolderName = reader.GetString(1),
                                    CreatedDate = reader.GetDateTime(2)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching folders: {ex.Message}");
            }
            return folders;
        }

        public async Task<List<ChartData>> GetFileTypeStatsAsync(int userId)
        {
            var stats = new List<ChartData>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("sp_GetInputFileTypeStats", connection))
                    {
                        command.CommandType = System.Data.CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@UserID", userId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                stats.Add(new ChartData
                                {
                                    Label = reader.GetString(0),
                                    Value = reader.GetInt32(1)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching file type stats: {ex.Message}");
            }
            return stats;
        }

        public async Task<string> GetUserPreferredFormatAsync(int userId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(
                        "SELECT Format FROM OutputFormatPreference WHERE UserID = @UserID",
                        connection))
                    {
                        command.Parameters.AddWithValue("@UserID", userId);
                        var result = await command.ExecuteScalarAsync();
                        return result?.ToString() ?? "Excel"; // Default to Excel if no preference set
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting user format preference: {ex.Message}");
                return "Excel"; // Default to Excel on error
            }
        }

        public async Task SaveUserPreferredFormatAsync(int userId, string format)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(
                        @"MERGE OutputFormatPreference WITH (HOLDLOCK) AS target
                          USING (SELECT @UserID AS UserID, @Format AS Format) AS source
                          ON target.UserID = source.UserID
                          WHEN MATCHED THEN
                              UPDATE SET Format = source.Format, UpdatedAt = GETDATE()
                          WHEN NOT MATCHED THEN
                              INSERT (UserID, Format)
                              VALUES (source.UserID, source.Format);",
                        connection))
                    {
                        command.Parameters.AddWithValue("@UserID", userId);
                        command.Parameters.AddWithValue("@Format", format);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving user format preference: {ex.Message}");
            }
        }

        public async Task LogFileUsageAsync(int userId, string fileName, string fileType, string processingMode)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(
                        @"INSERT INTO FileUsageHistory (UserID, FileName, FileType, ProcessingMode)
                          VALUES (@UserID, @FileName, @FileType, @ProcessingMode)",
                        connection))
                    {
                        command.Parameters.AddWithValue("@UserID", userId);
                        command.Parameters.AddWithValue("@FileName", fileName);
                        command.Parameters.AddWithValue("@FileType", fileType);
                        command.Parameters.AddWithValue("@ProcessingMode", processingMode);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging file usage: {ex.Message}");
            }
        }

        public async Task<bool> SaveFileToFolderAsync(int userId, int folderId, string fileName, string filePath)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(
                        @"INSERT INTO SavedFiles (UserID, FolderID, FileName, FilePath)
                          VALUES (@UserID, @FolderID, @FileName, @FilePath)",
                        connection))
                    {
                        command.Parameters.AddWithValue("@UserID", userId);
                        command.Parameters.AddWithValue("@FolderID", folderId);
                        command.Parameters.AddWithValue("@FileName", fileName);
                        command.Parameters.AddWithValue("@FilePath", filePath);
                        await command.ExecuteNonQueryAsync();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving file to folder: {ex.Message}");
                return false;
            }
        }

        public async Task<List<SavedFile>> GetSavedFilesInFolderAsync(int userId, int folderId)
        {
            var files = new List<SavedFile>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(
                        @"SELECT Id, FileName, FilePath, CreatedAt
                          FROM SavedFiles
                          WHERE UserID = @UserID AND FolderID = @FolderID
                          ORDER BY CreatedAt DESC",
                        connection))
                    {
                        command.Parameters.AddWithValue("@UserID", userId);
                        command.Parameters.AddWithValue("@FolderID", folderId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                files.Add(new SavedFile
                                {
                                    Id = reader.GetGuid(0),
                                    FileName = reader.GetString(1),
                                    FilePath = reader.GetString(2),
                                    CreatedDate = reader.GetDateTime(3)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting saved files: {ex.Message}");
            }
            return files;
        }
        public async Task<int> LogHistoryAsync(int userId, int inputFileTypeId, int outputFormatId, string prompt, string processType)
        {
            // Log method entry with parameters
            Debug.WriteLine($"[LogHistoryAsync] Starting history logging for UserID: {userId}, " +
                           $"InputFileTypeID: {inputFileTypeId}, OutputFormatID: {outputFormatId}, " +
                           $"ProcessType: {processType}");

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    // Log connection opening
                    Debug.WriteLine($"[LogHistoryAsync] Opening database connection...");
                    await connection.OpenAsync();
                    Debug.WriteLine($"[LogHistoryAsync] Database connection opened successfully");

                    using (var command = new SqlCommand(
                        @"INSERT INTO History (UserID, InputFileType, OutputFormatID, ProcessDate, ProcessingTime, PromptText, ProcessType)
                  VALUES (@UserID, @InputFileType, @OutputFormatID, GETDATE(), 0, @PromptText, @ProcessType);
                  SELECT CAST(SCOPE_IDENTITY() AS INT);",
                        connection))
                    {
                        // Log parameter values
                        Debug.WriteLine($"[LogHistoryAsync] Setting parameters: " +
                                       $"UserID={userId}, " +
                                       $"InputFileType={inputFileTypeId}, " +
                                       $"OutputFormatID={outputFormatId}, " +
                                       $"PromptText={(prompt != null ? "[REDACTED]" : "null")}, " +
                                       $"ProcessType={processType}");

                        command.Parameters.AddWithValue("@UserID", userId);
                        command.Parameters.AddWithValue("@InputFileType", inputFileTypeId);
                        command.Parameters.AddWithValue("@OutputFormatID", outputFormatId);
                        command.Parameters.AddWithValue("@PromptText", prompt ?? string.Empty);
                        command.Parameters.AddWithValue("@ProcessType", processType ?? string.Empty);

                        // Log before executing command
                        Debug.WriteLine($"[LogHistoryAsync] Executing SQL command...");

                        var result = await command.ExecuteScalarAsync();
                        int historyId = (result != null) ? (int)result : -1;

                        // Log successful insertion
                        Debug.WriteLine($"[LogHistoryAsync] Successfully logged history. HistoryID: {historyId}");

                        return historyId;
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                // Special handling for SQL-specific errors
                Debug.WriteLine($"[LogHistoryAsync] SQL Error {sqlEx.Number}: {sqlEx.Message}");
                Debug.WriteLine($"[LogHistoryAsync] SQL Server Errors:");
                foreach (SqlError err in sqlEx.Errors)
                {
                    Debug.WriteLine($"[LogHistoryAsync] - Error {err.Number}: {err.Message}");
                    Debug.WriteLine($"[LogHistoryAsync] - Procedure: {err.Procedure}, Line: {err.LineNumber}");
                }
                return -1;
            }
            catch (Exception ex)
            {
                // General error handling
                Debug.WriteLine($"[LogHistoryAsync] General Error: {ex.Message}");
                Debug.WriteLine($"[LogHistoryAsync] Stack Trace: {ex.StackTrace}");
                return -1;
            }
            finally
            {
                Debug.WriteLine($"[LogHistoryAsync] Logging operation completed");
            }
        }

        public async Task UpdateHistoryProcessingTimeAsync(int historyId, int processingTimeMs)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(
                        "UPDATE History SET ProcessingTime = @ProcessingTime WHERE HistoryID = @HistoryId",
                        connection))
                    {
                        command.Parameters.AddWithValue("@HistoryId", historyId);
                        command.Parameters.AddWithValue("@ProcessingTime", processingTimeMs);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating history processing time: {ex.Message}");
            }
        }

        public async Task UpdateHistoryStatusAsync(int historyId, bool isSuccess, int processingTimeMs)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(
                        @"UPDATE History 
                          SET ProcessingTime = @ProcessingTime, 
                              IsSuccess = @IsSuccess
                          WHERE HistoryID = @HistoryId",
                        connection))
                    {
                        command.Parameters.AddWithValue("@HistoryId", historyId);
                        command.Parameters.AddWithValue("@ProcessingTime", processingTimeMs);
                        command.Parameters.AddWithValue("@IsSuccess", isSuccess);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating history status: {ex.Message}");
            }
        }

        public async Task LogOutputFileAsync(int historyId, string fileName, string filePath, long fileSize)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(
                        @"INSERT INTO OutputFile (HistoryID, FileName, FilePath, FileSize, CreatedDate)
                          VALUES (@HistoryId, @FileName, @FilePath, @FileSize, GETDATE())",
                        connection))
                    {
                        command.Parameters.AddWithValue("@HistoryId", historyId);
                        command.Parameters.AddWithValue("@FileName", fileName);
                        command.Parameters.AddWithValue("@FilePath", filePath);
                        command.Parameters.AddWithValue("@FileSize", fileSize);
                        await command.ExecuteNonQueryAsync();
                    }
                }
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
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(
                        "SELECT FileTypeID FROM FileType WHERE TypeName = @TypeName",
                        connection))
                    {
                        command.Parameters.AddWithValue("@TypeName", typeName);
                        var result = await command.ExecuteScalarAsync();

                        if (result != null)
                        {
                            return (int)result;
                        }

                        // Default ke 'OTHER' jika tipe tidak ditemukan
                        return await GetFileTypeId("OTHER");
                    }
                }
            }
            catch
            {
                // Default ke 'OTHER' pada error
                return await GetFileTypeId("OTHER");
            }
        }


        public async Task<int> GetOutputFormatId(string formatName)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(
                        "SELECT OutputFormatID FROM OutputFormat WHERE FormatName = @FormatName",
                        connection))
                    {
                        command.Parameters.AddWithValue("@FormatName", formatName);
                        var result = await command.ExecuteScalarAsync();
                        return (result != null) ? (int)result : 1; // Default ke Excel (ID=1)
                    }
                }
            }
            catch
            {
                return 1; // Default ke Excel (ID=1) pada error
            }
        }
        public async Task<List<HistoryItem>> GetRecentHistoryAsync(int userId, int count)
        {
            var historyList = new List<HistoryItem>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"
                SELECT TOP (@Count) 
                    h.HistoryID,
                    ft.TypeName AS InputType,
                    opf.FormatName AS OutputFormat,  -- Mengganti alias 'of' menjadi 'opf'
                    h.ProcessDate,
                    h.ProcessingTime,
                    h.IsSuccess,
                    h.ProcessType
                FROM History h
                JOIN [User] u ON h.UserID = u.UserID
                JOIN FileType ft ON h.InputFileType = ft.FileTypeID
                JOIN OutputFormat opf ON h.OutputFormatID = opf.OutputFormatID  -- Diubah disini
                WHERE h.UserID = @UserID
                ORDER BY h.ProcessDate DESC";

                    Debug.WriteLine($"Executing query: {query}");  // Log query untuk debugging

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Count", count);
                        command.Parameters.AddWithValue("@UserID", userId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                historyList.Add(new HistoryItem
                                {
                                    HistoryId = reader.GetInt32(0),
                                    InputType = reader.GetString(1),
                                    OutputFormat = reader.GetString(2),
                                    ProcessDate = reader.GetDateTime(3),
                                    ProcessingTime = reader.GetInt32(4),
                                    IsSuccess = reader.GetBoolean(5),
                                    ProcessType = reader.IsDBNull(6) ? "" : reader.GetString(6)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetRecentHistoryAsync: {ex.ToString()}");
                throw;  // Re-throw exception untuk ditangani di layer atas
            }

            return historyList;
        }
    }

    public class OutputFile
    {
        public int FileId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class HistoryItem
    {
        public int HistoryId { get; set; }
        public string InputType { get; set; }
        public string OutputFormat { get; set; }
        public DateTime ProcessDate { get; set; }
        public int ProcessingTime { get; set; }
        public bool IsSuccess { get; set; }
        public string ProcessType { get; set; }  // Ditambahkan
    }
    public class Folder
    {
        public int FolderId { get; set; }
        public string FolderName { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class ChartData
    {
        public string Label { get; set; }
        public int Value { get; set; }
    }

    public class SavedFile
    {
        public Guid Id { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}