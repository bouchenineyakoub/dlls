#pragma once

#include <windows.h>
#include <string>
#include <vector>

#ifdef CLIPBOARDMANAGER_EXPORTS
#define CLIPBOARDMANAGER_API __declspec(dllexport)
#else
#define CLIPBOARDMANAGER_API __declspec(dllimport)
#endif

// Structure to hold file information
struct FileInfo
{
    wchar_t path[MAX_PATH];
    wchar_t name[MAX_PATH];
    BOOL isDirectory;
    __int64 size;
};

// Export functions
extern "C"
{
    // Get count of files in clipboard
    CLIPBOARDMANAGER_API int GetClipboardFileCount();

    // Get file paths from clipboard
    CLIPBOARDMANAGER_API BOOL GetClipboardFiles(wchar_t* buffer, int bufferSize, int* fileCount);

    // Get detailed file information
    CLIPBOARDMANAGER_API BOOL GetClipboardFileInfo(FileInfo* fileInfoArray, int maxFiles, int* actualCount);

    // Get total size of all files
    CLIPBOARDMANAGER_API __int64 GetClipboardTotalSize();

    // Check if clipboard has files
    CLIPBOARDMANAGER_API BOOL HasClipboardFiles();

    // Clear clipboard
    CLIPBOARDMANAGER_API void ClearClipboard();

    // Free memory allocated by DLL
    CLIPBOARDMANAGER_API void FreeMemory(void* ptr);
}
