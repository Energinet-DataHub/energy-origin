using FluentValidation;

namespace API.Query.API.ApiModels.Requests;

public class TransferCertificateValidator : AbstractValidator<TransferCertificate>
{
    public TransferCertificateValidator()
    {
        RuleFor(tc => tc.Source)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .NotEqual(tc => tc.Target);

        RuleFor(tc => tc.Target)
            .NotEmpty();
    }
}
