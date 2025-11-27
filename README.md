# dlls
📌 ClipboardManager.dll

A lightweight and powerful DLL that exposes advanced Windows Clipboard capabilities — including full support for file operations — whether your application runs locally or through Remote Desktop (RDP / RemoteApp).

ClipboardManager.dll provides a clean, high-level API so you never have to deal directly with low-level Win32 clipboard handling or CF_HDROP structures.

🚀 Getting Started
1. Copy the DLL

Place ClipboardManager.dll in your application's execution directory.

2. Add a Reference

Add the DLL to your project as a .NET assembly.

3. Start Using It

Example:

if (ClipboardManager.HasClipboardFiles())
{
    var files = ClipboardManager.GetClipboardFiles();
    // process files...
}

📂 Key Features

✔ Access to all major Windows Clipboard file formats
✔ Supports local clipboard and RDP redirected clipboard
✔ Simple, high-level .NET API
✔ Retrieve file paths, metadata, and size
✔ Clipboard cleaning and file-count utilities
✔ Can be used from any .NET-capable environment
(C#, WPF, WinForms, MAUI, .NET Core, .NET 6+, server-side, etc.)

🔧 Available Functions
Function	Returns	Description
GetClipboardFileCount()	int	Number of files currently stored in the clipboard
GetClipboardTotalSize()	long	Total file size (in bytes) in the clipboard
HasClipboardFiles()	bool	Quick check to detect presence of files
GetClipboardFileInfo()	ClipboardFileInfo[]	Array of objects with detailed information
GetClipboardFiles()	string[]	List of full file paths
ClearClipboard()	void	Safely clears clipboard contents
🧱 Data Structure: ClipboardFileInfo
public class ClipboardFileInfo
{
    public string FilePath { get; set; }
    public string FileName { get; set; }
    public long FileSize { get; set; }
}

🖥 Compatibility

Windows 10 / 11

Windows Server / Remote Desktop Services

.NET Framework 4.x

.NET Core / .NET 5+ / .NET 6+

Any application that can reference a .NET assembly

🙌 Contributions

Issues, suggestions, and pull requests are welcome.
Feel free to help improve support, add new clipboard features, or enhance cross-environment behavior.
