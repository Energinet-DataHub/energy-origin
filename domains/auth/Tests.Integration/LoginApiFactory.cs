using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using API;
using API.Repositories;
using API.Repositories.Data;
using API.Services;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace Tests.Integration
{
    public class LoginApiFactory : WebApplicationFactory<IApiMarker>, IAsyncLifetime
    {
        private readonly PostgreSqlTestcontainer testContainer
            = new ContainerBuilder<PostgreSqlTestcontainer>()
           .WithDatabase(new PostgreSqlTestcontainerConfiguration
           {
               Database = "Database",
               Username = "admin",
               Password = "admin",
           })
           .WithImage("postgres").
            WithPortBinding(5432,5432)
           .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
           .Build();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {

            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
            });

            builder.ConfigureTestServices(services =>
            {
                //Remove DataSyncSyncerWorker
                services.Remove(services.First(s => s.ImplementationType == typeof(DataContext)));

                services.AddDbContext<DataContext>(options =>
                {
                    options.UseNpgsql(testContainer.ConnectionString);
                });

                services.AddScoped<IUserDataContext, DataContext>();
            });
        }

        public HttpClient CreateUnauthenticatedClient() => CreateClient();

        public async Task InitializeAsync()
        {
            try
            {
                await testContainer.StartAsync();
                var scope = Services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
                await dbContext.Database.MigrateAsync();
            }
            catch (Exception e)
            {
                var d = e;
               throw;
            }
            
        }
        async Task IAsyncLifetime.DisposeAsync()
        {
            await testContainer.DisposeAsync();
        }
    }
}
