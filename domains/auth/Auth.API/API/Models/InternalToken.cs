using FluentValidation;

namespace API.Models;
public record InternalToken
{
    public DateTime Issued { get; init; }
    public DateTime Expires { get; init; }
    public string Actor { get; init; }
    public string Subject { get; init; }
    public List<string> Scope { get; init; }
}

public class InternalTokenValidator : AbstractValidator<InternalToken>
{
    public InternalTokenValidator()
    {
        RuleFor(it => it.Issued).LessThan(DateTime.UtcNow);
        RuleFor(it => it.Expires).GreaterThan(DateTime.UtcNow);
    }
}
