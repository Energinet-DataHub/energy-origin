using API.Transfer.Api.Controllers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace API.Transfer;

public class AddTransferTagDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Tags.Add(new OpenApiTag
        {
            Name = "Reports",
            Description = """
                          The Reports endpoints allow you to request, monitor, and download PDF reports
                          for production and consumption data in Energy Track & Trace.
                          """
        });

        swaggerDoc.Tags.Add(new OpenApiTag
        {
            Name = nameof(ReportsController).Replace("Controller", string.Empty),
            Description = """
                          • **RequestReportGeneration** (POST `/api/reports`):
                            Kick off an asynchronous report job for a given organization and date range.
                          • **GetReportStatuses** (GET `/api/reports?organizationId={guid}`):
                            List all pending/completed report jobs for an organization.
                          • **DownloadReport** (GET `/api/reports/{reportId}/download?organizationId={guid}`):
                            Retrieve the generated PDF for a completed report.
                          """
        });
    }
}
