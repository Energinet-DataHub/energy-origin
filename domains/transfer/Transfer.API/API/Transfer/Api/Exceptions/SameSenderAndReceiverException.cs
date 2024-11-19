using System.Net;
using EnergyOrigin.Domain.Exceptions;

namespace API.Transfer.Api.Exceptions;

public class SameSenderAndReceiverException()
    : BusinessException("ReceiverTin cannot be the same as SenderTin.", HttpStatusCode.BadRequest);
