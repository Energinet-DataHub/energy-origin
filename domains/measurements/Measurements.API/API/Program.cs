using API.Helpers;
using API.Services;
using API.Validation;
using FluentValidation.AspNetCore;
using MicroElements.Swashbuckle.FluentValidation;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;
using Serilog.Formatting.Json;


[assembly: InternalsVisibleTo("Tests")]

var builder = WebApplication.CreateBuilder(args);

var logger = new LoggerConfiguration()
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger(); 
   
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);

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

//builder.Services.AddHttpClient();

builder.Services.AddHttpClient<IDataSyncService, DataSyncService>(client =>
{
    client.BaseAddress = new Uri(Configuration.GetDataSyncEndpoint());
});
builder.Services.AddScoped<IMeasurementsService, MeasurementsService>();
builder.Services.AddScoped<IConsumptionAggregator, ConsumptionAggregation>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (builder.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.UseHttpLogging();

app.MapControllers();

app.Run();
