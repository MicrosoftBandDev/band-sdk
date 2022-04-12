// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.Phone.StorageProvider
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using System;
using System.Collections.Generic;
using System.IO;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Microsoft.Band.Admin.Phone
{
  internal sealed class StorageProvider : IStorageProvider
  {
    private StorageFolder appRootFolder;
    private StorageFolder deviceRootFolder;

    private StorageProvider(StorageFolder deviceRootFolder)
    {
      this.appRootFolder = ApplicationData.Current.LocalFolder;
      this.deviceRootFolder = deviceRootFolder;
    }

    internal static void CleanDeprecated()
    {
      string str = "userId";
      StorageFolder localFolder = ApplicationData.Current.LocalFolder;
      IReadOnlyList<StorageFolder> result = localFolder.GetFoldersAsync().AsTask<IReadOnlyList<StorageFolder>>().Result;
      for (int index = 0; index < ((IReadOnlyCollection<StorageFolder>) result).Count; ++index)
      {
        if (result[index].Name.Equals(str, StringComparison.OrdinalIgnoreCase))
        {
          localFolder.CreateFolderAsync(str, (CreationCollisionOption) 3).AsTask<StorageFolder>().Result.DeleteAsync((StorageDeleteOption) 1).AsTask().Wait();
          break;
        }
      }
    }

    internal static StorageProvider Create() => new StorageProvider((StorageFolder) null);

    internal static StorageProvider Create(string userId, string deviceId)
    {
      if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(deviceId))
      {
        ArgumentException e = new ArgumentException();
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      string str1 = string.Format("{0}{1}", new object[2]
      {
        (object) "u_",
        (object) userId
      });
      string str2 = string.Format("{0}{1}", new object[2]
      {
        (object) "d_",
        (object) deviceId
      });
      char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
      if (str1.IndexOfAny(invalidFileNameChars) != -1)
      {
        ArgumentException e = new ArgumentException(string.Format(CommonSR.InvalidCharactersInFolderName, new object[1]
        {
          (object) str1
        }));
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      if (str2.IndexOfAny(invalidFileNameChars) != -1)
      {
        ArgumentException e = new ArgumentException(string.Format(CommonSR.InvalidCharactersInFolderName, new object[1]
        {
          (object) str2
        }));
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      StorageFolder result1 = ApplicationData.Current.LocalFolder.CreateFolderAsync(str1, (CreationCollisionOption) 3).AsTask<StorageFolder>().Result;
      StorageFolder result2 = result1.CreateFolderAsync(str2, (CreationCollisionOption) 3).AsTask<StorageFolder>().Result;
      Logger.Log(LogLevel.Info, "Created phone storageProvider for userId = {0}, deviceId = {1}", (object) userId, (object) deviceId);
      return new StorageProvider(result1.CreateFolderAsync(str2, (CreationCollisionOption) 3).AsTask<StorageFolder>().Result);
    }

    private StorageFolder GetRootFolder(StorageProviderRoot root)
    {
      if (root == StorageProviderRoot.App)
        return this.appRootFolder;
      if (root != StorageProviderRoot.Device)
        throw new ArgumentException("StorageProviderRoot value unknown", nameof (root));
      return this.deviceRootFolder != null ? this.deviceRootFolder : throw new ArgumentException("StorageProviderRoot.Device not supported", nameof (root));
    }

    public Stream OpenFileForWrite(string relativePath, bool append, int bufferSize = 0) => this.OpenFileForWrite(StorageProviderRoot.Device, relativePath, append, bufferSize);

    public Stream OpenFileForWrite(
      StorageProviderRoot root,
      string relativePath,
      bool append,
      int bufferSize = 0)
    {
      Logger.Log(LogLevel.Verbose, "Opening phone file for write: {0}", (object) relativePath);
      IAsyncOperation<StorageFile> source1 = !append ? this.GetRootFolder(root).CreateFileAsync(relativePath, (CreationCollisionOption) 1) : this.GetRootFolder(root).CreateFileAsync(relativePath, (CreationCollisionOption) 3);
      source1.AsTask<StorageFile>().Wait();
      IAsyncOperation<IRandomAccessStream> source2 = source1.GetResults().OpenAsync((FileAccessMode) 1);
      source2.AsTask<IRandomAccessStream>().Wait();
      return source2.GetResults().AsStream(bufferSize);
    }

    public IRandomAccessStream OpenFileRandomAccessStreamForRead(
      StorageProviderRoot root,
      string relativePath)
    {
      Logger.Log(LogLevel.Verbose, "Opening phone file for read: {0}", (object) relativePath);
      IAsyncOperation<IStorageItem> itemAsync = this.GetRootFolder(root).GetItemAsync(relativePath);
      itemAsync.AsTask<IStorageItem>().Wait();
      IAsyncOperation<IRandomAccessStream> source = ((StorageFile) itemAsync.GetResults()).OpenAsync((FileAccessMode) 0);
      source.AsTask<IRandomAccessStream>().Wait();
      return source.GetResults();
    }

    public Stream OpenFileForRead(string relativePath, int bufferSize = 0) => this.OpenFileForRead(StorageProviderRoot.Device, relativePath, bufferSize);

    public Stream OpenFileForRead(
      StorageProviderRoot root,
      string relativePath,
      int bufferSize = 0)
    {
      FileRandomAccessStream windowsRuntimeStream = (FileRandomAccessStream) this.OpenFileRandomAccessStreamForRead(root, relativePath);
      if (bufferSize == -1)
        bufferSize = (int) Math.Min(windowsRuntimeStream.Size, 8192UL);
      return ((IRandomAccessStream) windowsRuntimeStream).AsStream(bufferSize);
    }

    public void DeleteFile(string relativePath) => this.DeleteFile(StorageProviderRoot.Device, relativePath);

    public void DeleteFile(StorageProviderRoot root, string relativePath)
    {
      Logger.Log(LogLevel.Verbose, "Deleting phone file: {0}", (object) relativePath);
      string str = Path.Combine(new string[2]
      {
        this.GetRootFolder(root).Path,
        relativePath
      });
      try
      {
        StorageFile result = StorageFile.GetFileFromPathAsync(str).AsTask<StorageFile>().Result;
        if (result == null)
          return;
        try
        {
          result.DeleteAsync().AsTask().Wait();
        }
        catch (Exception ex)
        {
          Logger.LogException(LogLevel.Error, ex, " Unable to delete file: " + str);
        }
      }
      catch (FileNotFoundException ex)
      {
      }
      catch (AggregateException ex)
      {
        if (ex.InnerExceptions.Count == 1 && ex.InnerException is FileNotFoundException)
          return;
        throw;
      }
    }

    public void RenameFile(
      string relativeSourcePath,
      string relativeDestFolder,
      string destFileName)
    {
      this.RenameFile(StorageProviderRoot.Device, relativeSourcePath, StorageProviderRoot.Device, relativeDestFolder, destFileName);
    }

    public void RenameFile(
      StorageProviderRoot sourceRoot,
      string relativeSourcePath,
      StorageProviderRoot destRoot,
      string relativeDestFolder,
      string destFileName)
    {
      Logger.Log(LogLevel.Verbose, "Renaming phone file:{0} to destinationFolder:{1}, destinationFileName:{2}", (object) relativeSourcePath, (object) relativeDestFolder, (object) destFileName);
      StorageFile result = this.GetRootFolder(sourceRoot).GetFileAsync(relativeSourcePath).AsTask<StorageFile>().Result;
      result.CopyAsync((IStorageFolder) this.ResolveFolder(destRoot, relativeDestFolder), destFileName, (NameCollisionOption) 1).AsTask<StorageFile>().Wait();
      result.DeleteAsync().AsTask().Wait();
    }

    public long GetFileSize(string relativePath) => this.GetFileSize(StorageProviderRoot.Device, relativePath);

    public long GetFileSize(StorageProviderRoot root, string relativePath)
    {
      using (IRandomAccessStreamWithContentType result = this.GetRootFolder(root).GetFileAsync(relativePath).AsTask<StorageFile>().Result.OpenReadAsync().AsTask<IRandomAccessStreamWithContentType>().Result)
      {
        Logger.Log(LogLevel.Verbose, "Retrieved file size for phone file:{0} is {1}", (object) relativePath, (object) (long) ((IRandomAccessStream) result).Size);
        return (long) ((IRandomAccessStream) result).Size;
      }
    }

    public DateTime GetFileCreationTimeUtc(string relativePath) => this.GetFileCreationTimeUtc(StorageProviderRoot.Device, relativePath);

    public DateTime GetFileCreationTimeUtc(StorageProviderRoot root, string relativePath)
    {
      StorageFile result = this.GetRootFolder(root).GetFileAsync(relativePath).AsTask<StorageFile>().Result;
      Logger.Log(LogLevel.Verbose, "Retrieved file creation UTC time for phone file:{0} is {1}", (object) relativePath, (object) result.DateCreated.UtcDateTime);
      return result.DateCreated.UtcDateTime;
    }

    public string[] GetFiles(string folderRelativePath) => this.GetFiles(StorageProviderRoot.Device, folderRelativePath);

    public string[] GetFiles(StorageProviderRoot root, string folderRelativePath)
    {
      Logger.Log(LogLevel.Verbose, "Getting files from phone folder:{0}", (object) folderRelativePath);
      IReadOnlyList<StorageFile> result = this.ResolveFolder(root, folderRelativePath).GetFilesAsync().AsTask<IReadOnlyList<StorageFile>>().Result;
      string[] files = new string[((IReadOnlyCollection<StorageFile>) result).Count];
      for (int index = 0; index < files.Length; ++index)
        files[index] = result[index].Name;
      return files;
    }

    public string[] GetFolders(string folderRelativePath) => this.GetFolders(StorageProviderRoot.Device, folderRelativePath);

    public string[] GetFolders(StorageProviderRoot root, string folderRelativePath)
    {
      Logger.Log(LogLevel.Verbose, "Getting folders from phone folder:{0}", (object) folderRelativePath);
      IReadOnlyList<StorageFolder> result = this.ResolveFolder(root, folderRelativePath).GetFoldersAsync().AsTask<IReadOnlyList<StorageFolder>>().Result;
      string[] folders = new string[((IReadOnlyCollection<StorageFolder>) result).Count];
      for (int index = 0; index < folders.Length; ++index)
        folders[index] = result[index].Name;
      return folders;
    }

    public void CreateFolder(string folderRelativePath) => this.CreateFolder(StorageProviderRoot.Device, folderRelativePath);

    public void CreateFolder(StorageProviderRoot root, string folderRelativePath)
    {
      Logger.Log(LogLevel.Verbose, "Creating phone folder with relative path: {0}", (object) folderRelativePath);
      this.GetRootFolder(root).CreateFolderAsync(folderRelativePath, (CreationCollisionOption) 3).AsTask<StorageFolder>().Wait();
    }

    public void DeleteFolder(string folderRelativePath) => this.DeleteFolder(StorageProviderRoot.Device, folderRelativePath);

    public void DeleteFolder(StorageProviderRoot root, string folderRelativePath)
    {
      Logger.Log(LogLevel.Verbose, "Deleting phone folder with relative path:{0}", (object) folderRelativePath);
      string str = Path.Combine(new string[2]
      {
        this.GetRootFolder(root).Path,
        folderRelativePath
      });
      try
      {
        StorageFolder result = StorageFolder.GetFolderFromPathAsync(str).AsTask<StorageFolder>().Result;
        if (result == null)
          return;
        try
        {
          result.DeleteAsync().AsTask().Wait();
        }
        catch (Exception ex)
        {
          Logger.LogException(LogLevel.Error, ex, " Unable to delete folder: " + str);
        }
      }
      catch (FileNotFoundException ex)
      {
      }
      catch (AggregateException ex)
      {
        if (ex.InnerExceptions.Count == 1 && ex.InnerException is FileNotFoundException)
          return;
        throw;
      }
    }

    public bool FileExists(string relativePath) => this.FileExists(StorageProviderRoot.Device, relativePath);

    public bool FileExists(StorageProviderRoot root, string relativePath)
    {
      Logger.Log(LogLevel.Verbose, "Checking if phone file exists with relative path: {0}", (object) relativePath);
      string str = Path.Combine(new string[2]
      {
        this.GetRootFolder(root).Path,
        relativePath
      });
      try
      {
        return StorageFile.GetFileFromPathAsync(str).AsTask<StorageFile>().Result != null;
      }
      catch (FileNotFoundException ex)
      {
        return false;
      }
      catch (AggregateException ex)
      {
        if (ex.InnerExceptions.Count == 1 && ex.InnerException is FileNotFoundException)
          return false;
        throw;
      }
    }

    public bool DirectoryExists(string relativePath) => this.DirectoryExists(StorageProviderRoot.Device, relativePath);

    public bool DirectoryExists(StorageProviderRoot root, string relativePath)
    {
      Logger.Log(LogLevel.Verbose, "Checking if phone directory exists with relative path: {0}", (object) relativePath);
      string str = Path.Combine(new string[2]
      {
        this.GetRootFolder(root).Path,
        relativePath
      });
      try
      {
        return StorageFolder.GetFolderFromPathAsync(str).AsTask<StorageFolder>().Result != null;
      }
      catch (FileNotFoundException ex)
      {
        return false;
      }
      catch (AggregateException ex)
      {
        if (ex.InnerExceptions.Count == 1 && ex.InnerException is FileNotFoundException)
          return false;
        throw;
      }
    }

    private StorageFolder ResolveFolder(
      StorageProviderRoot root,
      string folderRelativePath)
    {
      Logger.Log(LogLevel.Verbose, "Resolving phone folder with relative path: {0}", (object) folderRelativePath);
      if (string.IsNullOrWhiteSpace(folderRelativePath))
      {
        ArgumentException e = new ArgumentException();
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      StorageFolder storageFolder = this.GetRootFolder(root);
      string str1 = folderRelativePath;
      char[] chArray = new char[1]{ '\\' };
      foreach (string str2 in str1.Split(chArray))
      {
        if (!string.IsNullOrWhiteSpace(str2) && !(str2 == "."))
          storageFolder = storageFolder.GetFolderAsync(str2).AsTask<StorageFolder>().Result;
      }
      return storageFolder;
    }

    public void MoveFolder(
      StorageProviderRoot sourceRoot,
      string relativeSourceFolder,
      StorageProviderRoot destRoot,
      string relativeDestFolder,
      bool overwrite = false)
    {
      Logger.Log(LogLevel.Verbose, "Move folder:{0} to destinationFolder:{1}", (object) relativeSourceFolder, (object) relativeDestFolder);
      StorageFolder sourceFolder = this.ResolveFolder(sourceRoot, relativeSourceFolder);
      if (overwrite)
        this.DeleteFolder(destRoot, relativeDestFolder);
      StorageFolder result = this.GetRootFolder(destRoot).CreateFolderAsync(relativeDestFolder, (CreationCollisionOption) 2).AsTask<StorageFolder>().Result;
      this.CopyFolder(sourceFolder, result);
      this.DeleteFolder(sourceRoot, relativeSourceFolder);
    }

    private void CopyFolder(StorageFolder sourceFolder, StorageFolder destFolder)
    {
      foreach (StorageFile storageFile in (IEnumerable<StorageFile>) sourceFolder.GetFilesAsync().AsTask<IReadOnlyList<StorageFile>>().Result)
        storageFile.CopyAsync((IStorageFolder) destFolder).AsTask<StorageFile>().Wait();
      foreach (StorageFolder sourceFolder1 in (IEnumerable<StorageFolder>) sourceFolder.GetFoldersAsync().AsTask<IReadOnlyList<StorageFolder>>().Result)
      {
        StorageFolder result = destFolder.CreateFolderAsync(sourceFolder1.Name, (CreationCollisionOption) 3).AsTask<StorageFolder>().Result;
        this.CopyFolder(sourceFolder1, result);
      }
    }
  }
}
