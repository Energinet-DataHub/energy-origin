namespace API.Errors;

public record AuthError(string ErrorCode, string ErrorDescription)
{
    public static AuthError UnknownErrorFromIdentityProvider => new("E0", "Unknown error from Identity Provider");
    public static AuthError PrivateUsersNotAllowedToLogin => new("E504", "Private users are not allowed to login");
    public static AuthError UserInterrupted => new("E1", "User interrupted");
    public static AuthError FailedToCommunicateWithIdentityProvider => new("E505", "Failed to communicate with Identity Provider");
}
