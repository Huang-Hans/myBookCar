using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LMS.BuildingBlocks.Http.Requests.Contracts;

namespace LMS.Platform.Api.Contracts.Files;

[AllowRequestBody]
public sealed class FileUploadQuery : QueryRequest
{
    [Required(ErrorMessage = "Is required")]
    [Description("上传地址的签名，由 CB05 返回的上传地址携带；只覆盖单个文件和失效时间。")]
    public string? Signature { get; set; }

    [Required(ErrorMessage = "Is required")]
    [Description("签名失效时间，Unix 秒；由 CB05 返回的上传地址携带。")]
    public long? Expires { get; set; }
}
