using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace ClipboardManager
{
    /// <summary>
    /// Provides functionality to retrieve clipboard content and file information
    /// Works with local clipboard and RDP redirected clipboard from client machines
    /// </summary>
    public class ClipboardFileManager
    {
        // P/Invoke declarations for clipboard and RDP support
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseClipboard();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        /// <summary>
        /// Gets the list of file paths from the clipboard
        /// Supports both local files and RDP redirected files from client machine
        /// </summary>
        /// <returns>Array of file paths currently in clipboard</returns>
        public static string[] GetClipboardFiles()
        {
            string[] result = new string[0];
            
            try
            {
                // Use a separate thread with STA to ensure clipboard works
                System.Threading.Thread thread = new System.Threading.Thread(
                    new System.Threading.ThreadStart(delegate()
                    {
                        try
                        {
                            IDataObject dataObject = Clipboard.GetDataObject();
                            
                            if (dataObject == null)
                                return;

                            // Check if clipboard contains file drop format
                            if (dataObject.GetDataPresent(DataFormats.FileDrop))
                            {
                                object data = dataObject.GetData(DataFormats.FileDrop);
                                if (data is string[])
                                {
                                    string[] files = (string[])data;
                                    result = FilterValidFiles(files);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine("Error in STA thread: " + ex.Message);
                        }
                    }));
                
                thread.SetApartmentState(System.Threading.ApartmentState.STA);
                thread.Start();
                thread.Join();
                
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error getting clipboard files: " + ex.Message);
                return new string[0];
            }
        }

        /// <summary>
        /// Filters valid files from clipboard entries
        /// Includes both local files and UNC paths (from RDP redirection)
        /// </summary>
        private static string[] FilterValidFiles(string[] files)
        {
            List<string> validFiles = new List<string>();

            foreach (string file in files)
            {
                if (string.IsNullOrEmpty(file))
                    continue;

                // Accept:
                // 1. Local absolute paths (C:\path\to\file)
                // 2. UNC paths (\\server\share\file) - from RDP redirection
                // 3. Paths that exist locally
                if ((file.Length > 2 && file[1] == ':') ||  // Local drive
                    (file.StartsWith(@"\\", StringComparison.OrdinalIgnoreCase)) ||  // UNC/network path
                    File.Exists(file) || Directory.Exists(file))  // Physically exists
                {
                    validFiles.Add(file);
                }
            }

            return validFiles.ToArray();
        }

        /// <summary>
        /// Gets the count of files in the clipboard
        /// </summary>
        /// <returns>Number of files in clipboard</returns>
        public static int GetClipboardFileCount()
        {
            return GetClipboardFiles().Length;
        }

        /// <summary>
        /// Gets file information for all files in the clipboard
        /// </summary>
        /// <returns>Array of ClipboardFileInfo objects</returns>
        public static ClipboardFileInfo[] GetClipboardFileInfo()
        {
            try
            {
                string[] files = GetClipboardFiles();
                List<ClipboardFileInfo> fileInfoList = new List<ClipboardFileInfo>();

                foreach (string filePath in files)
                {
                    if (File.Exists(filePath) || Directory.Exists(filePath))
                    {
                        var fileInfo = new ClipboardFileInfo();
                        fileInfo.Path = filePath;
                        fileInfo.Name = Path.GetFileName(filePath);
                        fileInfo.IsDirectory = Directory.Exists(filePath);
                        fileInfo.Size = GetItemSize(filePath);
                        fileInfoList.Add(fileInfo);
                    }
                }

                return fileInfoList.ToArray();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error getting clipboard file info: " + ex.Message);
                return new ClipboardFileInfo[0];
            }
        }

        /// <summary>
        /// Gets the total size of all files in the clipboard
        /// </summary>
        /// <returns>Total size in bytes</returns>
        public static long GetClipboardTotalSize()
        {
            try
            {
                string[] files = GetClipboardFiles();
                long totalSize = 0;

                foreach (string filePath in files)
                {
                    totalSize += GetItemSize(filePath);
                }

                return totalSize;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error calculating total size: " + ex.Message);
                return 0;
            }
        }

        /// <summary>
        /// Clears the clipboard content
        /// </summary>
        public static void ClearClipboard()
        {
            try
            {
                Clipboard.Clear();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error clearing clipboard: " + ex.Message);
            }
        }

        /// <summary>
        /// Checks if clipboard contains files
        /// </summary>
        /// <returns>True if clipboard contains files</returns>
        public static bool HasClipboardFiles()
        {
            return GetClipboardFileCount() > 0;
        }

        private static long GetItemSize(string path)
        {
            try
            {
                // Handle UNC paths (network/RDP redirected paths)
                if (path.StartsWith(@"\\", StringComparison.OrdinalIgnoreCase))
                {
                    // For UNC paths, try to access them directly
                    if (File.Exists(path))
                    {
                        return new FileInfo(path).Length;
                    }
                    else if (Directory.Exists(path))
                    {
                        return GetDirectorySizeUNC(path);
                    }
                    // If path doesn't exist, return 0 (might be temporarily unavailable in RDP)
                    return 0;
                }

                // Handle local paths
                if (File.Exists(path))
                {
                    return new FileInfo(path).Length;
                }
                else if (Directory.Exists(path))
                {
                    long size = 0;
                    var dirInfo = new DirectoryInfo(path);
                    try
                    {
                        foreach (var file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
                        {
                            size += file.Length;
                        }
                    }
                    catch
                    {
                        // If we can't enumerate, return what we have
                    }
                    return size;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error getting size for " + path + ": " + ex.Message);
            }

            return 0;
        }

        /// <summary>
        /// Gets directory size for UNC paths (with error handling for network delays)
        /// </summary>
        private static long GetDirectorySizeUNC(string path)
        {
            try
            {
                long size = 0;
                var dirInfo = new DirectoryInfo(path);
                foreach (var file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
                {
                    try
                    {
                        size += file.Length;
                    }
                    catch
                    {
                        // Skip files that can't be accessed
                    }
                }
                return size;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Checks whether a UNC path is accessible within a timeout (ms).
        /// Returns true if the file or directory becomes accessible within the timeout.
        /// </summary>
        public static bool IsUNCFileAccessibleWithTimeout(string path, int timeoutMs)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                    return false;

                // If not a UNC path, just check existence
                if (!path.StartsWith("\\\\", StringComparison.OrdinalIgnoreCase))
                {
                    return File.Exists(path) || Directory.Exists(path);
                }

                if (timeoutMs <= 0)
                    timeoutMs = 3000;

                var sw = System.Diagnostics.Stopwatch.StartNew();
                while (sw.ElapsedMilliseconds < timeoutMs)
                {
                    try
                    {
                        if (File.Exists(path) || Directory.Exists(path))
                        {
                            return true;
                        }
                    }
                    catch
                    {
                        // swallow transient network exceptions and retry
                    }

                    System.Threading.Thread.Sleep(150);
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Waits for a UNC file to become available using multiple retries.
        /// </summary>
        public static bool WaitForUNCFileAvailable(string path, int maxRetries, int delayMs)
        {
            if (maxRetries <= 0) maxRetries = 5;
            if (delayMs <= 0) delayMs = 500;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                if (IsUNCFileAccessibleWithTimeout(path, delayMs))
                    return true;

                try { System.Threading.Thread.Sleep(delayMs); } catch { }
            }

            return false;
        }

        /// <summary>
        /// Process a UNC file with retries. Returns true if accessible.
        /// </summary>
        public static bool ProcessUNCFileWithRetry(string path, int maxRetries)
        {
            if (string.IsNullOrEmpty(path)) return false;

            bool isUNC = path.StartsWith("\\\\", StringComparison.OrdinalIgnoreCase);
            if (!isUNC)
            {
                return File.Exists(path) || Directory.Exists(path);
            }

            if (maxRetries <= 0) maxRetries = 3;

            for (int r = 1; r <= maxRetries; r++)
            {
                if (IsUNCFileAccessibleWithTimeout(path, 1000))
                    return true;

                try { System.Threading.Thread.Sleep(1000); } catch { }
            }

            return false;
        }

        /// <summary>
        /// Returns diagnostic information about a given path (accessibility, size, type).
        /// </summary>
        public static string DiagnosticUNCAccess(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                    return "Path empty";

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("=== DIAGNOSTIC ACCES ===");
                sb.AppendLine("Path: " + path);

                bool isUNC = path.StartsWith("\\\\");
                sb.AppendLine(isUNC ? "Type: UNC" : "Type: Local");

                bool accessible = false;
                try { accessible = File.Exists(path) || Directory.Exists(path); } catch { accessible = false; }
                sb.AppendLine(accessible ? "Accessible: YES" : "Accessible: NO");

                if (accessible)
                {
                    if (File.Exists(path))
                    {
                        long size = new FileInfo(path).Length;
                        sb.AppendLine("Size bytes: " + size);
                    }
                    else if (Directory.Exists(path))
                    {
                        long size = GetDirectorySizeUNC(path);
                        sb.AppendLine("Directory size bytes (approx): " + size);
                    }
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Diagnostic error: " + ex.Message;
            }
        }

        /// <summary>
        /// Detects if the application is running in a RemoteApp/RDP session.
        /// </summary>
        public static bool IsRemoteAppSession()
        {
            try
            {
                string sessionName = Environment.GetEnvironmentVariable("SESSIONNAME");
                if (!string.IsNullOrEmpty(sessionName))
                {
                    return sessionName.Contains("RDP") || sessionName.Contains("RDP-Tcp");
                }
            }
            catch { }
            return false;
        }
    }

    /// <summary>
    /// Represents information about a file in the clipboard
    /// </summary>
    public class ClipboardFileInfo
    {
        /// <summary>
        /// Gets or sets the full path of the file
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the file name (without path)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets whether the item is a directory
        /// </summary>
        public bool IsDirectory { get; set; }

        /// <summary>
        /// Gets or sets the size in bytes
        /// </summary>
        public long Size { get; set; }
    }
}
