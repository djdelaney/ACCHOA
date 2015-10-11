using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace HOA.Services
{
    public interface IFileStore
    {
        string StoreFile(Stream data);
        Stream RetriveFile(string id);
    }
}
