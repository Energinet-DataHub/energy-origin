using System.Text.Json.Serialization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using API.Helpers;
using API.Services;
using API.Validation;
using FluentValidation.AspNetCore;
using MicroElements.Swashbuckle.FluentValidation;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;

[assembly: InternalsVisibleTo("Tests")]

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.AddFluentValidation(c =>
{
    c.RegisterValidatorsFromAssemblyContaining<MeasurementsRequestValidator>();
    c.ValidatorFactoryType = typeof(HttpContextServiceProviderValidatorFactory);
});


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddFluentValidationRulesToSwagger();

builder.Services.AddHttpClient();

builder.Services.AddHttpClient<IDataSyncService, DataSyncService>(client =>
{
    client.BaseAddress = new Uri(Configuration.GetDataSyncEndpoint());
});
builder.Services.AddScoped<IMeasurementsService, MeasurementsService>();
builder.Services.AddScoped<IAggregateMeasurements, AggregateMeasurements>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (builder.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
