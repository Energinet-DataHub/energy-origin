using System.Net;

namespace API.Transfer.Api.Exceptions;

public class SameSenderAndReceiverException()
    : BusinessException("ReceiverTin cannot be the same as SenderTin.", HttpStatusCode.BadRequest);
