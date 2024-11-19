using System.Net;

namespace API.Transfer.Api.Exceptions;

public class TransferAgreementConflictException()
    : BusinessException("There is already a Transfer Agreement with this company tin within the selected date range", HttpStatusCode.Conflict);