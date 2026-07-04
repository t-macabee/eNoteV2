using eNote.Application.Features.Shared;
using Mapster;
using MapsterMapper;

namespace eNote.Tests.TestUtils;

public static class TestMapper
{
    private static readonly TypeAdapterConfig Config = BuildConfig();

    public static Mapper Create() => new(Config);

    private static TypeAdapterConfig BuildConfig()
    {
        var config = new TypeAdapterConfig();
        config.Scan(typeof(InstrumentMappingConfig).Assembly);
        config.Compile();
        return config;
    }
}
