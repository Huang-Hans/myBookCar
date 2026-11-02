using System.Security.Cryptography;
using System.Text.RegularExpressions;
using LMS.Platform.Application.Files.Models;
using LMS.Platform.Application.Files.Options;
using LMS.Platform.Application.Files.Ports;
using Microsoft.Extensions.Options;

namespace LMS.Platform.Infrastructure.Files;

// 本地磁盘存储：文件名只取内部生成的存储引用（拒绝任何路径字符），先写临时文件，成功后以不覆盖的方式移动到位。
internal sealed partial class LocalDiskFileContentStore(IOptions<FileStorageOptions> options) : IFileContentStore
{
    private const int BufferSize = 81920;

    public async Task<StoredContent> WriteAsync(string storageReference, Stream content, long maxBytes, CancellationToken cancellationToken)
    {
        var target = ResolvePath(storageReference);
        if (File.Exists(target))
        {
            return new StoredContent(StoreOutcome.AlreadyExists, 0, string.Empty);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        var temporary = $"{target}.{Guid.NewGuid():N}.part";
        try
        {
            var copied = await CopyAsync(content, temporary, maxBytes, cancellationToken);

            return copied.Outcome == StoreOutcome.Stored ? Promote(temporary, target, copied) : copied;
        }
        finally
        {
            DeleteQuietly(temporary);
        }
    }

    public Task DeleteAsync(string storageReference, CancellationToken cancellationToken)
    {
        DeleteQuietly(ResolvePath(storageReference));

        return Task.CompletedTask;
    }

    public Stream OpenRead(string storageReference) =>
        new FileStream(ResolvePath(storageReference), FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true);

    private static async Task<StoredContent> CopyAsync(Stream content, string temporaryPath, long maxBytes, CancellationToken cancellationToken)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        await using var output = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);
        var buffer = new byte[BufferSize];
        long total = 0;
        int read;
        while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
        {
            total += read;
            if (total > maxBytes)
            {
                return new StoredContent(StoreOutcome.Exceeded, total, string.Empty);
            }

            hash.AppendData(buffer, 0, read);
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        return new StoredContent(StoreOutcome.Stored, total, Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant());
    }

    private static StoredContent Promote(string temporaryPath, string targetPath, StoredContent copied)
    {
        try
        {
            File.Move(temporaryPath, targetPath, overwrite: false);
            return copied;
        }
        catch (IOException) when (File.Exists(targetPath))
        {
            return new StoredContent(StoreOutcome.AlreadyExists, 0, string.Empty);
        }
    }

    private string ResolvePath(string storageReference)
    {
        if (!ReferencePattern().IsMatch(storageReference))
        {
            throw new InvalidOperationException("The storage reference is not a valid internal reference.");
        }

        return Path.Combine(Path.GetFullPath(options.Value.RootPath), storageReference);
    }

    private static void DeleteQuietly(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // 清理失败不影响结果：临时文件名唯一，残留文件不会被读取。
        }
    }

    [GeneratedRegex("^[a-z0-9]{1,64}$", RegexOptions.CultureInvariant)]
    private static partial Regex ReferencePattern();
}
