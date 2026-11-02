using LMS.BuildingBlocks.Core.Querying;
using LMS.Dispatch.Domain.Reports;

namespace LMS.Dispatch.Application.Reports.Models;

public sealed record ListVehicleIssuesQuery(ListQuery List, string DriverId, VehicleIssueStatus? Status);
