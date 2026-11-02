using System.ComponentModel;
using LMS.BuildingBlocks.Http.Requests.Contracts;
using LMS.Platform.Application.Configuration.Models;

namespace LMS.Platform.Api.Contracts.Configuration;

public sealed class IntegrationReferenceListRequest : QueryRequest
{
    [Description("集成分组：缺省为 SYSTEM（系统集成卡片），NAVIGATION 为地图服务。")]
    public IntegrationGroup Group { get; set; } = IntegrationGroup.System;
}
