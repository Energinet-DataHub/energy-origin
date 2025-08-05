using FluentValidation;

namespace API.Query.API.ApiModels.Requests;

public class EditContractEndDateValidator : AbstractValidator<EditContractEndDate>
{
    public EditContractEndDateValidator()
    {
        RuleFor(cs => cs.EndDate)
            .MustBeBeforeYear10000()
            .When(s => s.EndDate != default);
    }
}
