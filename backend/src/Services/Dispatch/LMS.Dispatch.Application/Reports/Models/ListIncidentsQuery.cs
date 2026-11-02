using LMS.BuildingBlocks.Core.Querying;
using LMS.Dispatch.Domain.Reports;

namespace LMS.Dispatch.Application.Reports.Models;

public sealed record ListIncidentsQuery(ListQuery List, string DriverId, IncidentType? Type, IncidentStatus? Status);
