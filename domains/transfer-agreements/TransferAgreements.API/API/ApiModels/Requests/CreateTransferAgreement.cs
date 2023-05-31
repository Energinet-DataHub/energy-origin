using System;
using System.ComponentModel.DataAnnotations;

namespace API.ApiModels.Requests;

public class CreateTransferAgreement
{
    [Required] public DateTimeOffset StartDate { get; set; }

    [Required] public DateTimeOffset EndDate { get; set; }

    [Required] public string ReceiverTin { get; set; }
}
