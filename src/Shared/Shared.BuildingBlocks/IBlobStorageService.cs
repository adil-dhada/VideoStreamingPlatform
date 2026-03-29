using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.BuildingBlocks;

public interface IBlobStorageService
{
    Task UploadAsync(string path, Stream data, string contentType, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string path, CancellationToken ct = default);
    Task<bool> ExistsAsync(string path, CancellationToken ct = default);
    Task DeleteAsync(string path, CancellationToken ct = default);
    Task DeleteDirectoryAsync(string pathPrefix, CancellationToken ct = default);
    Task<IEnumerable<string>> ListPathsAsync(string pathPrefix, CancellationToken ct = default);
    Task<Uri?> GetSignedDownloadUriAsync(string path, TimeSpan expiry, CancellationToken ct = default);
}
