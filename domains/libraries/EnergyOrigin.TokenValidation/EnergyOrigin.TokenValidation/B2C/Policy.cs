namespace EnergyOrigin.TokenValidation.b2c;

public static class Policy
{
    public const string FrontendOr3rdParty = "ett-3rdparty";
    public const string B2CInternal = "ett-authorization-b2c";
    public const string Frontend = "ett-frontend"; // Would frontend not always have CVR?
    public const string ETT_Frontend_TIN = "ett-frontend-tin";
}
