using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MDE_Client.Pages.Auth;
using MDE_Client.Application.Interfaces;
using MDE_Client.Application.Services;
using MDE_Client.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class RegisterModelTests
{
    private readonly Mock<ILogger<RegisterModel>> _mockLogger = new();
    private readonly Mock<IAuthenticationService> _mockAuthService = new();
    private readonly Mock<ICompanyService> _mockCompanyService = new();

    private RegisterModel CreateModel()
    {
        return new RegisterModel(_mockLogger.Object, _mockAuthService.Object, _mockCompanyService.Object);
    }

    [Fact]
    public async Task OnGet_PopulatesCompanyOptions()
    {
        var model = CreateModel();
        _mockCompanyService.Setup(s => s.GetAllCompaniesAsync())
            .ReturnsAsync(new List<Company>
            {
                new Company { CompanyID = 1, Name = "TestCo" },
                new Company { CompanyID = 2, Name = "AlphaCorp" }
            });

        await model.OnGet();

        Assert.Equal(2, model.CompanyOptions.Count);
        Assert.Contains(model.CompanyOptions, c => c.Text == "TestCo");
        Assert.Contains(model.CompanyOptions, c => c.Text == "AlphaCorp");
    }

    [Fact]
    public async Task OnPostAsync_ReturnsPage_WhenUsernameOrPasswordMissing()
    {
        var model = CreateModel();
        model.Username = "";
        model.Password = "abc";

        _mockCompanyService.Setup(s => s.GetAllCompaniesAsync())
            .ReturnsAsync(new List<Company>
            {
                new Company { CompanyID = 1, Name = "TestCo" }
            });

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("❌ Please enter both username and password.", model.Message);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsPage_WhenSuccessfulRegister()
    {
        var model = CreateModel();
        model.Username = "testuser";
        model.Password = "password123";
        model.SelectedCompanyId = 1;

        _mockCompanyService.Setup(s => s.GetAllCompaniesAsync())
            .ReturnsAsync(new List<Company>
            {
                new Company { CompanyID = 1, Name = "TestCo" }
            });

        _mockAuthService.Setup(s => s.RegisterAsync("testuser", "password123", 1))
            .ReturnsAsync(true);

        _mockAuthService.Setup(s => s.LoginAsync("testuser", "password123"))
            .ReturnsAsync(false);

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("❌ Invalid login credentials.", model.Message);
    }
}
