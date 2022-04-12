using System;
using System.Collections.Generic;
using System.IO;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Microsoft.Band.Admin.Windows;

internal sealed class StorageProvider : IStorageProvider
{
    private StorageFolder appRootFolder;

    private StorageFolder deviceRootFolder;

    private StorageProvider(StorageFolder deviceRootFolder)
    {
        appRootFolder = ApplicationData.Current.LocalFolder;
        this.deviceRootFolder = deviceRootFolder;
    }

    internal static void CleanDeprecated()
    {
        string text = "userId";
        StorageFolder localFolder = ApplicationData.Current.LocalFolder;
        IReadOnlyList<StorageFolder> result = WindowsRuntimeSystemExtensions.AsTask<IReadOnlyList<StorageFolder>>(localFolder.GetFoldersAsync()).Result;
        for (int i = 0; i < result.Count; i++)
        {
            if (result[i].Name.Equals(text, StringComparison.OrdinalIgnoreCase))
            {
                WindowsRuntimeSystemExtensions.AsTask(WindowsRuntimeSystemExtensions.AsTask<StorageFolder>(localFolder.CreateFolderAsync(text, (CreationCollisionOption)3)).Result.DeleteAsync((StorageDeleteOption)1)).Wait();
                break;
            }
        }
    }

    internal static StorageProvider Create()
    {
        return new StorageProvider(null);
    }

