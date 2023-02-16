namespace API.Values;

public readonly struct ErrorCode
{
    public struct Authentication
    {
        public const string Failed = "600";
        public const string InvalidTokens = "601";
    }

    public struct AuthenticationUpstream
    {
        public const string Failed = "700";
        public const string DiscoveryUnavailable = "701";
        public const string Aborted = "702";
        public const string NoContext = "703";
        public const string InternalError = "704";
        public const string InvalidClient = "710";
        public const string InvalidRequest = "720";
        public const string InvalidScope = "730";
    }
}
