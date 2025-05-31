using System;
using DataContext.Models;

namespace API.ReportGenerator.Rendering;

public interface IDisclaimerRenderer
{
    string Render(Language language);
}

public sealed class DisclaimerRenderer : IDisclaimerRenderer
{
    public string Render(Language language)
    {
        var label = DisclaimerLabels.From(language);

        return $"""
                    <div class="disclaimer">
                        <p>{label.Text}</p>
                    </div>
                """.Trim();
    }
}

public sealed record DisclaimerLabels
{
    public required string Text { get; init; }

    public static DisclaimerLabels From(Language language) => language switch
    {
        Language.English => new DisclaimerLabels
        {
            Text = "Data foundation & Approval. Vivamus sagittis lacus vel augue laoreet rutrum faucibus dolor auctor. Fusce dapibus, tellus ac cursus commodo, tortor mauris condimentum nibh, ut fermentum massa justo sit amet risus. Duis mollis, est non commodo luctus, nisi erat porttitor ligula, eget lacinia odio sem nec elit."
        },
        Language.Danish => new DisclaimerLabels
        {
            Text = "Data grundlag & Godkendelse. Vivamus sagittis lacus vel augue laoreet rutrum faucibus dolor auctor. Fusce dapibus, tellus ac cursus commodo, tortor mauris condimentum nibh, ut fermentum massa justo sit amet risus. Duis mollis, est non commodo luctus, nisi erat porttitor ligula, eget lacinia odio sem nec elit."
        },
        _ => throw new ArgumentOutOfRangeException(nameof(language), language, "Unsupported language")
    };
}
