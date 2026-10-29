using EnterpriseApp.Application.DTOs.Factorial;
using EnterpriseApp.Application.Interfaces.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FactorialController : ControllerBase
{
    private readonly IFactorialCalculatorService _calculatorService;
    private readonly IValidator<BatchCalculationRequest> _batchValidator;

    public FactorialController(
        IFactorialCalculatorService calculatorService,
        IValidator<BatchCalculationRequest> batchValidator)
    {
        _calculatorService = calculatorService;
        _batchValidator = batchValidator;
    }

    [HttpPost("batch")]
    [ProducesResponseType(typeof(BatchCalculationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CalculateBatch(
        [FromBody] BatchCalculationRequest request,
        CancellationToken ct)
    {
        var validationResult = await _batchValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return BadRequest(new ValidationProblemDetails(errors));
        }

        var result = await _calculatorService.CalculateBatchAsync(request.Numbers, request.MaxConcurrency, null, ct);
        return Ok(result);
    }

    [HttpPost("single/{number:int}")]
    [ProducesResponseType(typeof(FactorialTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CalculateSingle(int number, CancellationToken ct)
    {
        if (number < 0 || number > 1000)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Parameter",
                Detail = "Number must be between 0 and 1,000."
            });
        }

        var result = await _calculatorService.CalculateSingleAsync(number, ct);
        return Ok(result);
    }
}
