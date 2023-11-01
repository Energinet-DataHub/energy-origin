using API.IntegrationTests.Factories;

namespace API.IntegrationTests;

using Xunit;

[CollectionDefinition("HealthControllerTests")]
public class HealthControllerTestsCollection : ICollectionFixture<TransferAgreementsApiWebApplicationFactory> { }

[CollectionDefinition("SwaggerTests")]
public class SwaggerTestsCollection : ICollectionFixture<TransferAgreementsApiWebApplicationFactory> { }

[CollectionDefinition("ClaimAutomationControllerTest")]
public class ClaimAutomationControllerTestCollection : ICollectionFixture<TransferAgreementsApiWebApplicationFactory> { }

[CollectionDefinition("ConnectionInvitationsControllerTests")]
public class ConnectionInvitationsControllerTestsCollection : ICollectionFixture<TransferAgreementsApiWebApplicationFactory> { }

[CollectionDefinition("ConnectionsControllerTests")]
public class ConnectionsControllerTestsCollection : ICollectionFixture<TransferAgreementsApiWebApplicationFactory> { }

[CollectionDefinition("ConnectionInvitationCleanupServiceTests")]
public class ConnectionInvitationCleanupServiceTests : ICollectionFixture<TransferAgreementsApiWebApplicationFactory> { }

[CollectionDefinition("CvrControllerTests")]
public class CvrControllerTestsCollection : ICollectionFixture<TransferAgreementsApiWebApplicationFactory> { }

[CollectionDefinition("TransferAgreementAutomationControllerTest")]
public class TransferAgreementAutomationControllerTestCollection : ICollectionFixture<TransferAgreementsApiWebApplicationFactory> { }

[CollectionDefinition("TransferAgreementHistoryEntriesControllerTests")]
public class TransferAgreementHistoryEntriesControllerTestsCollection : ICollectionFixture<TransferAgreementsApiWebApplicationFactory> { }

[CollectionDefinition("TransferAgreementsControllerTests")]
public class TransferAgreementsControllerTestsCollection : ICollectionFixture<TransferAgreementsApiWebApplicationFactory> { }

[CollectionDefinition("TransferAgreementRepositoryTest")]
public class TransferAgreementRepositoryTestCollection : ICollectionFixture<TransferAgreementsApiWebApplicationFactory> { }

[CollectionDefinition("ExampledMigrationTests")]
public class ExampledMigrationTestsCollection : ICollectionFixture<TransferAgreementsApiWebApplicationFactory> { }

[CollectionDefinition("RollbackMigrationTests")]
public class RollbackMigrationTestsCollection : ICollectionFixture<TransferAgreementsApiWebApplicationFactory> { }

