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
using MDE_Client.Application.Services;
using MDE_Client.Application.Interfaces;
using MDE_Client.Domain.Models;
using MDE_Client.Application;

public class UserActivityServiceTests
{
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<IAuthenticationService> _authService;
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly AuthSession _authSession;

    public UserActivityServiceTests()
    {
        _mockConfig = new Mock<IConfiguration>();
        _mockConfig.Setup(c => c["Api:BaseUrl"]).Returns("http://localhost/");

        _authService = new Mock<IAuthenticationService>();

        _mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        _httpClient = new HttpClient(_mockHandler.Object)
        {
            BaseAddress = new Uri("http://localhost/")
        };

        _authSession = new AuthSession(_authService.Object, _mockConfig.Object);
    }

    [Fact]
    public async Task LogActivityAsync_SendsPostRequestWithCorrectPayload()
    {
        var log = new UserActivityLog
        {
            UserId = 1,
            MachineId = 2,
            Action = "Started VPN",
            IpAddress = "127.0.0.1",
            UserAgent = "MDE-Client",
            Timestamp = DateTime.UtcNow
        };

        HttpRequestMessage capturedRequest = null;

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri.ToString().Contains("api/activity")),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Created));

        var service = new UserActivityService(_httpClient, _mockConfig.Object, _authSession);
        await service.LogActivityAsync(log);

        Assert.NotNull(capturedRequest);
        var json = await capturedRequest.Content.ReadAsStringAsync();
        Assert.Contains("Started VPN", json);
        Assert.Contains("userId", json);
        Assert.Contains("machineId", json);
    }

    [Fact]
    public async Task GetActivitiesForMachineAsync_ReturnsListOfLogs()
    {
        var expected = new ObservableCollection<UserActivityLog>
        {
            new UserActivityLog
            {
               
                UserId = 1,
                MachineId = 2,
                Action = "Started VPN",
                IpAddress = "127.0.0.1",
                UserAgent = "MDE-Client",
                Timestamp = DateTime.UtcNow
            }
        };

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri.ToString().Contains("api/activity/2")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(expected)
            });

        var service = new UserActivityService(_httpClient, _mockConfig.Object, _authSession);
        var result = await service.GetActivitiesForMachineAsync(2);

        Assert.Single(result);
        Assert.Equal("Started VPN", result[0].Action);
    }
}
