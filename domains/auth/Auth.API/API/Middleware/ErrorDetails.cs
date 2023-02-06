using System.Text.Json;

namespace API.Middleware
{
#pragma warning disable CS8618
    public class ErrorDetails
    {
        public int StatusCode { get; init; }
        public string Message { get; init; }

        public override string ToString() => JsonSerializer.Serialize(this);
    }
}
