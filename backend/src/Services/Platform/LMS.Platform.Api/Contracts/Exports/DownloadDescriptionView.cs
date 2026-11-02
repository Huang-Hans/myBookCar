namespace LMS.Platform.Api.Contracts.Exports;

/// <summary>
/// 经当前授权签发的结果下载描述。
/// </summary>
/// <param name="Url">短效下载地址，自带签名和失效时间，无需 Bearer。</param>
/// <param name="FileName">下载文件名。</param>
/// <param name="ContentType">结果文件的媒体类型。</param>
/// <param name="ExpiresAt">地址失效时间（UTC）。</param>
public sealed record DownloadDescriptionView(string Url, string FileName, string ContentType, DateTime ExpiresAt);
