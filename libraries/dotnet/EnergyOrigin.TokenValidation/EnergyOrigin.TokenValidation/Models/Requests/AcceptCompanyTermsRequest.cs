using EnergyOrigin.TokenValidation.Values;

namespace EnergyOrigin.TokenValidation.Models.Requests;
public record AcceptCompanyTermsRequest(CompanyTermsType TermsType, string AcceptedTermsFileName);
