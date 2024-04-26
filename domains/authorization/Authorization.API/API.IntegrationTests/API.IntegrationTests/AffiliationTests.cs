using API.IntegrationTests.Setup;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.IntegrationTests
{
    public class AffiliationTests : IClassFixture<DatabaseTest>
    {
        private readonly DatabaseTest _test;

        public AffiliationTests(DatabaseTest test)
        {
            _test = test;
        }

        [Fact]
        public async Task CannotAddDuplicateAffiliation()
        {
            using var context = _test.CreateContext();
            var user = new User();
            var organization = new Organization();
            var affiliation1 = Affiliation.Create(user, organization);
            var affiliation2 = Affiliation.Create(user, organization);

            context.Users.Add(user);
            context.Organizations.Add(organization);
            context.Affiliations.Add(affiliation1);
            await context.SaveChangesAsync();

            context.Affiliations.Add(affiliation2);
            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }
    }
}
