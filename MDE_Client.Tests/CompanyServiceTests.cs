using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using Xunit;
using MDE_Client.Application.Services;
using MDE_Client.Domain.Models;
using MDE_Client.Application.Interfaces;
using System.Text.Json;
using MDE_Client.Application;
using System.Net.Http.Headers;

public class CompanyServiceTests
{
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly Mock<IAuthenticationService> _authService;
    private readonly HttpClient _httpClient;
    private readonly AuthSession _authSession;
    private readonly Mock<IConfiguration> _config;

    public CompanyServiceTests()
    {
        // Configuration mock
        _mockConfig = new Mock<IConfiguration>();
        _mockConfig.Setup(c => c["Api:BaseUrl"]).Returns("http://localhost/");
        _mockConfig.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        _mockConfig.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");

        // HTTP mock
        _mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        _httpClient = new HttpClient(_mockHandler.Object)
        {
            BaseAddress = new Uri("http://localhost/")
        };

        // Instantiate AuthenticationService (can be real or mock)
        _authService = new Mock<IAuthenticationService>();

        // Set up AuthSession with a dummy token for testing
        _authSession = new AuthSession(_authService.Object, _mockConfig.Object);
        
    }

    [Fact]
    public async Task GetAllCompaniesAsync_ReturnsListOfCompanies()
    {
        var expectedCompanies = new List<Company>
        {
            new Company { CompanyID = 1, Name = "TestCo", Description = "Desc" }
        };

        HttpRequestMessage capturedRequest = null;

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri.ToString().Contains("api/companies")),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(expectedCompanies)
            });

        // Manually attach mocked token
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "mocked-token");

        var service = new CompanyService(_httpClient, _mockConfig.Object, _authSession);
        var result = await service.GetAllCompaniesAsync();

        Assert.Single(result);
        Assert.Equal("TestCo", result[0].Name);
        // ✅ Assert Authorization header was sent correctly
        Assert.NotNull(capturedRequest.Headers.Authorization);
        Assert.Equal("Bearer", capturedRequest.Headers.Authorization.Scheme);
        Assert.Equal("mocked-token", capturedRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task GetCompanyByIdAsync_ReturnsCorrectCompany()
    {
        var expectedCompany = new Company { CompanyID = 5, Name = "TargetCo", Description = "TargetDesc" };

        HttpRequestMessage capturedRequest = null;

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri.ToString().Contains("api/companies/5")),
                ItExpr.IsAny<CancellationToken>())
             .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(expectedCompany)
            });

        // Manually attach mocked token
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "mocked-token");

        var service = new CompanyService(_httpClient, _mockConfig.Object, _authSession);
        var result = await service.GetCompanyByIdAsync(5);

        Assert.NotNull(result);
        Assert.Equal("TargetCo", result.Name);

        // ✅ Assert Authorization header was sent correctly
        Assert.NotNull(capturedRequest.Headers.Authorization);
        Assert.Equal("Bearer", capturedRequest.Headers.Authorization.Scheme);
        Assert.Equal("mocked-token", capturedRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task AddCompanyAsync_SendsPostRequestWithCorrectPayload()
    {
        HttpRequestMessage capturedRequest = null;

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri.ToString().Contains("api/companies")),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Created));

        var service = new CompanyService(_httpClient, _mockConfig.Object, _authSession);
        await service.AddCompanyAsync("NewCo", "NewDesc");

        Assert.NotNull(capturedRequest);
        var contentString = await capturedRequest.Content.ReadAsStringAsync();
        Assert.Contains("NewCo", contentString);
        Assert.Contains("NewDesc", contentString);
    }

}
