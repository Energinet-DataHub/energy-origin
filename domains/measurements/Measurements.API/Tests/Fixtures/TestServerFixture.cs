#region Copyright notice and license
// Copyright 2019 The gRPC Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// https://github.com/dotnet/AspNetCore.Docs/blob/main/aspnetcore/grpc/test-services/sample/Tests/Server/IntegrationTests/Helpers/GrpcTestFixture.cs
#endregion

using System;
using System.Collections.Generic;
using System.Net.Http;
using EnergyOrigin.Setup;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tests.Fixtures.TestServerHelpers;
using Xunit;

namespace Tests.Fixtures
{
    public delegate void LogMessage(LogLevel logLevel, string categoryName, EventId eventId, string message, Exception? exception);

    public class TestServerFixture<TStartup> : IDisposable where TStartup : class
    {

        private TestServer? _server;
        private IHost? _host;
        private HttpMessageHandler? _handler;
        private GrpcChannel? _channel;
        private Dictionary<string, string?>? _configurationDictionary;
        private bool _disposed;
        public event Action<IServiceCollection>? ConfigureTestServices;

        public event LogMessage? LoggedMessage;
        public GrpcChannel Channel => _channel ??= CreateGrpcChannel();

        public TestServerFixture()
        {
            LoggerFactory = new LoggerFactory();
            LoggerFactory.AddProvider(new ForwardingLoggerProvider((logLevel, category, eventId, message, exception) =>
            {
                LoggedMessage?.Invoke(logLevel, category, eventId, message, exception);
            }));
        }

        public T GetRequiredService<T>() where T : class
        {
            EnsureServer();
            return _host!.Services.GetRequiredService<T>();
        }

        public void ConfigureHostConfiguration(Dictionary<string, string?> configuration)
        {
            _configurationDictionary = configuration;
        }

        public void RefreshHostAndGrpcChannelOnNextClient()
        {
            _host = null;
            _channel = null;
        }

        private void EnsureServer(string environment = "Development")
        {
            if (_host == null)
            {
                var builder = new HostBuilder();
                if (_configurationDictionary != null)
                {
                    builder.ConfigureHostConfiguration(config =>
                        {
                            config.Add(new MemoryConfigurationSource()
                            {
                                InitialData = _configurationDictionary
                            });
                        });
                }

                builder
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton<ILoggerFactory>(LoggerFactory);
                    })
                    .ConfigureWebHostDefaults(webHost =>
                    {
                        webHost
                            .UseTestServer()
                            .UseEnvironment(environment)
                            .UseStartup<TStartup>();
                    })
                    .ConfigureServices(services =>
                    {
                        if (ConfigureTestServices != null)
                            ConfigureTestServices.Invoke(services);
                    });

                _host = builder.Start();
                _server = _host.GetTestServer();
                _handler = _server.CreateHandler();
            }
        }

        public LoggerFactory LoggerFactory { get; }

        private GrpcChannel CreateGrpcChannel()
        {
            EnsureServer();

            return GrpcChannel.ForAddress(_server!.BaseAddress, new GrpcChannelOptions
            {
                LoggerFactory = LoggerFactory,
                HttpHandler = _handler
            });
        }

        public HttpClient CreateUnauthenticatedClient(string environment = "Development")
        {
            EnsureServer(environment);

            var client = _server!.CreateClient();
            client.DefaultRequestHeaders.Add("X-API-Version", ApiVersions.Version1);
            return client;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _channel?.Dispose();
                    _handler?.Dispose();
                    _host?.Dispose();
                    _server?.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~TestServerFixture()
        {
            Dispose(false);
        }

        public IDisposable GetTestLogger(ITestOutputHelper outputHelper)
        {
            return new TestServerContext<TStartup>(this, outputHelper);
        }
    }
}

