// using Microsoft.OpenApi.Models;
// using Swashbuckle.AspNetCore.SwaggerGen;
//
// namespace API.Authorization.Swagger;
//
// public class AddContractsTagDocumentFilter : IDocumentFilter
// {
//     public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
//     {
//         swaggerDoc.Tags.Add(new OpenApiTag
//         {
//             Name = "Contracts",
//             Description = "Contracts are key in Energy Origin." +
//                           " When you have an active contract for at metering point" +
//                           " Granular Certificates will be generated until the end of the contract." +
//                           " It applies to both production and consumption metering points." +
//                           " However, the production metering points must be either Wind or Solar" +
//                           " – otherwise it is not possible to generate GCs." +
//                           " When a contract is inactive Granular Certificates will no longer be generated."
//         });
//     }
// }
