namespace VayaPreguntita.API.Tests.Testing;

// Domain tests share one PostgreSQL container and run one at a time; each test wipes the
// database first (DomainTestBase), so they never see each other's rows.
[CollectionDefinition(Name)]
public class PostgresCollection : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "Postgres";
}
