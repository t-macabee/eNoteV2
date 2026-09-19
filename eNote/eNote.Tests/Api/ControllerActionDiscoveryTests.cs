using System.Reflection;
using eNote.API.Controllers.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace eNote.Tests.Api;

/// The Admin reference-data controllers inherit their CRUD actions from
/// ReferenceDataController<TEntity, TDto, TRequest, TSearch>. These tests run
/// MVC's own action discovery over the API assembly, so an inherited action
/// that stops being routable fails here instead of only in a manual smoke test.
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
