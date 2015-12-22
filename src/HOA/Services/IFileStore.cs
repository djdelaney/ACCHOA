using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace HOA.Services
{
    public interface IFileStore
    {
        Task<string> StoreFile(Stream data);
        Task<Stream> RetriveFile(string id);
        Task DeleteFile(string id);
    }
}
