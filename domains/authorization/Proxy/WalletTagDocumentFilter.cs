using System.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Proxy;

public class WalletTagDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // Check if the "Contracts" tag already exists to avoid duplicates
        if (!swaggerDoc.Tags.Any(tag => tag.Name == "Wallet"))
        {
            swaggerDoc.Tags.Add(new OpenApiTag
            {
                Name = "Wallet",
                Description = "The Wallet is essential for Energy Origin," +
                              " since it keeps track of all the user’s Granular Certificates" +
                              " – both the ones generated from the user’s own metering points," +
                              " but also the ones transferred from other users." +
                              " In other words, the Wallet will hold all available certificates for the user." +
                              " Moreover, it will show all transfers, that may have been made," +
                              " to other users’ wallets as well.\n"
            });
        }
    }
}
