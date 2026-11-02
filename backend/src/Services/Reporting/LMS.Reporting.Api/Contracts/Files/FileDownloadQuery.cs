using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LMS.BuildingBlocks.Http.Requests.Contracts;

namespace LMS.Reporting.Api.Contracts.Files;

public sealed class FileDownloadQuery : QueryRequest
{
    [Required(ErrorMessage = "Is required")]
    [Description("下载地址的签名，由 EX03 返回的下载地址携带；只覆盖单个文件和失效时间。")]
    public string? Signature { get; set; }

    [Required(ErrorMessage = "Is required")]
    [Description("签名失效时间，Unix 秒；由下载地址携带。")]
    public long? Expires { get; set; }
}
