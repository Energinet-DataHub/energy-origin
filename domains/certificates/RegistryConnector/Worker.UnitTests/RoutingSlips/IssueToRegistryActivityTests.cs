using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using RegistryConnector.Worker.RoutingSlips;
using Xunit;

namespace RegistryConnector.Worker.UnitTests.RoutingSlips;

public class IssueToRegistryActivityTests
{
    public IssueToRegistryActivityTests()
    {

    }

    [Fact]
    public void ShouldIssueToRegistry()
    {
        var optionsMock = Substitute.For<IOptions<ProjectOriginOptions>>();
        var loggerMock = Substitute.For<ILogger<IssueToRegistryActivity>>();

        //var slip = new IssueToRegistryActivity(optionsMock, loggerMock);

    }
}
