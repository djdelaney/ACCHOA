using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HOA.Services
{
    public class MockFileStore : IFileStore
    {
        Task<Stream> IFileStore.RetriveFile(string id)
        {
            throw new NotImplementedException();
        }

        async Task<string> IFileStore.StoreFile(Stream data)
        {
            return Guid.NewGuid().ToString();
        }
    }
}
