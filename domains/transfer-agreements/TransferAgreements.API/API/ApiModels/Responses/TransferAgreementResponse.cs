using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace API.ApiModels.Responses;

public class TransferAgreementResponse
{

    public Guid Id { get; set; }

    public DateTimeOffset StartDate { get; set; }

    public DateTimeOffset EndDate { get; set; }

    public string ActorId { get; set; }

    public Guid SenderId { get; set; }

    public int ReceiverTin { get; set; }
}
