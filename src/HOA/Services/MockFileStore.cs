using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HOA.Services
{
    public class MockFileStore : IFileStore
    {
        public Stream RetriveFile(string id)
        {
            throw new NotImplementedException();
        }

        public string StoreFile(Stream data)
        {
            return Guid.NewGuid().ToString();
        }
    }
}
