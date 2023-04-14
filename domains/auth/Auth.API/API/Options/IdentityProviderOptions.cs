using EnergyOrigin.TokenValidation.Values;
using Microsoft.IdentityModel.Tokens;

namespace API.Options;

public class IdentityProviderOptions
{
    public const string Prefix = "IdentityProvider";

    public List<ProviderType> Providers { get; init; } = null!;

    public (string, List<KeyValuePair<string, string>>) GetIdentityProviderArguments()
    {
        if (Providers.Any() == false)
        {
            throw new ArgumentException("No identity providers were found.");
        }

        var scope = "openid ssn userinfo_token";
        var idp_values = string.Empty;
        var idp_params = string.Empty;

        if (Providers.Contains(ProviderType.NemID_Private) || Providers.Contains(ProviderType.NemID_Professional))
        {
            scope = string.Join(" ", scope, "nemid");
            idp_values = "nemid";

            if (Providers.Contains(ProviderType.NemID_Private) && Providers.Contains(ProviderType.NemID_Professional))
            {
                scope = string.Join(" ", scope, "private_to_business");
                idp_params =
                    """
                    "nemid": {"amr_values": "nemid.otp nemid.keyfile"}
                    """;
            }
            else
            {
                idp_params = Providers.Contains(ProviderType.NemID_Private)
                    ? """
                    "nemid": {"amr_values": "nemid.otp"}
                    """
                    : """
                    "nemid": {"amr_values": "nemid.keyfile"}
                    """;
            }
        }

        if (Providers.Contains(ProviderType.MitID_Private) || Providers.Contains(ProviderType.MitID_Professional))
        {
            if (Providers.Contains(ProviderType.MitID_Private))
            {
                // TODO: We get the following error which isn't even available in the documentation: "access_denied: user_navigation_error"
                //       nemid.pid needs to be added to the scope to be able to map nemid_private and mitid_private users,
                //       but there seems to be a bug from SignaturGruppen when adding nemid.pid to the scope when trying to login as mitid_erhverv.
                scope = string.Join(" ", scope, "mitid");
                idp_values = string.Join(idp_values.IsNullOrEmpty() ? null : " ", idp_values, "mitid");
            }

            if (Providers.Contains(ProviderType.MitID_Professional))
            {
                scope = string.Join(" ", scope, "nemlogin");
                idp_values = string.Join(idp_values.IsNullOrEmpty() ? null : " ", idp_values, "mitid_erhverv");
            }
        }

        idp_params = idp_params.IsNullOrEmpty() ? idp_params : $$"""{{{idp_params}}}""";

        var arguments = new List<KeyValuePair<string, string>>();

        if (idp_values.IsNullOrEmpty() == false)
        {
            arguments.Add(new KeyValuePair<string, string>("idp_values", idp_values));
        }

        if (idp_params.IsNullOrEmpty() == false)
        {
            arguments.Add(new KeyValuePair<string, string>("idp_params", idp_params));
        }

        return (scope, arguments);
    }
}
