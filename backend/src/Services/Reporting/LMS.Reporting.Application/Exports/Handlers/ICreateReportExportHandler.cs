using LMS.BuildingBlocks.Core.Operations;
using LMS.Reporting.Application.Exports.Commands;
using LMS.Reporting.Domain.Exports;

namespace LMS.Reporting.Application.Exports.Handlers;

public interface ICreateReportExportHandler
{
    Task<OperationResult<ReportExport>> HandleAsync(CreateReportExportCommand command, CancellationToken cancellationToken);
}
