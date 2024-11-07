using System;
using DataContext.Models;
using FluentValidation;
using System.Text.Json.Serialization;

namespace API.Transfer.Api.Dto.Requests;

public record CreateTransferAgreementProposal(
    long StartDate,
    long? EndDate,
    string? ReceiverTin,
    CreateTransferAgreementType? Type = null);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CreateTransferAgreementType
{
    TransferAllCertificates = 0,
    TransferCertificatesBasedOnConsumption = 1,
}

public static class CreateTransferAgreementTypeMapper
{
    public static TransferAgreementType MapCreateTransferAgreementType(CreateTransferAgreementType? request)
    {
        return request switch
        {
            null => TransferAgreementType.TransferAllCertificates,
            CreateTransferAgreementType.TransferAllCertificates => TransferAgreementType.TransferAllCertificates,
            CreateTransferAgreementType.TransferCertificatesBasedOnConsumption => TransferAgreementType.TransferCertificatesBasedOnConsumption,
            _ => throw new ArgumentOutOfRangeException($"Unable to map transfer agreement type value {request}")
        };
    }
}

public class CreateTransferAgreementProposalValidator : AbstractValidator<CreateTransferAgreementProposal>
{
    public CreateTransferAgreementProposalValidator()
    {
        var now = DateTimeOffset.UtcNow;

        RuleFor(createProposal => createProposal.StartDate)
            .NotEmpty()
            .WithMessage("Start Date cannot be empty.")
            .GreaterThanOrEqualTo(_ => now.ToUnixTimeSeconds())
            .WithMessage("Start Date cannot be in the past.")
            .MustBeBeforeYear10000();

        RuleFor(createProposal => createProposal.EndDate)
            .Cascade(CascadeMode.Stop)
            .Must((createProposal, endDate) => endDate == null || endDate > createProposal.StartDate)
            .WithMessage("End Date must be null or later than Start Date.")
            .MustBeBeforeYear10000()
            .When(t => t.EndDate != null);

        RuleFor(createProposal => createProposal.ReceiverTin)
            .Length(8)
            .Matches("^[0-9]{8}$")
            .WithMessage("ReceiverTin must be 8 digits without any spaces.");
    }
}
