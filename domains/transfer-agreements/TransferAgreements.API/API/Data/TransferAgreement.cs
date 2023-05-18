using System;

namespace API.Data;

public class TransferAgreement
{
    public Guid Id { get; set; }

    public DateTimeOffset StartDate { get; set; }

    public DateTimeOffset EndDate { get; set; }

    public int ReceiverTin { get; set; }
}
