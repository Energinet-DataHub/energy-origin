using EnergyOrigin.Setup.Exceptions;

namespace API.Authorization.Exceptions;

public class UnableToGrantConsentToOwnOrganizationException() : BusinessException("Unable to grant consent to users own organization");
