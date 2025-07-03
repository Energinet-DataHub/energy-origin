using API.ReportGenerator.Processing;

namespace API.ReportGenerator.Rendering;

public interface IOtherCoverageRenderer
{
    string Render(CoveragePercentage coverage);
}

public class OtherCoverageRenderer : IOtherCoverageRenderer
{
    public string Render(CoveragePercentage coverage)
    {
        var html = @$"<div class=""other-coverage"">
                        <div data-property-1=""daily-coverage"" style=""width: 118px; height: 13px; justify-content: flex-start; align-items: center; gap: 3px; display: inline-flex"">
                            <div style=""color: #002433; font-size: 10px; font-family: OpenSansNormal; font-weight: 400; line-height: 13px; word-wrap: break-word"">Dagsdækning</div>
                            <div style=""color: #002433; font-size: 10px; font-family: OpenSansNormal; font-weight: 400; line-height: 13px; word-wrap: break-word"">{coverage.DailyPercentage:F0}%</div>
                        </div>
                        <div data-property-1=""weekly-coverage"" style=""width: 118px; height: 13px; justify-content: flex-start; align-items: center; gap: 3px; display: inline-flex"">
                            <div style=""color: #002433; font-size: 10px; font-family: OpenSansNormal; font-weight: 400; line-height: 13px; word-wrap: break-word"">Ugedækning</div>
                            <div style=""color: #002433; font-size: 10px; font-family: OpenSansNormal; font-weight: 400; line-height: 13px; word-wrap: break-word"">{coverage.WeeklyPercentage:F0}%</div>
                        </div>
                        <div data-property-1=""monthly-coverage"" style=""width: 118px; height: 13px; justify-content: flex-start; align-items: center; gap: 3px; display: inline-flex"">
                            <div style=""color: #002433; font-size: 10px; font-family: OpenSansNormal; font-weight: 400; line-height: 13px; word-wrap: break-word"">Månedsdækning</div>
                            <div style=""color: #002433; font-size: 10px; font-family: OpenSansNormal; font-weight: 400; line-height: 13px; word-wrap: break-word"">{(coverage.MonthlyPercentage == null ? "N/A" : coverage.MonthlyPercentage.Value.ToString("F0") + "%")}</div>
                        </div>
                        <div data-property-1=""yearly-coverage"" style=""width: 118px; height: 13px; justify-content: flex-start; align-items: center; gap: 3px; display: inline-flex"">
                            <div style=""color: #002433; font-size: 10px; font-family: OpenSansNormal; font-weight: 400; line-height: 13px; word-wrap: break-word"">Årsdækning</div>
                            <div style=""color: #002433; font-size: 10px; font-family: OpenSansNormal; font-weight: 400; line-height: 13px; word-wrap: break-word"">{(coverage.YearlyPercentage == null ? "N/A" : coverage.YearlyPercentage.Value.ToString("F0") + "%")}</div>
                        </div>
                    </div>";

        return html.Trim();
    }
}
