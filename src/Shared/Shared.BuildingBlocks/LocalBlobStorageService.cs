using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.BuildingBlocks;

public class LocalBlobStorageService : IBlobStorageService
{
    private readonly string _rootDirectory;

    public LocalBlobStorageService(string rootDirectory)
    {
        _rootDirectory = Path.GetFullPath(rootDirectory);
        if (!Directory.Exists(_rootDirectory))
        {
            Directory.CreateDirectory(_rootDirectory);
        }
    }

    private string GetFullPath(string path)
    {
        path = path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(_rootDirectory, path));
        if (!fullPath.StartsWith(_rootDirectory, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid blob path");
        }
        return fullPath;
    }

    public async Task UploadAsync(string path, Stream data, string contentType, CancellationToken ct = default)
    {
        var fullPath = GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (directory != null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
        await data.CopyToAsync(fileStream, ct);
    }

    public Task<Stream> DownloadAsync(string path, CancellationToken ct = default)
    {
        var fullPath = GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Blob not found at {path}");
        }

        Stream fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        return Task.FromResult(fileStream);
    }

    public Task<bool> ExistsAsync(string path, CancellationToken ct = default)
    {
        var fullPath = GetFullPath(path);
        return Task.FromResult(File.Exists(fullPath));
    }

    public Task DeleteAsync(string path, CancellationToken ct = default)
    {
        var fullPath = GetFullPath(path);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        return Task.CompletedTask;
    }

    public Task DeleteDirectoryAsync(string pathPrefix, CancellationToken ct = default)
    {
        var fullPath = GetFullPath(pathPrefix);
        if (Directory.Exists(fullPath))
        {
            Directory.Delete(fullPath, true);
        }
        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> ListPathsAsync(string pathPrefix, CancellationToken ct = default)
    {
        var fullPath = GetFullPath(pathPrefix);
        if (!Directory.Exists(fullPath))
        {
            return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());
        }

        var files = Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories);
        var relativePaths = files.Select(f => Path.GetRelativePath(_rootDirectory, f).Replace(Path.DirectorySeparatorChar, '/'));
        
        return Task.FromResult<IEnumerable<string>>(relativePaths.ToList());
    }

    public Task<Uri?> GetSignedDownloadUriAsync(string path, TimeSpan expiry, CancellationToken ct = default)
    {
        return Task.FromResult<Uri?>(null);
    }
}
