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
// https://github.com/dotnet/AspNetCore.Docs/blob/main/aspnetcore/grpc/test-services/sample/Tests/Server/IntegrationTests/Helpers/GrpcTestContext.cs
#endregion

using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Tests.Fixtures.TestServerHelpers
{
    internal class TestServerContext<TStartup> : IDisposable where TStartup : class
    {
        private readonly Stopwatch _stopwatch;
        private readonly TestServerFixture<TStartup> _fixture;
        private readonly ITestOutputHelper _outputHelper;

        public TestServerContext(TestServerFixture<TStartup> fixture, ITestOutputHelper outputHelper)
        {
            _stopwatch = Stopwatch.StartNew();
            _fixture = fixture;
            _outputHelper = outputHelper;
            _fixture.LoggedMessage += WriteMessage;
        }

        private void WriteMessage(LogLevel logLevel, string category, EventId eventId, string message, Exception? exception)
        {
            var log = $"{_stopwatch.Elapsed.TotalSeconds:N3}s {category} - {logLevel}: {message}";
            if (exception != null)
            {
                log += Environment.NewLine + exception.ToString();
            }
            _outputHelper.WriteLine(log);
        }

        public void Dispose()
        {
            _fixture.LoggedMessage -= WriteMessage;
        }
    }
}
