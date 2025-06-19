using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Moq;
using MDE_Client.Application.Interfaces;
using MDE_Client.Domain.Models;
using MDE_Client.Pages.Admin;
using MDE_Client.Application;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Configuration;
using System.Collections.ObjectModel;

public class AdminPanelModelTests
{
    private readonly Mock<IUserService> _mockUserService = new();
    private readonly Mock<IMachineService> _mockMachineService = new();
    private readonly Mock<ICompanyService> _mockCompanyService = new();
    private readonly Mock<IAuthenticationService> _mockAuthService = new();
    private readonly Mock<IDashboardService> _mockDashboardService = new();
    private readonly Mock<IUserActivityService> _mockUserActivityService = new();

    private readonly Mock<IConfiguration> _mockIConfig = new();

    [Fact]
    public async Task OnGetAsync_Forbids_WhenUserIsNotAdmin()
    {
        var model = CreateModel(role: "2");
        var result = await model.OnGetAsync();
        model.Users = new List<SelectListItem>
{
    new SelectListItem { Value = "1", Text = "alice" },
    new SelectListItem { Value = "2", Text = "bob" }
};

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenUserIsAdmin()
    {
        var model = CreateModel(role: "1");
       /* model.Users = new List<SelectListItem>
{
    new SelectListItem { Value = "1", Text = "alice" },
    new SelectListItem { Value = "2", Text = "bob" }
};*/

        _mockUserService.Setup(s => s.GetAllUsersAsync()).ReturnsAsync(new System.Collections.ObjectModel.ObservableCollection<User> {
                new User { UserID = 1, Username = "admin" }
            });

        var result = await model.OnGetAsync();

        var json = Assert.IsType<Microsoft.AspNetCore.Mvc.RazorPages.PageResult>(result);
        Assert.Single(model.Users);
    }

    [Fact]
    public async Task OnPostCreateCompanyAsync_Forbids_WhenUserIsNotAdmin()
    {
        var model = CreateModel(role: "3");
        model.Users = new List<SelectListItem>
{
    new SelectListItem { Value = "1", Text = "alice" },
    new SelectListItem { Value = "2", Text = "bob" }
};

        var result = await model.OnPostCreateCompanyAsync();

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task OnPostCreateCompanyAsync_ReturnsPage_WhenCompanyNameMissing()
    {
        var model = CreateModel(role: "1");
        model.CompanyName = "";
        _mockUserService.Setup(s => s.GetAllUsersAsync()).ReturnsAsync(new System.Collections.ObjectModel.ObservableCollection<User> {
                new User { UserID = 1, Username = "admin" }
            });

        var result = await model.OnPostCreateCompanyAsync();

        var json = Assert.IsType<Microsoft.AspNetCore.Mvc.RazorPages.PageResult>(result);
        Assert.False(model.ModelState.IsValid);
    }

    [Fact]
    public async Task OnPostCreateCompanyAsync_AddsCompany_WhenValid()
    {
        var model = CreateModel(role: "1");
        model.CompanyName = "TestCo";
        model.CompanyDescription = "TestDesc";
        _mockUserService.Setup(s => s.GetAllUsersAsync()).ReturnsAsync(new System.Collections.ObjectModel.ObservableCollection<User> {
                new User { UserID = 1, Username = "admin" }
            });

        _mockCompanyService.Setup(s => s.AddCompanyAsync("TestCo", "TestDesc"))
            .Returns(Task.CompletedTask);

        var result = await model.OnPostCreateCompanyAsync();

        Assert.IsType<RedirectToPageResult>(result);
    }

    [Fact]
    public async Task OnPostDownloadGeneratedAsync_ReturnsFile_WhenSuccessful()
    {
        var model = CreateModel(role: "1");
        model.SelectedUserId = 5;
        model.MachineName = "M1";
        model.Description = "Desc";
        _mockUserService.Setup(s => s.GetAllUsersAsync()).ReturnsAsync(new System.Collections.ObjectModel.ObservableCollection<User> {
                new User { UserID = 1, Username = "admin" }
            });

        var user = new User { UserID = 5, CompanyID = 99 };
        var company = new Company { CompanyID = 99, Name = "C" };

        _mockUserService.Setup(s => s.GetUserByIdAsync(5)).ReturnsAsync(user);
        _mockCompanyService.Setup(s => s.GetCompanyByIdAsync(99)).ReturnsAsync(company);
        _mockMachineService.Setup(s => s.GenerateConfigAsync("M1", It.IsAny<string>(), company, false))
            .ReturnsAsync((System.Text.Encoding.UTF8.GetBytes("dummy"), "vpn.zip"));

        var result = await model.OnPostDownloadGeneratedAsync();

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/zip", fileResult.ContentType);
        Assert.Equal("vpn.zip", fileResult.FileDownloadName);
    }

    [Fact]
    public async Task OnPostDownloadGeneratedAsync_Forbids_WhenNotAdmin()
    {
        var model = CreateModel(role: "3");

        _mockUserService.Setup(s => s.GetAllUsersAsync()).ReturnsAsync(new System.Collections.ObjectModel.ObservableCollection<User> {
                new User { UserID = 1, Username = "admin" }
            });

        var result = await model.OnPostDownloadGeneratedAsync();

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task OnPostDownloadGeneratedAsync_ReturnsPage_WhenMissingInput()
    {
        var model = CreateModel(role: "1");
        model.SelectedUserId = 0;
        model.MachineName = "";
        _mockUserService.Setup(s => s.GetAllUsersAsync()).ReturnsAsync(new System.Collections.ObjectModel.ObservableCollection<User> {
                new User { UserID = 1, Username = "admin" }
            });


        var result = await model.OnPostDownloadGeneratedAsync();

        var json = Assert.IsType<Microsoft.AspNetCore.Mvc.RazorPages.PageResult>(result);
        Assert.False(model.ModelState.IsValid);
    }

    private AdminPanelModel CreateModel(string role = "1")
    {
        var fakeSession = new AuthSession(_mockAuthService.Object, _mockIConfig.Object)
        {
            Role = role,
            UserId = "5",
            CompanyId = "99",
            //Token = "dummy-token"
        };

        return new AdminPanelModel(
            _mockUserService.Object,
            _mockMachineService.Object,
            _mockCompanyService.Object,
            fakeSession
        );
    }
}
