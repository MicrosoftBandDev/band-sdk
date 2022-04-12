using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace Microsoft.Band.Admin;

internal static class ZipUtils
{
    internal static void Unzip(IStorageProvider storageProvider, StorageProviderRoot root, Stream zipStream, string extractFolder)
    {
        if (storageProvider == null)
        {
            throw new ArgumentNullException("storageProvider");
        }
        if (zipStream == null)
        {
            throw new ArgumentNullException("zipStream");
        }
        if (extractFolder == null)
        {
            throw new ArgumentNullException("extractFolder");
        }
        Logger.Log(LogLevel.Info, "Unzip file to folder: {0}", extractFolder);
        Stopwatch stopwatch = Stopwatch.StartNew();
        storageProvider.CreateFolder(root, extractFolder);
        using (ZipArchive zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true))
        {
            foreach (ZipArchiveEntry entry in zipArchive.Entries)
            {
                UnzipZipArchiveEntry(storageProvider, root, entry, entry.FullName, extractFolder);
            }
        }
        stopwatch.Stop();
        Logger.Log(LogLevel.Info, "Time to unzip: {0}", stopwatch.Elapsed);
    }

    private static void UnzipZipArchiveEntry(IStorageProvider storageProvider, StorageProviderRoot root, ZipArchiveEntry entry, string entryRelativePath, string extractFolder)
    {
        Logger.Log(LogLevel.Info, "Unzip file: {0}", entry.FullName);
        string directoryName = Path.GetDirectoryName(entryRelativePath);
        if (!string.IsNullOrEmpty(directoryName))
        {
            extractFolder = Path.Combine(new string[2] { extractFolder, directoryName });
            storageProvider.CreateFolder(root, extractFolder);
            entryRelativePath = Path.GetFileName(entryRelativePath);
        }
        if (string.IsNullOrEmpty(entryRelativePath))
        {
            return;
        }
        using Stream stream = entry.Open();
        string relativePath = Path.Combine(new string[2] { extractFolder, entryRelativePath });
        using Stream destination = storageProvider.OpenFileForWrite(root, relativePath, append: false, 8192);
        stream.CopyTo(destination);
    }
}
