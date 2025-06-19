using MDE_Client.Application;
using MDE_Client.Application.Interfaces;
using MDE_Client.Pages.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class LoginModelTests
{
    private readonly Mock<IAuthenticationService> _mockAuthService = new();
    private readonly Mock<ILogger<LoginModel>> _mockLogger = new();
    private readonly Mock<AuthSession> _mockSession = new();
    private readonly Mock<IConfiguration> _mockIConfig = new();

    /// private LoginModel CreateModel() =>
    //  new LoginModel(_mockLogger.Object, _mockAuthService.Object, _mockSession.Object);

    private LoginModel CreateModel()
    {
        var fakeSession = new AuthSession(_mockAuthService.Object, _mockIConfig.Object)
        {
            Role = "1",
            UserId = "5",
            CompanyId = "99"
        };

        return new LoginModel(_mockLogger.Object, _mockAuthService.Object, fakeSession);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsPage_WhenUsernameOrPasswordMissing()
    {
        var model = CreateModel();
        model.Username = "";
        model.Password = "";

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("❌ Please enter both username and password.", model.Message);
    }

    [Fact]
    public async Task OnPostAsync_Redirects_WhenLoginSuccessful()
    {
        var model = CreateModel();
        model.Username = "alice";
        model.Password = "secret";

        _mockAuthService.Setup(s => s.LoginAsync("alice", "secret"))
                        .ReturnsAsync(true);

        var result = await model.OnPostAsync();

        _mockAuthService.Verify(s => s.Logout(), Times.Once);
        _mockAuthService.Verify(s => s.LoginAsync("alice", "secret"), Times.Once);
       // _mockSession.Verify(s => s.ParseToken(), Times.Once);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Machine/Machines", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsPage_WhenLoginFails()
    {
        var model = CreateModel();
        model.Username = "alice";
        model.Password = "wrongpassword";

        _mockAuthService.Setup(s => s.LoginAsync("alice", "wrongpassword"))
                        .ReturnsAsync(false);

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("❌ Invalid login credentials.", model.Message);
    }
}
