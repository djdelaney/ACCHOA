using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HOA.Services
{
    public class MockFileStore : IFileStore
    {
        public Task DeleteFile(string id)
        {
            return Task.FromResult<object>(null);
        }

        Task<Stream> IFileStore.RetriveFile(string id)
        {
            throw new NotImplementedException();
        }

        Task<string> IFileStore.StoreFile(Stream data)
        {
            return Task.FromResult(Guid.NewGuid().ToString());
        }
    }
}
