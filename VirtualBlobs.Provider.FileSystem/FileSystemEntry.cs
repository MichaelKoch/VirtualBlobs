using VirtualBlobs.Interfaces;

namespace VirtualBlobs.Providers.FileSystem
{
    public class FileSystemEntry : IStorageFile
    {
        private readonly string _path;
        private readonly FileInfo _fileInfo;

        public FileSystemEntry(string path, FileInfo fileInfo)
        {
            _path = path;
            _fileInfo = fileInfo;
        }

        public string GetPath()
        {
            return _path;
        }

        public string GetName()
        {
            return _fileInfo.Name;
        }

        public long GetSize()
        {
            return _fileInfo.Length;
        }

        public DateTime GetLastUpdated()
        {
            return _fileInfo.LastWriteTime;
        }

        public string GetFileType()
        {

            return _fileInfo.Extension;
        }



        public Stream OpenRead()
        {
            return new FileStream(_fileInfo.FullName, FileMode.Open, FileAccess.Read);
        }

        public Stream OpenWrite()
        {
            return new FileStream(_fileInfo.FullName, FileMode.Open, FileAccess.ReadWrite);
        }

        public Stream CreateFile()
        {
            return new FileStream(_fileInfo.FullName, FileMode.Truncate, FileAccess.ReadWrite);
        }
    }
}