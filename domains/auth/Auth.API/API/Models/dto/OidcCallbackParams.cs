using FluentValidation;

namespace API.Controllers.dto;

#nullable disable
public record OidcCallbackParams
{
    public string State { get; init; }
    public string Iss { get; init; }
    public string Code { get; init; }
    public string Scope { get; init; }
    public string Error { get; init; }
    public string ErrorHint { get; init; }
    public string ErrorDescription { get; init; }
}
public class InvalidateAuthStateValidator : AbstractValidator<OidcCallbackParams>
{
    public InvalidateAuthStateValidator()
    {
        RuleFor(x => x.Error).NotEmpty();
        RuleFor(x => x.ErrorDescription).NotEmpty();
    }
}

