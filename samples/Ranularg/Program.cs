using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Ranularg;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddTransient<IClaimsTransformation, CustomClaimsTransformer>();
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "Cookies";
        options.DefaultChallengeScheme = "oidc";
    })
    .AddCookie("Cookies")

    .AddOpenIdConnect("oidc", options =>
    {
        //options.Authority = "https://login.microsoftonline.com/0eeb4972-4d01-4383-8f7c-80a38383d208";
        options.ClientId = "529a55d0-68c7-4129-ba3c-e06d4f1038c4";
        options.ClientSecret = "w3c...";

        options.CallbackPath = "/signin-oidc";
        options.SignedOutCallbackPath = "/signout-callback-oidc";

        options.SaveTokens = true;
        options.ResponseType = OpenIdConnectResponseType.IdTokenToken;

        options.Scope.Add("https://datahubeouenerginet.onmicrosoft.com/529a55d0-68c7-4129-ba3c-e06d4f1038c4/Test.Scope");

        options.GetClaimsFromUserInfoEndpoint = true;

        options.MetadataAddress = "https://datahubeouenerginet.b2clogin.com/datahubeouenerginet.onmicrosoft.com/B2C_1_SUSI/v2.0/.well-known/openid-configuration";
    })
    .AddOpenIdConnect("mitid", options =>
    {
        options.ClientId = "529a55d0-68c7-4129-ba3c-e06d4f1038c4";
        options.ClientSecret = "w3c...";
        options.ResponseType = OpenIdConnectResponseType.IdTokenToken;
        options.CallbackPath = "/signin-oidc-mitid";
        options.SignedOutCallbackPath = "/signout-callback-oidc";
        options.MetadataAddress = "https://datahubeouenerginet.b2clogin.com/datahubeouenerginet.onmicrosoft.com/B2C_1_SUSI_MitID/v2.0/.well-known/openid-configuration";
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;

        options.Scope.Add("https://datahubeouenerginet.onmicrosoft.com/529a55d0-68c7-4129-ba3c-e06d4f1038c4/Test.Scope");

        options.Events.OnTokenValidated = async context =>
        {
            var claimsIdentity = (ClaimsIdentity)context.Principal.Identity;
            foreach (var claim in claimsIdentity.Claims)
            {
                Console.WriteLine($"Claim Type: {claim.Type}, Claim Value: {claim.Value}");
            }
        };
    })
    .AddOpenIdConnect("mitiddirect", options =>
    {
        options.ClientId = "627f6de4-a1fc-43ef-b811-d88391075f24";
        options.ClientSecret = "8IQ...";

        options.ResponseType = OpenIdConnectResponseType.Code;
        options.CallbackPath = "/signin-oidc-mitiddirect";
        options.SignedOutCallbackPath = "/signout-callback-oidc";
        options.MetadataAddress = "https://pp.netseidbroker.dk/op/.well-known/openid-configuration";
        options.SaveTokens = true;
        options.MapInboundClaims = true;
        options.GetClaimsFromUserInfoEndpoint = true;

        options.Scope.Clear();
        options.Scope.Add("openid ssn private_to_business nemid mitid");

        // https://www.signaturgruppen.dk/download/broker/docs/Nets%20eID%20Broker%20Identity%20Providers.pdf
        options.ClaimActions.MapJsonKey("mitid.uuid", "mitid.uuid");
        options.ClaimActions.MapJsonKey("mitid.age", "mitid.age");
        options.ClaimActions.MapJsonKey("mitid.date_of_birth", "mitid.date_of_birth");
        options.ClaimActions.MapJsonKey("mitid.identity_name", "mitid.identity_name");

        options.Events.OnTokenResponseReceived = async context =>
        {

        };

        options.Events.OnAuthorizationCodeReceived = async context =>
        {

        };

        options.Events.OnTicketReceived = async context =>
        {


        };

        options.Events.OnUserInformationReceived = async context =>
        {

        };

        options.Events.OnTokenValidated = async context =>
        {

        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();


namespace Ranularg
{
}
//diff
