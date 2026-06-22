using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using VayaPreguntita.API.DTOs.Auth;
using VayaPreguntita.API.DTOs.Questions;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Tests.Testing;

namespace VayaPreguntita.API.Tests.Endpoints;

public class AuthIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly HttpClient _client;
    private readonly DatabaseFixture _fixture;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _passwordHasher = new PasswordHasher<User>();
        var connectionString = _fixture.ConnectionString;
        var factory = new CustomWebAppFactory(connectionString);
        _client = factory.CreateClient();
    }

    private void ClearUsers()
    {
        _fixture.Context.Users.RemoveRange(_fixture.Context.Users);
        _fixture.Context.SaveChanges();
    }

    private User SeedUser(
        string username = "seeduser",
        string email = "seed@example.com",
        string password = "12345678"
    )
    {
        ClearUsers();
        var user = new User { Username = username, Email = email };
        user.PasswordHash = _passwordHasher.HashPassword(user, password);
        _fixture.Context.Users.Add(user);
        _fixture.Context.SaveChanges();
        return user;
    }

    // Phase 1: Register Tests

    [Fact]
    public async Task Register_WithValidData_ReturnsCreated()
    {
        // Arrange
        ClearUsers();
        var request = new
        {
            Username = "newuser",
            Email = "new@example.com",
            Password = "12345678",
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(content);
        Assert.Equal(request.Username, content.Username);
        Assert.Equal(request.Email, content.Email);
        Assert.NotNull(content.AccessToken);
        Assert.NotNull(content.RefreshToken);
    }

    [Fact]
    public async Task Register_WithMissingUsername_ReturnsBadRequest()
    {
        // Arrange
        ClearUsers();
        var request = new
        {
            Username = "",
            Email = "test@example.com",
            Password = "12345678",
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithMissingEmail_ReturnsBadRequest()
    {
        // Arrange
        ClearUsers();
        var request = new
        {
            Username = "testuser",
            Email = "",
            Password = "12345678",
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithMissingPassword_ReturnsBadRequest()
    {
        // Arrange
        ClearUsers();
        var request = new
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "",
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithShortPassword_ReturnsBadRequest()
    {
        // Arrange
        ClearUsers();
        var request = new
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "1234567",
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithDuplicateUsername_ReturnsConflict()
    {
        // Arrange
        SeedUser("existinguser", "first@example.com");
        var request = new
        {
            Username = "existinguser",
            Email = "different@example.com",
            Password = "12345678",
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        // Arrange
        SeedUser("firstuser", "existing@example.com");
        var request = new
        {
            Username = "differentuser",
            Email = "existing@example.com",
            Password = "12345678",
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Conflict, response.StatusCode);
    }

    // Phase 1: Login Tests

    [Fact]
    public async Task Login_WithValidUsername_ReturnsOk()
    {
        // Arrange
        var user = SeedUser("loginuser", "login@example.com", "password123");
        var request = new LoginRequestDto { Identifier = "loginuser", Password = "password123" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(content);
        Assert.Equal(user.Username, content.Username);
        Assert.NotNull(content.AccessToken);
        Assert.NotNull(content.RefreshToken);
    }

    [Fact]
    public async Task Login_WithValidEmail_ReturnsOk()
    {
        // Arrange
        var user = SeedUser("emailuser", "email@example.com", "password123");
        var request = new LoginRequestDto
        {
            Identifier = "email@example.com",
            Password = "password123",
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(content);
        Assert.Equal(user.Username, content.Username);
    }

    [Fact]
    public async Task Login_WithInvalidUsernameAndEmail_ReturnsUnauthorized()
    {
        // Arrange
        SeedUser("someuser", "some@example.com", "password123");
        var request = new LoginRequestDto { Identifier = "nonexistent", Password = "password123" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        SeedUser("passuser", "pass@example.com", "correctpassword");
        var request = new LoginRequestDto { Identifier = "passuser", Password = "wrongpassword" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithMissingIdentifier_ReturnsBadRequest()
    {
        // Arrange
        SeedUser("someuser", "some@example.com");
        var request = new LoginRequestDto { Identifier = "", Password = "password123" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithMissingPassword_ReturnsBadRequest()
    {
        // Arrange
        SeedUser("someuser", "some@example.com");
        var request = new LoginRequestDto { Identifier = "someuser", Password = "" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Phase 1: Refresh Tests

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsOk()
    {
        // Arrange
        var user = SeedUser("refreshuser", "refresh@example.com");

        // Login to get a valid refresh token
        var loginRequest = new LoginRequestDto
        {
            Identifier = "refreshuser",
            Password = "12345678",
        };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginContent = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

        var request = new RefreshTokenRequestDto { RefreshToken = loginContent.RefreshToken };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(content);
        Assert.NotNull(content.AccessToken);
        Assert.NotNull(content.RefreshToken);
        Assert.NotEqual(loginContent.RefreshToken, content.RefreshToken);
    }

    [Fact]
    public async Task Refresh_WithExpiredToken_ReturnsUnauthorized()
    {
        // Arrange
        ClearUsers();
        var expiredToken = "invalid.expired.token";
        var request = new RefreshTokenRequestDto { RefreshToken = expiredToken };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        ClearUsers();
        var request = new RefreshTokenRequestDto { RefreshToken = "completely.invalid.token" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithEmptyToken_ReturnsBadRequest()
    {
        // Arrange
        ClearUsers();
        var request = new RefreshTokenRequestDto { RefreshToken = "" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Phase 1: Logout Tests

    [Fact]
    public async Task Logout_WithValidToken_ReturnsNoContent()
    {
        // Arrange
        var user = SeedUser("logoutuser", "logout@example.com");

        // Login to get a refresh token
        var loginRequest = new LoginRequestDto { Identifier = "logoutuser", Password = "12345678" };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginContent = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

        var request = new RefreshTokenRequestDto { RefreshToken = loginContent.RefreshToken };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/logout", request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithInvalidToken_ReturnsNoContent()
    {
        // Arrange
        ClearUsers();
        var request = new RefreshTokenRequestDto { RefreshToken = "invalid.token" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/logout", request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithEmptyToken_ReturnsBadRequest()
    {
        // Arrange
        ClearUsers();
        var request = new RefreshTokenRequestDto { RefreshToken = "" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/logout", request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }
}
