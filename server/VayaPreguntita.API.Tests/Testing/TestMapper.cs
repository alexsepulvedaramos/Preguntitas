using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using VayaPreguntita.API.Profiles;

namespace VayaPreguntita.API.Tests.Testing;

// The app's real AutoMapper configuration, so result DTOs are built exactly as in production.
public static class TestMapper
{
    public static readonly IMapper Instance = new MapperConfiguration(
        cfg => cfg.AddProfile<MappingProfile>(),
        NullLoggerFactory.Instance
    ).CreateMapper();
}
