using Xunit;
using Moq;
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Configuration;
using Moq.Protected;
using MDE_Client.Application.Services;
using MDE_Client.Domain.Models;
using MDE_Client.Application.Interfaces;
using MDE_Client.Application;

public class MachineServiceTests
{
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<IAuthenticationService> _authService;
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly AuthSession _authSession;

    public MachineServiceTests()
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
    public async Task GetMachinesForCompanyAsync_ReturnsCollection()
    {
        var expected = new ObservableCollection<Machine>
        {
            new Machine { MachineID = 1, Name = "MachineA" }
        };

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri.ToString().Contains("api/machines/company/1")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(expected)
            });

        var service = new MachineService(_httpClient, _mockConfig.Object, _authSession);
        var result = await service.GetMachinesForCompanyAsync(1);

        Assert.Single(result);
        Assert.Equal("MachineA", result[0].Name);
    }

    [Fact]
    public async Task GetMachineByIdAsync_ReturnsCorrectMachine()
    {
        var expected = new Machine { MachineID = 2, Name = "MachineB" };

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri.ToString().Contains("api/machines/2")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(expected)
            });

        var service = new MachineService(_httpClient, _mockConfig.Object, _authSession);
        var result = await service.GetMachineByIdAsync(2);

        Assert.NotNull(result);
        Assert.Equal("MachineB", result.Name);
    }

    [Fact]
    public async Task GenerateUserConfigAsync_ReplacesClientOvpnInZip()
    {
        var originalZip = CreateZipWithFile("client.ovpn", "placeholder");

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("api/vpn/generate-user")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(originalZip)
                {
                    Headers = { ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment") { FileName = "\"test.zip\"" } }
                }
            });

        var service = new MachineService(_httpClient, _mockConfig.Object, _authSession);
        var company = new Company { Name = "Acme", Subnet = "10.0.0.0/24" };
        var (bytes, fileName) = await service.GenerateUserConfigAsync("MachineC", "new config", company);

        Assert.Equal("test.zip", fileName);
        Assert.Contains("new config", ExtractFileFromZip(bytes, "client.ovpn"));
    }

    [Fact]
    public async Task GenerateConfigAsync_ReplacesClientOvpnInZip()
    {
        var originalZip = CreateZipWithFile("client.ovpn", "placeholder");

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("api/vpn/generate-cert")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(originalZip)
                {
                    Headers = { ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment") { FileName = "\"cert.zip\"" } }
                }
            });

        var service = new MachineService(_httpClient, _mockConfig.Object, _authSession);
        var company = new Company { Name = "Acme", Subnet = "10.0.0.0/24" };
        var (bytes, fileName) = await service.GenerateConfigAsync("MachineC", "replacement config", company, true);

        Assert.Equal("cert.zip", fileName);
        Assert.Contains("replacement config", ExtractFileFromZip(bytes, "client.ovpn"));
    }

    [Fact]
    public async Task UpdateMachineDashboardUrlAsync_PostsCorrectContent()
    {
        HttpRequestMessage capturedRequest = null;

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post && req.RequestUri.ToString().Contains("api/machines/123/dashboard-url")),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var service = new MachineService(_httpClient, _mockConfig.Object, _authSession);
        await service.UpdateMachineDashboardUrlAsync(123, "/dashboard/machine");

        var body = await capturedRequest.Content.ReadAsStringAsync();
        Assert.Contains("/dashboard/machine", body);
    }

    // Helpers for zipped content

    private byte[] CreateZipWithFile(string entryName, string content)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry(entryName);
            using var entryStream = entry.Open();
            using var writer = new StreamWriter(entryStream);
            writer.Write(content);
        }
        return ms.ToArray();
    }

    private string ExtractFileFromZip(byte[] zipBytes, string entryName)
    {
        using var ms = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);
        var entry = archive.GetEntry(entryName);
        using var reader = new StreamReader(entry.Open());
        return reader.ReadToEnd();
    }
}
