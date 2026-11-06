using eNote.API.Filters;
using eNote.Application.Features.ReferenceData.Cities;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace eNote.Tests.Api;

public sealed class FluentValidationFilterTests
{
    private const string NameMessage = "Naziv grada je obavezan.";
    private const string BadRequestMessage = "Neispravan zahtjev.";

    [Fact]
    public void Filter_RunsBeforeTheModelStateResponse()
    {
        var options = new ApiBehaviorOptions { InvalidModelStateResponseFactory = _ => new OkResult() };
        var modelStateFilter = new ModelStateInvalidFilter(options, NullLogger.Instance);

        Assert.True(FluentValidationFilter.FilterOrder < modelStateFilter.Order);
    }

    [Fact]
    public async Task BindingError_WithNoArgument_BecomesOneBosnianMessage()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("$", "JSON deserialization for type 'eNote.Application.Features.Identity.Auth.RegisterRequest' was missing required properties.");
        modelState.AddModelError("model", "The model field is required.");

        await RunFilterAsync(modelState, new Dictionary<string, object?>());

        var entry = Assert.Single(modelState);
        Assert.Equal(string.Empty, entry.Key);
        Assert.Equal(BadRequestMessage, Assert.Single(entry.Value!.Errors).ErrorMessage);
    }

    [Fact]
    public async Task BindingError_WithAValidArgument_BecomesOneBosnianMessage()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Page", "The value 'abc' is not valid for Page.");

        await RunFilterAsync(modelState, new Dictionary<string, object?> { ["request"] = new CityRequest { Name = "Mostar" } });

        var entry = Assert.Single(modelState);
        Assert.Equal(BadRequestMessage, Assert.Single(entry.Value!.Errors).ErrorMessage);
    }

    [Fact]
    public async Task ImplicitRequiredError_GivesWayToTheValidatorMessage()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Name", "The Name field is required.");

        await RunFilterAsync(modelState, new Dictionary<string, object?> { ["request"] = new CityRequest { Name = "" } });

        var entry = Assert.Single(modelState);
        Assert.Equal("Name", entry.Key);
        Assert.Equal(NameMessage, Assert.Single(entry.Value!.Errors).ErrorMessage);
    }

    [Fact]
    public async Task ValidatorError_WithNoBindingError_StaysUnderItsField()
    {
        var modelState = new ModelStateDictionary();

        await RunFilterAsync(modelState, new Dictionary<string, object?> { ["request"] = new CityRequest { Name = "" } });

        var entry = Assert.Single(modelState);
        Assert.Equal("Name", entry.Key);
        Assert.Equal(NameMessage, Assert.Single(entry.Value!.Errors).ErrorMessage);
    }

    [Fact]
    public async Task ValidRequest_LeavesTheModelStateValid()
    {
        var modelState = new ModelStateDictionary();

        await RunFilterAsync(modelState, new Dictionary<string, object?> { ["request"] = new CityRequest { Name = "Mostar" } });

        Assert.True(modelState.IsValid);
    }

    [Fact]
    public async Task FormBindingError_StaysForTheFileMessage()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError(string.Empty, "Failed to read the request form.");

        await RunFilterAsync(modelState, new Dictionary<string, object?>(), "multipart/form-data; boundary=x");

        var entry = Assert.Single(modelState);
        Assert.Equal("Failed to read the request form.", Assert.Single(entry.Value!.Errors).ErrorMessage);
    }

    private static async Task RunFilterAsync(ModelStateDictionary modelState, IDictionary<string, object?> arguments, string contentType = "application/json")
    {
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<CityRequest>, StubCityValidator>();
        await using var provider = services.BuildServiceProvider();
        var httpContext = new DefaultHttpContext { RequestServices = provider };
        httpContext.Request.ContentType = contentType;
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), modelState);
        var executing = new ActionExecutingContext(actionContext, [], arguments, new object());

        await new FluentValidationFilter().OnActionExecutionAsync(executing, () =>
            Task.FromResult(new ActionExecutedContext(actionContext, [], new object())));
    }

    private sealed class StubCityValidator : AbstractValidator<CityRequest>
    {
        public StubCityValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(NameMessage);
        }
    }
}
