using API.Configuration;
using API.Models;
using Markdig;
using Microsoft.Extensions.Options;

namespace API.Repository;

public class PrivacyPolicyStorage : IPrivacyPolicyStorage
{
    private readonly AuthOptions authOptions;

    public PrivacyPolicyStorage(IOptions<AuthOptions> authOptions) => this.authOptions = authOptions.Value;

    public async Task<PrivacyPolicy> Get()
    {
        var directory = new DirectoryInfo(authOptions.TermsMarkdownFolder);

        var newestFile = directory
            .GetFiles()
            .OrderByDescending(f => f.LastWriteTime)
            .First();

        var markdown = await File.ReadAllTextAsync(newestFile.FullName);

        return new PrivacyPolicy(
            Markdown.ToHtml(markdown),
            newestFile.Name.Split('.')[0],
            nameof(PrivacyPolicy)
        );
    }
}
