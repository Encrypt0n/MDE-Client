using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Moq;
using Moq.Protected;
using Xunit;
using MDE_Client.Application.Services;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

public class AuthenticationServiceTests
{
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly HttpClient _httpClient;
    private readonly Mock<HttpMessageHandler> _mockHandler;

    public AuthenticationServiceTests()
    {
        _mockConfig = new Mock<IConfiguration>();
        _mockConfig.Setup(c => c["Api:BaseUrl"]).Returns("http://localhost/");
        _mockConfig.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        _mockConfig.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");

        _mockHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHandler.Object)
        {
            BaseAddress = new Uri("http://localhost")
        };
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnTrue_WhenTokenIsValid()
    {
        var expectedToken = "fake-jwt-token";

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("api/auth/login")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(new TokenResponse { Token = expectedToken })
            });

        var authService = new AuthenticationService(_httpClient, _mockConfig.Object);
        var result = await authService.LoginAsync("user", "pass");

        Assert.True(result);
        Assert.Equal("Bearer", _httpClient.DefaultRequestHeaders.Authorization?.Scheme);
        Assert.Equal(expectedToken, _httpClient.DefaultRequestHeaders.Authorization?.Parameter);
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnTrue_WhenSuccessful()
    {
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("api/auth/register")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var authService = new AuthenticationService(_httpClient, _mockConfig.Object);
        var result = await authService.RegisterAsync("newuser", "password", 1);
        Assert.True(result);
    }

    [Fact]
    public async Task Logout_ShouldClearTokenAndAuthorizationHeader()
    {
        var expectedToken = "dummy-token";
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("api/auth/login")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(new TokenResponse { Token = expectedToken })
            });

        var authService = new AuthenticationService(_httpClient, _mockConfig.Object);
        await authService.LoginAsync("user", "pass");

        await authService.Logout();

        Assert.Null(_httpClient.DefaultRequestHeaders.Authorization);
    }


    [Fact]
    public void GetToken_ShouldReturnToken()
    {
        var authService = new AuthenticationService(_httpClient, _mockConfig.Object);
        typeof(AuthenticationService)
            .GetField("_token", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(authService, "test-token");

        var token = authService.GetToken();
        Assert.Equal("test-token", token);
    }

    private void WritePublicKeyToDisk(RSA rsa)
    {
        var publicKeyPem = PemEncoding.Write("PUBLIC KEY", rsa.ExportSubjectPublicKeyInfo());
        Directory.CreateDirectory("Keys");
        File.WriteAllText("Keys/public.key", new string(publicKeyPem));
    }


    [Fact]
    public async Task ValidateToken_WithValidToken_ReturnsClaimsPrincipal()
    {
        // Arrange
        var rsa = RSA.Create(2048);
        var rsaKey = new RsaSecurityKey(rsa);
        var creds = new SigningCredentials(rsaKey, SecurityAlgorithms.RsaSha256);

        var tokenHandler = new JwtSecurityTokenHandler();
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, "123"),
            new Claim("role", "2"),
            new Claim("companyId", "456")
        }),
            Expires = DateTime.UtcNow.AddMinutes(5),
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            SigningCredentials = creds
        };

        var token = tokenHandler.CreateToken(descriptor);
        var jwt = tokenHandler.WriteToken(token);

        // Write the public key to disk
        WritePublicKeyToDisk(rsa);

        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        mockConfig.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");

        var httpClient = new HttpClient(); // not used
        var authService = new AuthenticationService(_httpClient, _mockConfig.Object);

        // Act
        var result = await authService.ValidateToken(jwt);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Identity.IsAuthenticated);
     
    }


    [Fact]
    public void IsTokenValid_WithValidToken_ReturnsTrue()
    {
        // Arrange
        var rsa = RSA.Create(2048);
        var rsaKey = new RsaSecurityKey(rsa);
        var creds = new SigningCredentials(rsaKey, SecurityAlgorithms.RsaSha256);

        var tokenHandler = new JwtSecurityTokenHandler();
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, "123"),
            new Claim("role", "2"),
            new Claim("companyId", "456")
        }),
            Expires = DateTime.UtcNow.AddMinutes(5),
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            SigningCredentials = creds
        };

        var token = tokenHandler.CreateToken(descriptor);
        var jwt = tokenHandler.WriteToken(token);

        WritePublicKeyToDisk(rsa);

        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        mockConfig.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");

        var httpClient = new HttpClient(); // not used
        var authService = new AuthenticationService(_httpClient, _mockConfig.Object);

        // Use reflection to set _token private field
        var field = typeof(AuthenticationService).GetField("_token", BindingFlags.NonPublic | BindingFlags.Instance);
        field.SetValue(authService, jwt);

        // Act
        var isValid = authService.IsTokenValid();

        // Assert
        Assert.True(isValid);
    }

}
