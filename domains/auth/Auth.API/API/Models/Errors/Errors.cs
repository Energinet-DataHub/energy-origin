namespace API.Errors;

public record AuthError(string ErrorCode, string ErrorDescription)
{
    public static AuthError UnknownErrorFromIdentityProvider => new("E0", "Unknown error from Identity Provider");
    public static AuthError UserInterrupted => new("E1", "User interrupted");
    public static AuthError UserFailedToVerifySSN => new("E3", "User failed to verify SSN");
    public static AuthError UserDeclinedTerms => new("E4", "User declined terms");
    public static AuthError InternalServerError => new("E500", "Internal Server Error");
    public static AuthError InternalServerErrorAtIdentityProvider => new("E501", "Internal Server Error at Identity Provider");
    public static AuthError UnrecognizedErrorFromIdentityProvider => new ("E502", "Internal Server Error at Identity Provider");
    public static AuthError PrivateUsersNotAllowedToLogin => new("E504", "Private users are not allowed to login");
    public static AuthError FailedToCommunicateWithIdentityProvider => new ("E505", "Failed to communicate with Identity Provider");
}
