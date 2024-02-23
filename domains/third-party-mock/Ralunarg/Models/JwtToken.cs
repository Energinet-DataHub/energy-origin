namespace Ralunarg.Models;

public record JwtToken(string access_token, int expires_in, int ext_expires_in, string token_type);