    internal static StorageProvider Create(string userId, string deviceId)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(deviceId))
        {
            ArgumentException ex = new ArgumentException();
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        string text = string.Format("{0}{1}", new object[2] { "u_", userId });
        string text2 = string.Format("{0}{1}", new object[2] { "d_", deviceId });
        char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
        if (text.IndexOfAny(invalidFileNameChars) != -1)
        {
            ArgumentException ex2 = new ArgumentException(string.Format(CommonSR.InvalidCharactersInFolderName, new object[1] { text }));
            Logger.LogException(LogLevel.Error, ex2);
            throw ex2;
        }
        if (text2.IndexOfAny(invalidFileNameChars) != -1)
        {
            ArgumentException ex3 = new ArgumentException(string.Format(CommonSR.InvalidCharactersInFolderName, new object[1] { text2 }));
            Logger.LogException(LogLevel.Error, ex3);
            throw ex3;
        }
        StorageFolder result = WindowsRuntimeSystemExtensions.AsTask<StorageFolder>(ApplicationData.Current.LocalFolder.CreateFolderAsync(text, (CreationCollisionOption)3)).Result;
        _ = WindowsRuntimeSystemExtensions.AsTask<StorageFolder>(result.CreateFolderAsync(text2, (CreationCollisionOption)3)).Result;
        Logger.Log(LogLevel.Info, "Created phone storageProvider for userId = {0}, deviceId = {1}", userId, deviceId);
        return new StorageProvider(WindowsRuntimeSystemExtensions.AsTask<StorageFolder>(result.CreateFolderAsync(text2, (CreationCollisionOption)3)).Result);
    }

    private StorageFolder GetRootFolder(StorageProviderRoot root)
    {
        switch (root)
        {
        case StorageProviderRoot.App:
            return appRootFolder;
        case StorageProviderRoot.Device:
            if (deviceRootFolder == null)
            {
                throw new ArgumentException("StorageProviderRoot.Device not supported", "root");
            }
            return deviceRootFolder;
        default:
            throw new ArgumentException("StorageProviderRoot value unknown", "root");
        }
    }

    public Stream OpenFileForWrite(string relativePath, bool append, int bufferSize = 0)
    {
        return OpenFileForWrite(StorageProviderRoot.Device, relativePath, append, bufferSize);
    }

    public Stream OpenFileForWrite(StorageProviderRoot root, string relativePath, bool append, int bufferSize = 0)
    {
        //IL_0061: Unknown result type (might be due to invalid IL or missing references)
        //IL_006d: Expected O, but got Unknown
        Logger.Log(LogLevel.Verbose, "Opening phone file for write: {0}", relativePath);
        IAsyncOperation<StorageFile> val = null;
        val = ((!append) ? GetRootFolder(root).CreateFileAsync(relativePath, (CreationCollisionOption)1) : GetRootFolder(root).CreateFileAsync(relativePath, (CreationCollisionOption)3));
        WindowsRuntimeSystemExtensions.AsTask<StorageFile>(val).Wait();
        IAsyncOperation<IRandomAccessStream> obj = val.GetResults().OpenAsync((FileAccessMode)1);
        WindowsRuntimeSystemExtensions.AsTask<IRandomAccessStream>(obj).Wait();
        return WindowsRuntimeStreamExtensions.AsStream((IRandomAccessStream)(FileRandomAccessStream)obj.GetResults(), bufferSize);
    }

    public IRandomAccessStream OpenFileRandomAccessStreamForRead(StorageProviderRoot root, string relativePath)
    {
        //IL_0032: Unknown result type (might be due to invalid IL or missing references)
        //IL_004d: Unknown result type (might be due to invalid IL or missing references)
        //IL_0053: Expected O, but got Unknown
        Logger.Log(LogLevel.Verbose, "Opening phone file for read: {0}", relativePath);
        IAsyncOperation<IStorageItem> itemAsync = GetRootFolder(root).GetItemAsync(relativePath);
        WindowsRuntimeSystemExtensions.AsTask<IStorageItem>(itemAsync).Wait();
        IAsyncOperation<IRandomAccessStream> obj = ((StorageFile)itemAsync.GetResults()).OpenAsync((FileAccessMode)0);
        WindowsRuntimeSystemExtensions.AsTask<IRandomAccessStream>(obj).Wait();
        return (IRandomAccessStream)(FileRandomAccessStream)obj.GetResults();
    }

    public Stream OpenFileForRead(string relativePath, int bufferSize = 0)
    {
        return OpenFileForRead(StorageProviderRoot.Device, relativePath, bufferSize);
    }

    public Stream OpenFileForRead(StorageProviderRoot root, string relativePath, int bufferSize = 0)
    {
        //IL_0008: Unknown result type (might be due to invalid IL or missing references)
        //IL_000e: Expected O, but got Unknown
        FileRandomAccessStream val = (FileRandomAccessStream)OpenFileRandomAccessStreamForRead(root, relativePath);
        if (bufferSize == -1)
        {
            bufferSize = (int)Math.Min(val.Size, 8192uL);
        }
        return WindowsRuntimeStreamExtensions.AsStream((IRandomAccessStream)(object)val, bufferSize);
    }

    public void DeleteFile(string relativePath)
    {
        DeleteFile(StorageProviderRoot.Device, relativePath);
    }

    public void DeleteFile(StorageProviderRoot root, string relativePath)
    {
        Logger.Log(LogLevel.Verbose, "Deleting phone file: {0}", relativePath);
        string text = Path.Combine(new string[2]
        {
            GetRootFolder(root).Path,
            relativePath
        });
        try
        {
            StorageFile result = WindowsRuntimeSystemExtensions.AsTask<StorageFile>(StorageFile.GetFileFromPathAsync(text)).Result;
            if (result != null)
            {
                try
                {
                    WindowsRuntimeSystemExtensions.AsTask(result.DeleteAsync()).Wait();
                    return;
                }
                catch (Exception e)
                {
                    Logger.LogException(LogLevel.Error, e, " Unable to delete file: " + text);
                    return;
                }
            }
        }
        catch (FileNotFoundException)
        {
        }
        catch (AggregateException ex2)
        {
            if (ex2.InnerExceptions.Count != 1 || !(ex2.InnerException is FileNotFoundException))
            {
                throw;
            }
        }
    }

    public void RenameFile(string relativeSourcePath, string relativeDestFolder, string destFileName)
    {
        RenameFile(StorageProviderRoot.Device, relativeSourcePath, StorageProviderRoot.Device, relativeDestFolder, destFileName);
    }

    public void RenameFile(StorageProviderRoot sourceRoot, string relativeSourcePath, StorageProviderRoot destRoot, string relativeDestFolder, string destFileName)
    {
        Logger.Log(LogLevel.Verbose, "Renaming phone file:{0} to destinationFolder:{1}, destinationFileName:{2}", relativeSourcePath, relativeDestFolder, destFileName);
        StorageFile result = WindowsRuntimeSystemExtensions.AsTask<StorageFile>(GetRootFolder(sourceRoot).GetFileAsync(relativeSourcePath)).Result;
        StorageFolder val = ResolveFolder(destRoot, relativeDestFolder);
        WindowsRuntimeSystemExtensions.AsTask<StorageFile>(result.CopyAsync((IStorageFolder)(object)val, destFileName, (NameCollisionOption)1)).Wait();
        WindowsRuntimeSystemExtensions.AsTask(result.DeleteAsync()).Wait();
    }

    public long GetFileSize(string relativePath)
    {
        return GetFileSize(StorageProviderRoot.Device, relativePath);
    }

    public long GetFileSize(StorageProviderRoot root, string relativePath)
    {
        IRandomAccessStreamWithContentType result = WindowsRuntimeSystemExtensions.AsTask<IRandomAccessStreamWithContentType>(WindowsRuntimeSystemExtensions.AsTask<StorageFile>(GetRootFolder(root).GetFileAsync(relativePath)).Result.OpenReadAsync()).Result;
        try
        {
            Logger.Log(LogLevel.Verbose, "Retrieved file size for phone file:{0} is {1}", relativePath, (long)((IRandomAccessStream)result).Size);
            return (long)((IRandomAccessStream)result).Size;
        }
        finally
        {
            ((IDisposable)result)?.Dispose();
        }
    }

    public DateTime GetFileCreationTimeUtc(string relativePath)
    {
        return GetFileCreationTimeUtc(StorageProviderRoot.Device, relativePath);
    }

    public DateTime GetFileCreationTimeUtc(StorageProviderRoot root, string relativePath)
    {
        StorageFile result = WindowsRuntimeSystemExtensions.AsTask<StorageFile>(GetRootFolder(root).GetFileAsync(relativePath)).Result;
        Logger.Log(LogLevel.Verbose, "Retrieved file creation UTC time for phone file:{0} is {1}", relativePath, result.DateCreated.UtcDateTime);
        return result.DateCreated.UtcDateTime;
    }

    public string[] GetFiles(string folderRelativePath)
    {
        return GetFiles(StorageProviderRoot.Device, folderRelativePath);
    }

    public string[] GetFiles(StorageProviderRoot root, string folderRelativePath)
    {
        Logger.Log(LogLevel.Verbose, "Getting files from phone folder:{0}", folderRelativePath);
        IReadOnlyList<StorageFile> result = WindowsRuntimeSystemExtensions.AsTask<IReadOnlyList<StorageFile>>(ResolveFolder(root, folderRelativePath).GetFilesAsync()).Result;
        string[] array = new string[result.Count];
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = result[i].Name;
        }
        return array;
    }

    public string[] GetFolders(string folderRelativePath)
    {
        return GetFolders(StorageProviderRoot.Device, folderRelativePath);
    }

    public string[] GetFolders(StorageProviderRoot root, string folderRelativePath)
    {
        Logger.Log(LogLevel.Verbose, "Getting folders from phone folder:{0}", folderRelativePath);
        IReadOnlyList<StorageFolder> result = WindowsRuntimeSystemExtensions.AsTask<IReadOnlyList<StorageFolder>>(ResolveFolder(root, folderRelativePath).GetFoldersAsync()).Result;
        string[] array = new string[result.Count];
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = result[i].Name;
        }
        return array;
    }

    public void CreateFolder(string folderRelativePath)
    {
        CreateFolder(StorageProviderRoot.Device, folderRelativePath);
    }

    public void CreateFolder(StorageProviderRoot root, string folderRelativePath)
    {
        Logger.Log(LogLevel.Verbose, "Creating phone folder with relative path: {0}", folderRelativePath);
        WindowsRuntimeSystemExtensions.AsTask<StorageFolder>(GetRootFolder(root).CreateFolderAsync(folderRelativePath, (CreationCollisionOption)3)).Wait();
    }

    public void DeleteFolder(string folderRelativePath)
    {
        DeleteFolder(StorageProviderRoot.Device, folderRelativePath);
    }

    public void DeleteFolder(StorageProviderRoot root, string folderRelativePath)
    {
        Logger.Log(LogLevel.Verbose, "Deleting phone folder with relative path:{0}", folderRelativePath);
        string text = Path.Combine(new string[2]
        {
            GetRootFolder(root).Path,
            folderRelativePath
        });
        try
        {
            StorageFolder result = WindowsRuntimeSystemExtensions.AsTask<StorageFolder>(StorageFolder.GetFolderFromPathAsync(text)).Result;
            if (result != null)
            {
                try
                {
                    WindowsRuntimeSystemExtensions.AsTask(result.DeleteAsync()).Wait();
                    return;
                }
                catch (Exception e)
                {
                    Logger.LogException(LogLevel.Error, e, " Unable to delete folder: " + text);
                    return;
                }
            }
        }
        catch (FileNotFoundException)
        {
        }
        catch (AggregateException ex2)
        {
            if (ex2.InnerExceptions.Count != 1 || !(ex2.InnerException is FileNotFoundException))
            {
                throw;
            }
        }
    }

    public bool FileExists(string relativePath)
    {
        return FileExists(StorageProviderRoot.Device, relativePath);
    }

    public bool FileExists(StorageProviderRoot root, string relativePath)
    {
        Logger.Log(LogLevel.Verbose, "Checking if phone file exists with relative path: {0}", relativePath);
        string text = Path.Combine(new string[2]
        {
            GetRootFolder(root).Path,
            relativePath
        });
        try
        {
            return WindowsRuntimeSystemExtensions.AsTask<StorageFile>(StorageFile.GetFileFromPathAsync(text)).Result != null;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
        catch (AggregateException ex2)
        {
            if (ex2.InnerExceptions.Count == 1 && ex2.InnerException is FileNotFoundException)
            {
                return false;
            }
            throw;
        }
    }

    public bool DirectoryExists(string relativePath)
    {
        return DirectoryExists(StorageProviderRoot.Device, relativePath);
    }

    public bool DirectoryExists(StorageProviderRoot root, string relativePath)
    {
        Logger.Log(LogLevel.Verbose, "Checking if phone directory exists with relative path: {0}", relativePath);
        string text = Path.Combine(new string[2]
        {
            GetRootFolder(root).Path,
            relativePath
        });
        try
        {
            return WindowsRuntimeSystemExtensions.AsTask<StorageFolder>(StorageFolder.GetFolderFromPathAsync(text)).Result != null;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
        catch (AggregateException ex2)
        {
            if (ex2.InnerExceptions.Count == 1 && ex2.InnerException is FileNotFoundException)
            {
                return false;
            }
            throw;
        }
    }

    private StorageFolder ResolveFolder(StorageProviderRoot root, string folderRelativePath)
    {
        Logger.Log(LogLevel.Verbose, "Resolving phone folder with relative path: {0}", folderRelativePath);
        if (string.IsNullOrWhiteSpace(folderRelativePath))
        {
            ArgumentException ex = new ArgumentException();
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        StorageFolder val = GetRootFolder(root);
        string[] array = folderRelativePath.Split('\\');
        foreach (string text in array)
        {
            if (!string.IsNullOrWhiteSpace(text) && !(text == "."))
            {
                val = WindowsRuntimeSystemExtensions.AsTask<StorageFolder>(val.GetFolderAsync(text)).Result;
            }
        }
        return val;
    }

    public void MoveFolder(StorageProviderRoot sourceRoot, string relativeSourceFolder, StorageProviderRoot destRoot, string relativeDestFolder, bool overwrite = false)
    {
        Logger.Log(LogLevel.Verbose, "Move folder:{0} to destinationFolder:{1}", relativeSourceFolder, relativeDestFolder);
        StorageFolder sourceFolder = ResolveFolder(sourceRoot, relativeSourceFolder);
        if (overwrite)
        {
            DeleteFolder(destRoot, relativeDestFolder);
        }
        StorageFolder result = WindowsRuntimeSystemExtensions.AsTask<StorageFolder>(GetRootFolder(destRoot).CreateFolderAsync(relativeDestFolder, (CreationCollisionOption)2)).Result;
        CopyFolder(sourceFolder, result);
        DeleteFolder(sourceRoot, relativeSourceFolder);
    }

    private void CopyFolder(StorageFolder sourceFolder, StorageFolder destFolder)
    {
        foreach (StorageFile item in WindowsRuntimeSystemExtensions.AsTask<IReadOnlyList<StorageFile>>(sourceFolder.GetFilesAsync()).Result)
        {
            WindowsRuntimeSystemExtensions.AsTask<StorageFile>(item.CopyAsync((IStorageFolder)(object)destFolder)).Wait();
        }
        foreach (StorageFolder item2 in WindowsRuntimeSystemExtensions.AsTask<IReadOnlyList<StorageFolder>>(sourceFolder.GetFoldersAsync()).Result)
        {
            StorageFolder result = WindowsRuntimeSystemExtensions.AsTask<StorageFolder>(destFolder.CreateFolderAsync(item2.Name, (CreationCollisionOption)3)).Result;
            CopyFolder(item2, result);
        }
    }
}
