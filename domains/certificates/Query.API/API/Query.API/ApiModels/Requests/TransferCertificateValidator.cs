using FluentValidation;

namespace API.Query.API.ApiModels.Requests;

public class TransferCertificateValidator : AbstractValidator<TransferCertificate>
{
    public TransferCertificateValidator()
    {
        RuleFor(tc => tc.CurrentOwner)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .NotEqual(tc => tc.NewOwner);

        RuleFor(tc => tc.NewOwner)
            .NotEmpty();
    }
}
