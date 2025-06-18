using Xunit;
using Moq;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Configuration;
using Moq.Protected;
using MDE_Client.Domain.Models;
using MDE_Client.Application.Services;
using MDE_Client.Application.Interfaces;
using MDE_Client.Application;

public class DashboardServiceTests
{
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<IAuthenticationService> _authService;
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly AuthSession _authSession;

    public DashboardServiceTests()
    {
        _mockConfig = new Mock<IConfiguration>();
        _mockConfig.Setup(c => c["Api:BaseUrl"]).Returns("http://localhost/");
        _mockConfig.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        _mockConfig.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");

        _authService = new Mock<IAuthenticationService>();

        _mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        _httpClient = new HttpClient(_mockHandler.Object)
        {
            BaseAddress = new Uri("http://localhost/")
        };

        _authSession = new AuthSession(_authService.Object, _mockConfig.Object);
    }

    [Fact]
    public async Task GetDashboardPagesAsync_ReturnsObservableCollection()
    {
        var expectedPages = new ObservableCollection<DashboardPage>
        {
            new DashboardPage { PageID = 1, PageName = "Overview", PageURL = "/overview" }
        };

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri.ToString().Contains("api/dashboard/1")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(expectedPages)
            });

        var service = new DashboardService(_httpClient, _mockConfig.Object, _authSession);
        var result = await service.GetDashboardPagesAsync(1);

        Assert.Single(result);
        Assert.Equal("Overview", result[0].PageName);
    }

    [Fact]
    public async Task AddDashboardPageAsync_SendsPostWithCorrectPayload()
    {
        HttpRequestMessage capturedRequest = null;

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri.ToString().Contains("api/dashboard")),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Created));

        var service = new DashboardService(_httpClient, _mockConfig.Object, _authSession);
        await service.AddDashboardPageAsync(1, "Status", "/status");

        Assert.NotNull(capturedRequest);
        var content = await capturedRequest.Content.ReadAsStringAsync();
        Assert.Contains("Status", content);
        Assert.Contains("/status", content);
        Assert.Contains("1", content); // machineId
    }

    [Fact]
    public async Task DeleteDashboardPageAsync_SendsDeleteRequest()
    {
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Delete &&
                    req.RequestUri.ToString().Contains("api/dashboard/42")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NoContent));

        var service = new DashboardService(_httpClient, _mockConfig.Object, _authSession);
        await service.DeleteDashboardPageAsync(42);
    }

    [Fact]
    public async Task GetFirstDashboardPageUrlAsync_ReturnsUrl()
    {
        string expectedUrl = "/default-page";

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri.ToString().Contains("api/dashboard/default-url/99")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(expectedUrl)
            });

        var service = new DashboardService(_httpClient, _mockConfig.Object, _authSession);
        var result = await service.GetFirstDashboardPageUrlAsync(99);

        Assert.Equal(expectedUrl, result);
    }
}
