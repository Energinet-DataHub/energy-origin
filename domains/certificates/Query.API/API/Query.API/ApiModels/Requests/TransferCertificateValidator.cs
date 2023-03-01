using FluentValidation;

namespace API.Query.API.ApiModels.Requests;

public class TransferCertificateValidator : AbstractValidator<TransferCertificate>
{
    public TransferCertificateValidator()
    {
        RuleFor(co => co.CurrentOwner)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .NotEmpty()
            .NotEqual(no => no.NewOwner);
        RuleFor(no => no.NewOwner)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .NotNull()
            .NotEqual(co => co.CurrentOwner);

    }

}
