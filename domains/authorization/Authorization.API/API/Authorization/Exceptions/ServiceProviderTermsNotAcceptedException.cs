using EnergyOrigin.Setup.Exceptions;

namespace API.Authorization.Exceptions;

public class ServiceProviderTermsNotAcceptedException() : ForbiddenException("Organization has not accepted the latest service provider terms.");
