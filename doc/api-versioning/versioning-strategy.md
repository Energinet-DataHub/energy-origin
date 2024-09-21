## API Versioning Strategy

### Overview
In our subsystems, we implement a strategic approach to API versioning. This is particularly crucial for managing breaking changes to the API contract, ensuring backward compatibility, and facilitating smooth transitions between API versions.

### Versioning Methodology
Our versioning process is designed to handle breaking changes effectively.
Say you want to create a new api version. The following steps are taken:

1. **Duplication of the endpoint with the breaking change:**

    - We start by duplicating the endpoint, with the breaking change and its associated data transfer objects (DTOs), if necessary.
    - Necessary modifications are then applied to these duplicates, to incorporate the required changes.
    - The new endpoint is then versioned by appending the new version number to the `ApiVersion` annotation. An example is shown below:
      ```csharp
      [ApiVersion("2")] // <--- Append new version number to the annotation
      [Route("api/claim-automation")]
      ```
    - Since a new API version has been created, the other endpoints, that are not affected by the breaking change, need to be have their versions updated. This is done in the controller class's `ApiVersion` annotation. An example is shown below:
      ```csharp
      [Authorize]
      [ApiController]
      [ApiVersion("2")] // <--- Update version number to the annotation
      [Route("api/claim-automation")]
      ```

2. **Deprecation of Old Controllers:**
   If the old endpoint is to be deprecated, the following steps are taken:
    - Previous versions of the controllers are marked as deprecated by setting the `Deprecated = true` flag in the `ApiVersion` annotation. An example is shown below:
      ```csharp
      [Authorize]
      [ApiController]
      [ApiVersion("1", Deprecated = true)] // <--- Append flag to the annotation
      [Route("api/claim-automation")]
      ```
3. **Example of Versioned Endpoints:**

```csharp
    [Authorize]
    [ApiController]
    [ApiVersion("1", Deprecated = true)] // <--- Old Version remains the same, but is marked as deprecated.
    [ApiVersion("2")] // <--- New version added, to indicate controller has endppoints for this version as well.
    [Route("api/claim-automation")]
    public class ClaimAutomationController : ControllerBase
    {

    // Old GET endpoint, available only in the deprecated version.
    [ApiVersion("1")] // <--- Explicitly state that the old GET endpoint, is to only appear in the old version
    [HttpGet]
    public async Task<> GetClaimAutomationsOldVersion()
    {
        // Implementation for the old version...
    }

    // New GET endpoint, available only in the new version.
    [ApiVersion("2")] // <--- Explicitly state that the new GET endpoint, is to only appear in the new version
    [HttpGet]
    public async Task<ActionResult> GetClaimAutomationsNewVersion()
    {
        // Implementation for the new version...
    }

    // POST endpoint, available in both versions as it has no breaking changes.
    [HttpPost] // <--- Notice the POST endpoint is not explicitly versioned which means it is available in both versions
    public async Task<ActionResult<ClaimAutomationDto>> PostClaimAutomation() // <--- This endpoint is not duplicated
    {
        // Implementation common to both versions...
    }
}
   ```

### Testing Strategy for New API Versions
Our approach to testing new API versions involves:

- Creating new test Class for the tests of the new API versioned endpoints.
- Writing tests within that class specifically for the new API version, containing only the relevant tests.

This selective testing strategy aligns with our API versioning approach, maintaining efficiency and focus in our testing framework.
