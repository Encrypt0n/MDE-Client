﻿using Xunit;
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

public class UserServiceTests
{
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<IAuthenticationService> _authService;
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly AuthSession _authSession;

    public UserServiceTests()
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

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MDE-Client-Test");
    }

    [Fact]
    public async Task GetAllUsersAsync_ReturnsUsers_WhenSuccessful()
    {
        var json = """
        [
            { "Id": 1, "Username": "alice" },
            { "Id": 2, "Username": "bob" }
        ]
        """;

        HttpRequestMessage capturedRequest = null;

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get && req.RequestUri.ToString().EndsWith("api/users")),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });

        var service = new UserService(_httpClient, _mockConfig.Object, _authSession);
        var users = await service.GetAllUsersAsync();

        Assert.Equal(2, users.Count);
        Assert.Contains(users, u => u.Username == "alice");
        Assert.Contains(users, u => u.Username == "bob");

        // Check headers (Authorization + User-Agent)
        
       

    }

    [Fact]
    public async Task GetAllUsersAsync_ReturnsEmptyCollection_WhenHttpFails()
    {
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var service = new UserService(_httpClient, _mockConfig.Object, _authSession);
        var users = await service.GetAllUsersAsync();

        Assert.Empty(users);
    }

    [Fact]
    public async Task GetAllUsersAsync_ReturnsEmptyCollection_WhenExceptionThrown()
    {
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var service = new UserService(_httpClient, _mockConfig.Object, _authSession);
        var users = await service.GetAllUsersAsync();

        Assert.Empty(users);
    }

    [Fact]
    public async Task GetUserByIdAsync_ReturnsCorrectUser()
    {
        var expectedUser = new User { UserID = 5, Username = "carol" };

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri.ToString().EndsWith("api/users/5")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(expectedUser)
            });

        var service = new UserService(_httpClient, _mockConfig.Object, _authSession);
        var user = await service.GetUserByIdAsync(5);

        Assert.NotNull(user);
        Assert.Equal("carol", user.Username);
    }
}
