using System;
using System.ComponentModel.DataAnnotations;

namespace API.ApiModels.Requests;

public record CreateTransferAgreement(
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    string ReceiverTin);

