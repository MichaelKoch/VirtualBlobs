using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VirtualBlobs.Interfaces;


namespace VirtualBlobs.Providers.FileSystem
{
    public class FileSystemStorageProvider : IStorageProvider
    {
        private readonly string _storagePath;

        public FileSystemStorageProvider(string rootPath)
        {
            _storagePath = rootPath;
        }

        /// <summary>
        /// Maps a relative path into the storage path.
        /// </summary>
        /// <param name="path">The relative path to be mapped.</param>
        /// <returns>The relative path combined with the storage path.</returns>
        private string MapStorage(string path)
        {
            string mappedPath = string.IsNullOrEmpty(path) ? _storagePath : Path.Combine(_storagePath, path);
            return ValidatePath(_storagePath, mappedPath);
        }
        /// <summary>
        /// Determines if a path lies within the base path boundaries.
        /// If not, an exception is thrown.
        /// </summary>
        /// <param name="basePath">The base path which boundaries are not to be transposed.</param>
        /// <param name="mappedPath">The path to determine.</param>
        /// <rereturns>The mapped path if valid.</rereturns>
        /// <exception cref="ArgumentException">If the path is invalid.</exception>
        private static string ValidatePath(string basePath, string mappedPath)
        {
            bool valid = false;

            try
            {
                // Check that we are indeed within the storage directory boundaries
                valid = Path.GetFullPath(mappedPath).StartsWith(Path.GetFullPath(basePath), StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                // Make sure that if invalid for medium trust we give a proper exception
                valid = false;
            }

            if (!valid)
            {
                throw new ArgumentException("Invalid path");
            }

            return mappedPath;
        }
        private static string Fix(string path)
        {
            return string.IsNullOrEmpty(path)
                       ? ""
                       : Path.DirectorySeparatorChar != '/'
                             ? path.Replace('/', Path.DirectorySeparatorChar)
                             : path;
        }

        /// <summary>
        /// Retrieves a file within the storage provider.
        /// </summary>
        /// <param name="path">The relative path to the file within the storage provider.</param>
        /// <returns>The file.</returns>
        /// <exception cref="ArgumentException">If the file is not found.</exception>
        public async Task<IStorageFile> GetFileAsync(string path)
        {
            return await new Task<IStorageFile>(() =>
            {
                FileInfo fileInfo = new(MapStorage(path));
                if (!fileInfo.Exists)
                {
                    throw new ArgumentException(string.Format("File {0} does not exist", path));
                }
                return new FileSystemEntry(Fix(path), fileInfo);
            });

        }

        /// <summary>
        /// Lists the files within a storage provider's path.
        /// </summary>
        /// <param name="path">The relative path to the folder which files to list.</param>
        /// <returns>The list of files in the folder.</returns>
        public async Task<IEnumerable<IStorageFile>> ListFilesAsync(string path)
        {
            return await new Task<IEnumerable<IStorageFile>>(() =>
           {
               var directoryInfo = new DirectoryInfo(MapStorage(path));
               if (!directoryInfo.Exists)
               {
                   return Enumerable.Empty<IStorageFile>();
               }

               return directoryInfo
                   .GetFiles()
                   .Select<FileInfo, IStorageFile>(fi => new FileSystemEntry(Path.Combine(Fix(path), fi.Name), fi))
                   .ToList();
           });
        }

        /// <summary>
        /// Lists the folders within a storage provider's path.
        /// </summary>
        /// <param name="path">The relative path to the folder which folders to list.</param>
        /// <returns>The list of folders in the folder.</returns>
        public async Task<IEnumerable<IStorageFolder>> ListFoldersAsync(string path)
        {

            return await new Task<IEnumerable<IStorageFolder>>(() =>
            {
                var directoryInfo = new DirectoryInfo(MapStorage(path));
                if (!directoryInfo.Exists)
                {
                    try
                    {
                        directoryInfo.Create();
                    }
                    catch (Exception ex)
                    {
                        throw new ArgumentException(string.Format("The folder could not be created at path: {0}. {1}", path, ex));
                    }
                }
                return directoryInfo
                    .GetDirectories()
                    .Select<DirectoryInfo, IStorageFolder>(di => new FileSystemFolder(Path.Combine(Fix(path), di.Name), di))
                    .ToList();
            });
        }

        /// <summary>
        /// Tries to create a folder in the storage provider.
        /// </summary>
        /// <param name="path">The relative path to the folder to be created.</param>
        /// <returns>True if success; False otherwise.</returns>
        public async Task<bool> TryCreateFolderAsync(string path)
        {
            return await new Task<bool>(() =>
           {
               try
               {
                   // prevent unnecessary exception
                   var directoryInfo = new DirectoryInfo(MapStorage(path));
                   if (directoryInfo.Exists)
                   {
                       return false;
                   }
                   CreateFolderAsync(path).Wait();
               }
               catch
               {
                   return false;
               }
               return true;
           });
        }

        /// <summary>
        /// Creates a folder in the storage provider.
        /// </summary>
        /// <param name="path">The relative path to the folder to be created.</param>
        /// <exception cref="ArgumentException">If the folder already exists.</exception>
        public async Task CreateFolderAsync(string path)
        {
            await new Task(() =>
            {
                var directoryInfo = new DirectoryInfo(MapStorage(path));
                if (directoryInfo.Exists)
                {
                    throw new ArgumentException(string.Format("Directory {0} already exists", path));
                }
                Directory.CreateDirectory(directoryInfo.FullName);
            });
        }

        /// <summary>
        /// Deletes a folder in the storage provider.
        /// </summary>
        /// <param name="path">The relative path to the folder to be deleted.</param>
        /// <exception cref="ArgumentException">If the folder doesn't exist.</exception>
        public async Task DeleteFolderAsync(string path)
        {
            await new Task(() =>
            {
                var directoryInfo = new DirectoryInfo(MapStorage(path));
                if (!directoryInfo.Exists)
                {
                    throw new ArgumentException(string.Format("Directory {0} does not exist", path));
                }
                directoryInfo.Delete(true);
            });
        }

        /// <summary>
        /// Renames a folder in the storage provider.
        /// </summary>
        /// <param name="oldPath">The relative path to the folder to be renamed.</param>
        /// <param name="newPath">The relative path to the new folder.</param>
        public async Task RenameFolderAsync(string oldPath, string newPath)
        {
            await new Task(() =>
            {
                var sourceDirectory = new DirectoryInfo(MapStorage(oldPath));
                if (!sourceDirectory.Exists)
                {
                    throw new ArgumentException(string.Format("Directory {0} does not exist", oldPath));
                }
                var targetDirectory = new DirectoryInfo(MapStorage(newPath));
                if (targetDirectory.Exists)
                {
                    throw new ArgumentException(string.Format("Directory {0} already exists", newPath));
                }
                Directory.Move(sourceDirectory.FullName, targetDirectory.FullName);
            });
        }

        /// <summary>
        /// Deletes a file in the storage provider.
        /// </summary>
        /// <param name="path">The relative path to the file to be deleted.</param>
        /// <exception cref="ArgumentException">If the file doesn't exist.</exception>
        public async Task DeleteFileAsync(string path)
        {
            await new Task(() =>
           {
               var fileInfo = new FileInfo(MapStorage(path));
               if (!fileInfo.Exists)
               {
                   throw new ArgumentException(string.Format("File {0} does not exist", path));
               }

               fileInfo.Delete();
           });

        }

        /// <summary>
        /// Renames a file in the storage provider.
        /// </summary>
        /// <param name="oldPath">The relative path to the file to be renamed.</param>
        /// <param name="newPath">The relative path to the new file.</param>
        public async Task RenameFileAsync(string oldPath, string newPath)
        {
            await new Task(() =>
              {
                  var sourceFileInfo = new FileInfo(MapStorage(oldPath));
                  if (!sourceFileInfo.Exists)
                  {
                      throw new ArgumentException(string.Format("File {0} does not exist", oldPath));
                  }
                  var targetFileInfo = new FileInfo(MapStorage(newPath));
                  if (targetFileInfo.Exists)
                  {
                      throw new ArgumentException(string.Format("File {0} already exists", newPath));
                  }
                  File.Move(sourceFileInfo.FullName, targetFileInfo.FullName);
              });
        }

        /// <summary>
        /// Creates a file in the storage provider.
        /// </summary>
        /// <param name="path">The relative path to the file to be created.</param>
        /// <exception cref="ArgumentException">If the file already exists.</exception>
        /// <returns>The created file.</returns>
        public async Task<IStorageFile> CreateFileAsync(string path)
        {
            return await new Task<IStorageFile>(() =>
            {
                FileInfo fileInfo = new(MapStorage(path));
                if (fileInfo.Exists)
                {
                    throw new ArgumentException(string.Format("File {0} already exists", fileInfo.Name));
                }
                // ensure the directory exists
                var dirName = Path.GetDirectoryName(fileInfo.FullName);
                if (dirName != null)
                {
                    if (!Directory.Exists(dirName))
                    {
                        Directory.CreateDirectory(dirName);
                    }
                    File.WriteAllBytes(fileInfo.FullName, Array.Empty<byte>());
                }
                return new FileSystemEntry(Fix(path), fileInfo);
            });
        }

        /// <summary>
        /// Tries to save a stream in the storage provider.
        /// </summary>
        /// <param name="path">The relative path to the file to be created.</param>
        /// <param name="inputStream">The stream to be saved.</param>
        /// <returns>True if success; False otherwise.</returns>
        public async Task<bool> TrySaveStreamAsync(string path, Stream inputStream)
        {
            return await new Task<bool>(() =>
           {
               try
               {
                   SaveStreamAsync(path, inputStream).Wait();
               }
               catch
               {
                   return false;
               }

               return true;
           });
        }

        /// <summary>
        /// Saves a stream in the storage provider.
        /// </summary>
        /// <param name="path">The relative path to the file to be created.</param>
        /// <param name="inputStream">The stream to be saved.</param>
        /// <exception cref="ArgumentException">If the stream can't be saved due to access permissions.</exception>
        public async Task SaveStreamAsync(string path, Stream inputStream)
        {
            await new Task(async () =>
            {
                var file = await CreateFileAsync(path);
                var outputStream = file.OpenWrite();
                var buffer = new byte[8192];
                for (; ; )
                {
                    var length = inputStream.Read(buffer, 0, buffer.Length);
                    if (length <= 0)
                        break;
                    outputStream.Write(buffer, 0, length);
                }
                outputStream.Dispose();
            });
        }
        public async Task<bool> FileExistsAsync(string path)
        {
            return await new Task<bool>(() => new FileInfo(MapStorage(path)).Exists);
        }
        public DateTimeOffset? DefaultSharedAccessExpiration { get; set; }

        public async Task<IStorageFile> CreateOrReplaceFile(string path)
        {
            if (await FileExistsAsync(path))
                await DeleteFileAsync(path);
            return await CreateFileAsync(path);
        }
    }
}
