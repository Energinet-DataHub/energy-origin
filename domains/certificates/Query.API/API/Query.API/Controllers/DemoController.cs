using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;

namespace API.Query.API.Controllers;

[ApiController]
public class DemoController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(void), 200)]
    [ProducesResponseType(400)]
    [Route("api/demo")]
    public async Task<IActionResult> Get([FromBody] DemoBody body, [FromServices] IValidator<DemoBody> validator)
    {
        var validationResult = await validator.ValidateAsync(body);
        if (!validationResult.IsValid)
        {
            validationResult.AddToModelState(ModelState, null);
            return ValidationProblem(ModelState);
        }

        return body.Return switch
        {
            1 => BadRequest("Something bad"),
            2 => Problem("Something bad", statusCode: 400/*, title: "This is fun"*/),
            3 => ValidationProblem("Something bad", instance: "bla"),
            _ => Ok()
        };
    }
}

public class DemoBody
{
    public string Name { get; set; } = "";
    public int Return { get; set; } = 1;
}

public class DemoBodyValidator : AbstractValidator<DemoBody>
{
    public DemoBodyValidator()
    {
        RuleFor(b => b.Name).MinimumLength(5);
    }
}
