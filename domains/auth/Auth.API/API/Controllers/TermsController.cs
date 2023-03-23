using System.Net.Http.Headers;
using API.Models.Entities;
using API.Options;
using API.Services;
using API.Utilities;
using EnergyOrigin.TokenValidation.Models.Requests;
using EnergyOrigin.TokenValidation.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace API.Controllers;

[Authorize]
[ApiController]
public class TermsController : ControllerBase
{
    [HttpPut()]
    [Route("terms/accept")]
    public async Task<IActionResult> AcceptTermsAsync(
        ILogger<TermsController> logger,
        IHttpContextAccessor accessor,
        IUserDescriptMapper descriptMapper,
        IUserService userService,
        IHttpClientFactory clientFactory,
        IOptions<DataSyncOptions> options,
        [FromBody] AcceptTermsRequest acceptedTermsVersion)
    {
        var descriptor = descriptMapper.Map(User) ?? throw new NullReferenceException($"UserDescriptMapper failed: {User}");

        User user;
        if (descriptor.Id is null)
        {
            user = new User
            {
                Name = descriptor.Name,
                ProviderId = descriptor.ProviderId,
                Tin = descriptor.Tin,
                AllowCPRLookup = descriptor.AllowCPRLookup
            };
        }
        else
        {
            var id = descriptor.Id.Value;
            user = await userService.GetUserByIdAsync(id) ?? throw new NullReferenceException($"GetUserByIdAsync() returned null: {id}");
        }

        user.AcceptedTermsVersion = acceptedTermsVersion.Version;

        await userService.UpsertUserAsync(user);

        if (AuthenticationHeaderValue.TryParse(accessor.HttpContext?.Request.Headers.Authorization, out var authentication))
        {
            var client = clientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = authentication;

            // NOTE/TODO: The jwt and consequencely user descriptor does not yet contain SSN/CPR therefore we are using null as SSN value to create relations.
            //            However this value should be set when available or data sync should be updated to pull SSN and TIN values from the provided jwt instead.
            var result = await client.PostAsJsonAsync<Dictionary<string, object?>>($"{options.Value.Uri.AbsoluteUri}/relations", new()
            {
                { "ssn", null },
                { "tin", user.Tin }
            });

            if (!result.IsSuccessStatusCode)
            {
                logger.LogWarning("AcceptTerms: Unable to create relations for {subject}", user.Id); // TODO: This should be logging the subject when merged with "company changes".
            }
        }

        return NoContent();
    }
}
