using LMS.Platform.Application.Abstractions;
using LMS.Platform.Application.Locations.Models;
using LMS.Platform.Domain.Locations;

namespace LMS.Platform.Application.Locations.Commands;

// 替换一个区域内的完整地点顺序：Items 必须恰好包含该区域的全部地点，每个地点出现一次。
public sealed record ReplaceLocationOrderCommand(
    LocationRegion Region,
    IReadOnlyList<LocationOrderItem> Items,
    CommandContext Context);
