namespace API.Values;

// NOTE: Each subsection claims a hundreds range similar to HTTP status codes, starting where HTTP status codes ends (599). Any whole hundred number is considered a catch-all.
public readonly struct ErrorCode
{
    public const string QueryString = "errorCode";
    public struct Authentication
    {
        public const string Failed = "600";
        public const string InvalidTokens = "601";
    }

    public struct AuthenticationUpstream
    {
        public const string Failed = "700";
        public const string BadResponse = "701";
        public const string DiscoveryUnavailable = "702";

        // NOTE: This subsection is divided into ranges of ten, one for each predefined error in https://www.rfc-editor.org/rfc/rfc6749#section-4.1.2.1
        // - access_denied

        public const string Aborted = "711";
        public const string NoContext = "712";

        // - server_error
        // - temporarily_unavailable
        public const string InternalError = "714";

        // - unauthorized_client
        public const string InvalidClient = "720";

        // - invalid_request
        // - unsupported_response_type
        public const string InvalidRequest = "730";

        // - invalid_scope
        public const string InvalidScope = "740";

        public static string From(string? error, string? errorDescription) => (error?.ToLowerInvariant() ?? "", errorDescription?.ToLowerInvariant() ?? "")
        switch
        {
            ("access_denied", "no_ctx") => NoContext,
            ("access_denied", "user_aborted") or ("access_denied", "private_to_business_user_aborted") => Aborted,
            ("access_denied", "internal_error") or ("server_error", _) or ("temporarily_unavailable", _) => InternalError,
            ("unsupported_response_type", _) or ("invalid_request", _) => InvalidRequest,
            ("unauthorized_client", _) => InvalidClient,
            ("invalid_scope", _) => InvalidScope,
            _ => Failed,
        };
    }
}
