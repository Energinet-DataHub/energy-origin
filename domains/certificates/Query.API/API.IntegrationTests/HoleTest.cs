//extern alias registryConnector;
//using System;
//using System.Threading.Tasks;
//using API.IntegrationTests.Factories;
//using API.IntegrationTests.Testcontainers;
//using FluentAssertions;
//using Xunit;

//namespace API.IntegrationTests;

//public class HoleTest : IClassFixture<RegistryConnectorApplicationFactory>, IClassFixture<ProjectOriginStack>
//{
//    private readonly RegistryConnectorApplicationFactory factory;
//    private readonly ProjectOriginStack projectOriginStack;

//    public HoleTest(RegistryConnectorApplicationFactory factory, ProjectOriginStack projectOriginStack)
//    {
//        this.factory = factory;
//        this.projectOriginStack = projectOriginStack;

//        factory.ProjectOriginOptions = projectOriginStack.Options;
//    }

//    [Fact]
//    public async Task Test1()
//    {
//        factory.Start();

//        await Task.Delay(TimeSpan.FromSeconds(70));

//        projectOriginStack.WalletUrl.Should().Be("http://127.0.0.1:7890/");
//    }
//}
