using System.Net;
using EnergyOrigin.Setup.Exceptions;

namespace API.Transfer.Api.Exceptions;

public class TransferAgreementExpiredException()
    : BusinessException("Transfer agreement has expired", HttpStatusCode.BadRequest);
