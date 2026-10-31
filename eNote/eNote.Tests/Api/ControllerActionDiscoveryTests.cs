using System.Reflection;
using eNote.API.Controllers.Academic;
using eNote.API.Controllers.Communication;
using eNote.API.Controllers.Identity;
using eNote.API.Controllers.Rentals;
using eNote.API.Controllers.ReferenceData;
using eNote.API.Extensions;
using eNote.Application.Common.Files;
using eNote.Application.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace eNote.Tests.Api;

public sealed class ControllerActionDiscoveryTests
{
    private static readonly (string Name, string Suffix)[] CrudActions =
    [
        ("GetPaged", ""),
        ("GetById", "/{id:int}"),
        ("Create", ""),
        ("Update", "/{id:int}"),
        ("Delete", "/{id:int}")
    ];

    [Theory]
    [InlineData(typeof(AdminAddressController))]
    [InlineData(typeof(AdminCityController))]
    [InlineData(typeof(AdminEventController))]
    [InlineData(typeof(AdminInstrumentTypeController))]
    [InlineData(typeof(AdminMusicStoreController))]
    public void ReferenceDataCrudActions_AreRouted(Type controllerType)
    {
        var route = controllerType.GetCustomAttribute<RouteAttribute>()?.Template;
        Assert.NotNull(route);

        var actions = DiscoveredActions()
            .Where(a => a.ControllerTypeInfo.AsType() == controllerType)
            .ToList();

        foreach (var (name, suffix) in CrudActions)
        {
            var action = Assert.Single(actions, a => a.ActionName == name);
            Assert.Equal(route + suffix, action.AttributeRouteInfo?.Template);
        }
    }

    [Theory]
    [InlineData("POST", nameof(TuitionPaymentsController.CreateIntent), "api/v{version:apiVersion}/student/enrollments/{enrollmentId:int}/tuition/create-intent")]
    [InlineData("GET", nameof(TuitionPaymentsController.GetLatest), "api/v{version:apiVersion}/student/enrollments/{enrollmentId:int}/tuition")]
    [InlineData("GET", nameof(TuitionPaymentsController.GetHistory), "api/v{version:apiVersion}/student/enrollments/{enrollmentId:int}/tuition/history")]
    public void TuitionPaymentActions_HaveExpectedRoutesAndHttpMethods(string httpMethod, string actionName, string template)
    {
        var action = Assert.Single(DiscoveredActions(), a =>
            a.ControllerTypeInfo.AsType() == typeof(TuitionPaymentsController) && a.ActionName == actionName);

        Assert.Equal(template, action.AttributeRouteInfo?.Template);
        Assert.Equal(httpMethod, Assert.Single(action.ActionConstraints!.OfType<HttpMethodActionConstraint>().SelectMany(c => c.HttpMethods)));
    }

    [Fact]
    public void InheritedCrudActions_AreDeclaredOnTheGenericBase()
    {
        var declaringTypes = DiscoveredActions()
            .Where(a => a.ControllerTypeInfo.AsType() == typeof(AdminCityController))
            .Select(a => a.MethodInfo.DeclaringType?.Name)
            .Distinct()
            .ToList();

        Assert.StartsWith("ReferenceDataController", Assert.Single(declaringTypes));
    }

    [Fact]
    public void UploadActions_AreSizeCapped_AndRateLimited()
    {
        (Type Controller, string Action)[] uploadActions =
        [
            (typeof(InstrumentController), nameof(InstrumentController.UploadImage)),
            (typeof(AdminMusicStoreController), nameof(AdminMusicStoreController.UploadImage)),
            (typeof(ShopStoreController), nameof(ShopStoreController.UploadOwnStoreImage)),
            (typeof(AssignmentSubmissionController), nameof(AssignmentSubmissionController.Submit)),
            (typeof(UsersController), nameof(UsersController.UploadPicture)),
            (typeof(AnnouncementController), nameof(AnnouncementController.UploadImageForCourse)),
            (typeof(AnnouncementController), nameof(AnnouncementController.UploadImageForStore))
        ];

        foreach (var (controller, action) in uploadActions)
        {
            var method = controller.GetMethod(action);
            Assert.NotNull(method);

            var sizeLimit = Assert.Single(method!.GetCustomAttributes<RequestSizeLimitAttribute>());
            Assert.Equal(FileUploadLimits.MaxRequestBytes, ((IRequestSizeLimitMetadata)sizeLimit).MaxRequestBodySize);

            var throttle = Assert.Single(method.GetCustomAttributes<EnableRateLimitingAttribute>());
            Assert.Equal(RateLimitingExtensions.UploadsPolicy, throttle.PolicyName);
        }
    }

