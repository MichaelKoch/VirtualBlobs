using System;

namespace VirtualBlobs.Interfaces
{
    public interface IStorageFolder
    {
        string GetPath();
        string GetName();
        long GetSize();
        DateTime GetLastUpdated();
        IStorageFolder GetParent();
    }
}