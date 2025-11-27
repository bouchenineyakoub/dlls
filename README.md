# dlls
Usefull dlls
ClipboardManager.dll
Tips : 
1. Copy `ClipboardManager.dll` to your project folder
2. Add reference to the DLL in your project as a .NET assembly, not a native DLL
3. Use directly:

## Key Functions

| Function | Returns | Purpose |
|----------|---------|---------|
| `GetClipboardFileCount()` | integer | Number of files in clipboard |
| `GetClipboardTotalSize()` | integer | Total size in bytes |
| `HasClipboardFiles()` | bool | Check if clipboard has files |
| `GetClipboardFileInfo()` | ClipboardFileInfo[] | Array with file details |
| `GetClipboardFiles()` | string[] | Array of file paths |
| `ClearClipboard()` | void | Clear the clipboard |
