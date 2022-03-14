using DataSync.API.Controllers;
using Xunit;

namespace Tests
{
    public class DummyTest
    {
        [Fact]
        public void Test1()
        {
            Assert.Equal(1, 1);
        }

        [Fact]
        public void InstantiateHelloWorldController()
        {
            var controller = new HelloWorldController();

            Assert.NotNull(controller);
        }
    }
}