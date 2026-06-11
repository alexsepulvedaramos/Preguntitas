using FluentAssertions;
using FluentValidation.TestHelper;
using VayaPreguntita.API.Validators;
using VayaPreguntita.API.Models;

public class CreateQuestionDtoValidatorTests
{
    private readonly CreateQuestionDtoValidator _validator = new();

    [Fact]
    public void Superlative_WithValidData_ShouldBeValid()
    {
        var dto = new CreateQuestionDto
        {
            Type = QuestionType.Superlative,
            AllowNobody = true,
            BlacklistedUserIds = new List<int> { 3, 7 }
        };

        var result = _validator.TestValidate(dto);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Superlative_WithNegativeUserId_ShouldBeInvalid()
    {
        var dto = new CreateQuestionDto
        {
            Type = QuestionType.Superlative,
            AllowNobody = false,
            BlacklistedUserIds = new List<int> { -1 }
        };

        var result = _validator.TestValidate(dto);
        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.BlacklistedUserIds);
    }
}
