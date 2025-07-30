using FluentValidation;

namespace API.Query.API.ApiModels.Requests.Internal;

public class EditContractEndDateValidator : AbstractValidator<EditContractEndDate>
{
    public EditContractEndDateValidator()
    {
        RuleFor(cs => cs.EndDate)
            .MustBeBeforeYear10000()
            .When(s => s.EndDate != default);
    }
}

public class EditContractEndDate20230101Validator : AbstractValidator<EditContractEndDate20230101>
{
    public EditContractEndDate20230101Validator()
    {
        RuleFor(cs => cs.EndDate)
            .MustBeBeforeYear10000()
            .When(s => s.EndDate != default);
    }
}
