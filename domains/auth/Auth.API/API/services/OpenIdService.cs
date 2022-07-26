using API.Helpers;

namespace API.Services;


public class OpenIdService
{
    public string CreateAuthUrl(string state, string callbackUrl, string language = "en")
    {
        var amrValues = new Dictionary<string, string>()
        {
            { "amr_values", "nemid.otp nemid.keyfile" }
        };
        var nemId = new Dictionary<string, Dictionary<string, string>()
        {
            { "nemid", amrValues}
        };




        return "lol";
    }


}
