using Mock.Oidc.Extensions;
using Mock.Oidc.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddFromYamlFile<UserDescriptor[]>(builder.Configuration["SettingFiles:UsersPath"]);
builder.Services.AddFromYamlFile<ClientDescriptor[]>(builder.Configuration["SettingFiles:ClientsPath"]);

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