using System;
using System.ComponentModel.DataAnnotations;

namespace API.ApiModels.Requests;

public record CreateTransferAgreement(
    long StartDate,
    long EndDate,
    string ReceiverTin);

