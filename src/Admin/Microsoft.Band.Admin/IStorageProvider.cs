// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.IStorageProvider
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System;
using System.IO;

namespace Microsoft.Band.Admin
{
  public interface IStorageProvider
  {
    Stream OpenFileForWrite(string relativePath, bool append, int bufferSize = 0);

    Stream OpenFileForWrite(
      StorageProviderRoot root,
      string relativePath,
      bool append,
      int bufferSize = 0);

    Stream OpenFileForRead(string relativePath, int bufferSize = 0);

    Stream OpenFileForRead(StorageProviderRoot root, string relativePath, int bufferSize = 0);

    void DeleteFile(string relativePath);

    void DeleteFile(StorageProviderRoot root, string relativePath);

    void RenameFile(string relativeSourcePath, string relativeDestFolder, string destFileName);

    void RenameFile(
      StorageProviderRoot sourceRoot,
      string relativeSourcePath,
      StorageProviderRoot destRoot,
      string relativeDestFolder,
      string destFileName);

    long GetFileSize(string relativePath);

    long GetFileSize(StorageProviderRoot root, string relativePath);

    string[] GetFiles(string folderRelativePath);

    string[] GetFiles(StorageProviderRoot root, string folderRelativePath);

    string[] GetFolders(string folderRelativePath);

    string[] GetFolders(StorageProviderRoot root, string folderRelativePath);

    void CreateFolder(string folderRelativePath);

    void CreateFolder(StorageProviderRoot root, string folderRelativePath);

    void DeleteFolder(string folderRelativePath);

    void DeleteFolder(StorageProviderRoot root, string folderRelativePath);

    DateTime GetFileCreationTimeUtc(string relativePath);

    DateTime GetFileCreationTimeUtc(StorageProviderRoot root, string relativePath);

    bool FileExists(string relativePath);

    bool FileExists(StorageProviderRoot root, string relativePath);

    bool DirectoryExists(string relativePath);

    bool DirectoryExists(StorageProviderRoot root, string relativePath);

    void MoveFolder(
      StorageProviderRoot sourceRoot,
      string relativeSourceFolder,
      StorageProviderRoot destRoot,
      string relativeDestFolder,
      bool overwrite = false);
  }
}
