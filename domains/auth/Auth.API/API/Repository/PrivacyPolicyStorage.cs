using API.Configuration;
using API.Models;
using Markdig;
using Microsoft.Extensions.Options;

namespace API.Repository;

public class PrivacyPolicyStorage : IPrivacyPolicyStorage
{
    private readonly AuthOptions authOptions;

    public PrivacyPolicyStorage(IOptions<AuthOptions> authOptions) => this.authOptions = authOptions.Value;

    public async Task<PrivacyPolicy> GetLatestVersion()
    {
        var directory = new DirectoryInfo(authOptions.TermsMarkdownFolder);

        var newestVersion = directory
            .GetFiles()
            .OrderByDescending(f => f.Name)
            .First();

        var markdown = await File.ReadAllTextAsync(newestVersion.FullName);

        return new PrivacyPolicy(
            Markdown.ToHtml(markdown),
            newestVersion.Name.Split('.')[0],
            nameof(PrivacyPolicy)
        );
    }
}
