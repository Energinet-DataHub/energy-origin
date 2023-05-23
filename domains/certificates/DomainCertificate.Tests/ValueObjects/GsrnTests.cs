using DomainCertificate.ValueObjects;
using Xunit;

namespace DomainCertificate.Tests.ValueObjects
{
    public class GsrnTests
    {
        [Theory]
        [InlineData("571234567890123456")]
        public void Ctor_Success(string value)
        {
            var gsrn = new Gsrn(value);

            Assert.Equal(value, gsrn.Value);
        }

        [Theory]
        [InlineData("471234567890123456")]
        [InlineData("57123456789012345")]
        [InlineData("5712345678901234567")]
        public void Ctor_Fail(string value)
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var gsrn = new Gsrn(value);
            });
        }
    }
}