    [Fact]
    public void AnonymousActions_AreOnlyTheAllowListed()
    {
        string[] expected =
        [
            "AuthController.ForgotPassword",
            "AuthController.Login",
            "AuthController.Register",
            "AuthController.ResetPassword",
            "StripeWebhookController.Handle"
        ];

        var anonymous = DiscoveredActions()
            .Where(a => a.MethodInfo.IsDefined(typeof(AllowAnonymousAttribute), inherit: true)
                || a.ControllerTypeInfo.IsDefined(typeof(AllowAnonymousAttribute), inherit: true))
            .Select(a => $"{a.ControllerTypeInfo.Name}.{a.MethodInfo.Name}")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected, anonymous);
    }

    [Fact]
    public void RoleRoutePrefixes_RequireExactlyTheirRole()
    {
        var allRoles = $"{AppRoles.Administrator},{AppRoles.Instructor},{AppRoles.StoreEmployee},{AppRoles.Student}";
        var expectedRoles = new Dictionary<string, string>
        {
            ["admin"] = AppRoles.Administrator,
            ["instructor"] = AppRoles.Instructor,
            ["shop"] = AppRoles.StoreEmployee,
            ["student"] = AppRoles.Student
        };
        var sharedActions = new Dictionary<string, string>
        {
            ["StudentNotificationController"] = allRoles
        };
        const string prefix = "api/v{version:apiVersion}/";

        var violations = new List<string>();
        var checkedActions = 0;

        foreach (var action in DiscoveredActions())
        {
            var template = action.AttributeRouteInfo?.Template ?? "";
            var segment = template.StartsWith(prefix, StringComparison.Ordinal)
                ? template[prefix.Length..].Split('/')[0]
                : "";

            if (!expectedRoles.TryGetValue(segment, out var expected))
            {
                continue;
            }

            if (sharedActions.TryGetValue(action.ControllerTypeInfo.Name, out var shared))
            {
                expected = shared;
            }

            checkedActions++;
            var actual = EffectiveRoles(action);
            if (actual != expected)
            {
                violations.Add($"{template} ({action.ControllerTypeInfo.Name}.{action.MethodInfo.Name}): expected '{expected}', found '{actual}'");
            }
        }

        Assert.True(checkedActions > 100, "The route scan found too few role-prefixed actions.");
        Assert.Empty(violations);
    }

    [Fact]
    public void ActionsOutsideRolePrefixes_AreAuthenticatedOrAllowListed()
    {
        string[] expectedAnyAuthenticatedUser =
        [
            "AuthController.Logout",
            "InstrumentController.GetPublicById",
            "InstrumentController.GetPublicPaged",
            "UploadsController.GetAnnouncement",
            "UploadsController.GetAssignment",
            "UploadsController.GetCourse",
            "UploadsController.GetEvent",
            "UploadsController.GetInstrument",
            "UploadsController.GetMusicStore",
            "UsersController.ChangePassword",
            "UsersController.DeletePicture",
            "UsersController.GetCurrentUser",
            "UsersController.GetPicture",
            "UsersController.GetUserPicture",
            "UsersController.UpdateProfile",
            "UsersController.UploadPicture"
        ];
        string[] rolePrefixes = ["admin", "instructor", "shop", "student"];
        const string prefix = "api/v{version:apiVersion}/";

        var open = DiscoveredActions()
            .Where(a => !IsAnonymous(a))
            .Where(a =>
            {
                var template = a.AttributeRouteInfo?.Template ?? "";
                return !rolePrefixes.Contains(template[prefix.Length..].Split('/')[0]);
            })
            .Where(a => EffectiveRoles(a) == "")
            .Select(a => $"{a.ControllerTypeInfo.Name}.{a.MethodInfo.Name}")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedAnyAuthenticatedUser, open);
    }

    private static bool IsAnonymous(ControllerActionDescriptor action) =>
        action.MethodInfo.IsDefined(typeof(AllowAnonymousAttribute), inherit: true)
        || action.ControllerTypeInfo.IsDefined(typeof(AllowAnonymousAttribute), inherit: true);

    private static string EffectiveRoles(ControllerActionDescriptor action)
    {
        var attributes = action.MethodInfo.GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Concat(action.ControllerTypeInfo.GetCustomAttributes<AuthorizeAttribute>(inherit: true));

        return attributes.FirstOrDefault(a => !string.IsNullOrEmpty(a.Roles))?.Roles ?? "";
    }

    private static List<ControllerActionDescriptor> DiscoveredActions()
    {
        var parts = new ApplicationPartManager();
        parts.ApplicationParts.Add(new AssemblyPart(typeof(AdminCityController).Assembly));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(parts);
        services.AddSingleton<IHostApplicationLifetime>(new StubHostApplicationLifetime());
        services.AddControllers();

        using var provider = services.BuildServiceProvider();

        return provider.GetRequiredService<IActionDescriptorCollectionProvider>()
            .ActionDescriptors.Items
            .OfType<ControllerActionDescriptor>()
            .ToList();
    }

    private sealed class StubHostApplicationLifetime : IHostApplicationLifetime
    {
        public CancellationToken ApplicationStarted => CancellationToken.None;

        public CancellationToken ApplicationStopping => CancellationToken.None;

        public CancellationToken ApplicationStopped => CancellationToken.None;

        public void StopApplication() { }
    }
}
