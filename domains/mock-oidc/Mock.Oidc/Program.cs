using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Mock.Oidc;
using Mock.Oidc.Extensions;
using Mock.Oidc.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.AccessDeniedPath = "/connect/signin";
        options.LoginPath = "/connect/signin";
        options.LogoutPath = "/connect/signout";
    });

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // Configure the context to use an in-memory store.
    options.UseInMemoryDatabase("db");

    // Register the entity sets needed by OpenIddict.
    options.UseOpenIddict();
});

//TODO: Remove commented code if scheduled tasks for e.g. pruning is not needed

// OpenIddict offers native integration with Quartz.NET to perform scheduled tasks
// (like pruning orphaned authorizations/tokens from the database) at regular intervals.
//builder.Services.AddQuartz(options =>
//{
//    options.UseMicrosoftDependencyInjectionJobFactory();
//    options.UseSimpleTypeLoader();
//    options.UseInMemoryStore();
//});

// Register the Quartz.NET service and configure it to block shutdown until jobs are complete.
//builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

// Register the OpenIddict services.
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        // Register the Entity Framework Core models/stores.
        options.UseEntityFrameworkCore().UseDbContext<ApplicationDbContext>();

        // Enable Quartz.NET integration.
        //options.UseQuartz(); //TODO: Include if Quartz is used
    })
    .AddServer(options =>
    {
        // Enable the authorization, token (, introspection and userinfo) endpoints. //TODO: Fix this comment
        options.SetAuthorizationEndpointUris("/connect/authorize")
            .SetTokenEndpointUris("/connect/token")
            .SetLogoutEndpointUris("/connect/logout");
        //.SetIntrospectionEndpointUris("/connect/introspect") //TODO: Is this needed?
        //.SetUserinfoEndpointUris("/connect/userinfo");

        // Enable the authorization code, implicit and the refresh token flows.
        options.AllowAuthorizationCodeFlow()
            .AllowImplicitFlow()
            .AllowRefreshTokenFlow();

        // Expose all the supported claims in the discovery document.
        options.RegisterClaims( //TODO: Correct claims
            "address",
            "birthdate",
            "email",
            "email_verified",
            "family_name",
            "gender",
            "given_name",
            "issuer",
            "locale",
            "middle_name",
            "name",
            "nickname",
            "phone_number",
            "phone_number_verified",
            "picture",
            "preferred_username",
            "profile",
            "subject",
            "updated_at",
            "website",
            "zoneinfo");

        // Expose all the supported scopes in the discovery document.
        options.RegisterScopes( //TODO: Correct scopes
            "address",
            "email",
            "phone",
            "profile");

        // Note: an ephemeral signing key is deliberately used to make the "OP-Rotation-OP-Sig"
        // test easier to run as restarting the application is enough to rotate the keys.
        options.AddEphemeralEncryptionKey()
            .AddEphemeralSigningKey(); //TODO: Follow up on if this is okay in final solution. Doc says this is for development-only, not production.

        // Register the ASP.NET Core host and configure the ASP.NET Core-specific options.
        //
        // Note: the pass-through mode is not enabled for the token endpoint
        // so that token requests are automatically handled by OpenIddict.
        options.UseAspNetCore()
            .EnableLogoutEndpointPassthrough()
            .EnableAuthorizationEndpointPassthrough()
            .EnableAuthorizationRequestCaching();

        // TODO: Enable if user info is added
        // Register the event handler responsible for populating userinfo responses.
        //options.AddEventHandler<OpenIddictServerEvents.HandleUserinfoRequestContext>(options =>
        //    options.UseSingletonHandler<Handlers.PopulateUserinfo>());
    })
    .AddValidation(options =>
    {
        // Import the configuration from the local OpenIddict server instance.
        options.UseLocalServer();

        // Register the ASP.NET Core host.
        options.UseAspNetCore();

        // Enable authorization entry validation, which is required to be able
        // to reject access tokens retrieved from a revoked authorization code.
        options.EnableAuthorizationEntryValidation();
    });

builder.Services.AddHostedService<OpenIddictApplicationSeeder>();

builder.Services.AddFromYamlFile<UserDescriptor[]>(builder.Configuration["OidcFiles:UsersPath"]);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();