using System.ComponentModel.DataAnnotations;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.IdentityModel.Tokens;

namespace API.Options;

public class IdentityProviderOptions
{
    public const string Prefix = "IdentityProvider";

    [Required] public List<ProviderType> Providers { get; init; } = null!;

    public (string, List<KeyValuePair<string, string>>) GetIdentityProviderArguments()
    {
        if (Providers.Any() == false) throw new ArgumentException("No identity providers were found.");

        var scope = "openid ssn userinfo_token";
        var idpValues = string.Empty;
        var idpParams = string.Empty;

        if (Providers.Contains(ProviderType.NemID_Private) || Providers.Contains(ProviderType.NemID_Professional))
        {
            scope = string.Join(" ", scope, "nemid");
            idpValues = "nemid";
            idpParams =
                """
                "nemid": {"amr_values": "nemid.otp nemid.keyfile"}
                """;

            if (Providers.Contains(ProviderType.NemID_Private) && Providers.Contains(ProviderType.NemID_Professional))
                scope = string.Join(" ", scope, "private_to_business");
        }

        if (Providers.Contains(ProviderType.MitID_Private) || Providers.Contains(ProviderType.MitID_Professional))
        {
            if (Providers.Contains(ProviderType.MitID_Private))
            {
                // TODO: We get the following error which isn't even available in the documentation: "access_denied: user_navigation_error"
                //       nemid.pid needs to be added to the scope to be able to map nemid_private and mitid_private users,
                //       but there seems to be a bug from SignaturGruppen when adding nemid.pid to the scope when trying to login as mitid_erhverv.
                scope = string.Join(" ", scope, "mitid");
                idpValues = string.Join(idpValues.IsNullOrEmpty() ? null : " ", idpValues, "mitid");
            }

            if (Providers.Contains(ProviderType.MitID_Professional))
            {
                scope = string.Join(" ", scope, "nemlogin");
                idpValues = string.Join(idpValues.IsNullOrEmpty() ? null : " ", idpValues, "mitid_erhverv");
            }
        }

        idpParams = idpParams.IsNullOrEmpty() ? idpParams : $$"""{{{idpParams}}}""";

        var arguments = new List<KeyValuePair<string, string>>();

        if (idpValues.IsNullOrEmpty() == false)
            arguments.Add(new KeyValuePair<string, string>("idp_values", idpValues));

        if (idpParams.IsNullOrEmpty() == false)
            arguments.Add(new KeyValuePair<string, string>("idp_params", idpParams));

        return (scope, arguments);
    }
}
