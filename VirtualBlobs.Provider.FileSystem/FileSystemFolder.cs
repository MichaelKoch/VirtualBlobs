using VirtualBlobs.Interfaces;

namespace VirtualBlobs.Providers.FileSystem
{
    public class FileSystemFolder : IStorageFolder
    {
        private readonly string _path;
        private readonly DirectoryInfo _directoryInfo;

        public FileSystemFolder(string path, DirectoryInfo directoryInfo)
        {
            _path = path;
            _directoryInfo = directoryInfo;
        }

        public string GetPath()
        {
            return _path;
        }

        public string GetName()
        {
            return _directoryInfo.Name;
        }

        public DateTime GetLastUpdated()
        {
            return _directoryInfo.LastWriteTime;
        }

        public long GetSize()
        {
            return GetDirectorySize(_directoryInfo);
        }

        public IStorageFolder GetParent()
        {
            var dirName = Path.GetDirectoryName(_path);

            if ((_directoryInfo.Parent != null) && (dirName != null))
            {
                return new FileSystemFolder(dirName, _directoryInfo.Parent);
            }
            throw new ArgumentException(string.Format("Directory {0} does not have a parent directory", _directoryInfo.Name));
        }



        private static long GetDirectorySize(DirectoryInfo directoryInfo)
        {
            long size = 0;

            FileInfo[] fileInfos = directoryInfo.GetFiles();
            foreach (FileInfo fileInfo in fileInfos)
            {
                size += fileInfo.Length;
            }
            DirectoryInfo[] directoryInfos = directoryInfo.GetDirectories();
            foreach (DirectoryInfo dInfo in directoryInfos)
            {
                size += GetDirectorySize(dInfo);
            }
            return size;
        }
    }
}