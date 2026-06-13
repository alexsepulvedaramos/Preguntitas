using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.DTOs;
using VayaPreguntita.API.Tests.Testing;

namespace VayaPreguntita.API.Tests.Endpoints;

// IClassFixture ensures that your PostgreSQL container starts before the test
public class AuthIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly HttpClient _client;

    // xUnit automatically injects your DatabaseFixture here
    public AuthIntegrationTests(DatabaseFixture fixture)
    {
        // 1. Extract the dynamic connection string from your container
        var connectionString = fixture.ConnectionString;

        // 2. Start your in-memory API, forcing it to use that connection string
        var factory = new CustomWebAppFactory(connectionString);

        // 3. Create the tool that will act as your Postman
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsSuccess()
    {
        // Arrange: Create a registration request with valid data
        var request = new
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "12345678",
        };

        // Act: Send the registration request to your API
        var responseBody = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert 1: Verify the status code is 201 Created
        Assert.Equal(System.Net.HttpStatusCode.Created, responseBody.StatusCode);

        // Assert 2: Extract the response body as JSON
        var responseContent = await responseBody.Content.ReadFromJsonAsync<AuthResponseDto>();

        // Assert 3: Verify the response contains the expected data
        Assert.NotNull(responseContent);
        Assert.NotNull(responseContent.AccessToken);
        Assert.Equal(request.Username, responseContent.Username);
    }
}
