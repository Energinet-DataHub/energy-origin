using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Mock.Oidc;

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