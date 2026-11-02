using LMS.Reporting.Application.Files.Models;

namespace LMS.Reporting.Application.Files.Ports;

public interface IFileContentStore
{
    // 流式写入并计算摘要，最多接受 maxBytes 字节；storageReference 是内部引用，不含用户提供的名称。
    Task<StoredContent> WriteAsync(string storageReference, Stream content, long maxBytes, CancellationToken cancellationToken);

    Task DeleteAsync(string storageReference, CancellationToken cancellationToken);

    // 打开已存储内容用于顺序读取；调用方负责释放。引用必须是内部生成的存储引用。
    Stream OpenRead(string storageReference);
}
