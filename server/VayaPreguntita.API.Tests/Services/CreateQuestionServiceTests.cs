using FluentAssertions;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.Models;
using VayaPreguntita.API.Services;
using Microsoft.EntityFrameworkCore;

public class CreateQuestionServiceTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public CreateQuestionServiceTests(DatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task CreateSuperlativeQuestion_ShouldPersistMetadata()
    {
        // Arrange
        var service = new CreateQuestionService(_fixture.Context);
        var dto = new CreateQuestionDto
        {
            Type = QuestionType.Superlative,
            AllowNobody = true,
            BlacklistedUserIds = new List<int> { 2, 5 }
        };

        // Act
        var created = await service.CreateAsync(dto);

        // Assert
        var persisted = await _fixture.Context.Questions
            .AsNoTracking()
            .FirstAsync(q => q.Id == created.Id);

        persisted.Metadata.Should().NotBeNull();
        persisted.Metadata.AllowNobody.Should().BeTrue();
        persisted.Metadata.BlacklistedUserIds.Should().ContainInOrder(2, 5);
    }
}
