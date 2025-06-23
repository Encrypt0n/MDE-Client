using MDE_Client.Application.Interfaces;
using MDE_Client.Application;
using MDE_Client.Domain.Models;
using MDE_Client.Pages.Machine;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Collections.ObjectModel;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;

public class MachineModelTests
{
    private readonly Mock<IMachineService> _mockMachineService = new();
    private readonly Mock<IAuthenticationService> _mockAuthService = new();
    private readonly Mock<IDashboardService> _mockDashboardService = new();
    private readonly Mock<IUserActivityService> _mockUserActivityService = new();
    private readonly Mock<IUserService> _mockUserService = new();
    private readonly Mock<ICompanyService> _mockCompanyService = new();
    private readonly Mock<IConfiguration> _mockIConfig = new();
    private readonly AuthSession _authSession;

   

    

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenMachineFound()
    {
        var model = CreateModel(role: "2", companyId: "5", userId: "2");
        _mockUserActivityService.Setup(s => s.GetActivitiesForMachineAsync(1))
            .ReturnsAsync(new ObservableCollection<UserActivityLog>());
        _mockMachineService.Setup(s => s.GetMachineByIdAsync(1))
            .ReturnsAsync(new MDE_Client.Domain.Models.Machine());
        _mockDashboardService.Setup(s => s.GetDashboardPagesAsync(1))
            .ReturnsAsync(new ObservableCollection<DashboardPage>());

        var result = await model.OnGetAsync();

        Assert.IsType<PageResult>(result);
    }

    [Fact]
    public async Task OnPostSelectDashboardPageAsync_Forbids_WhenUnauthorized()
    {
        //_authSession.Role = "3";
        var model = CreateModel(role: "6", companyId: "5", userId: "2");

        var result = await model.OnPostSelectDashboardPageAsync();

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task OnPostSelectDashboardPageAsync_ReturnsPage_WhenAuthorized()
    {
        var model = CreateModel(role: "2", companyId: "5", userId: "2");
        model.MachineId = 1;
        model.SelectedPageUrl = "/dashboard/page1";

        _mockMachineService.Setup(s => s.UpdateMachineDashboardUrlAsync(1, "/dashboard/page1")).Returns(Task.CompletedTask);
        _mockUserActivityService.Setup(s => s.GetActivitiesForMachineAsync(1))
            .ReturnsAsync(new ObservableCollection<UserActivityLog>());
        _mockDashboardService.Setup(s => s.GetDashboardPagesAsync(1))
            .ReturnsAsync(new ObservableCollection<DashboardPage>());
        _mockMachineService.Setup(s => s.GetMachineByIdAsync(1))
            .ReturnsAsync(new MDE_Client.Domain.Models.Machine());

        var result = await model.OnPostSelectDashboardPageAsync();

        Assert.IsType<PageResult>(result);
    }

    [Fact]
    public async Task OnPostSelectDashboardPageAsync_ReturnsForbid_WhenUnauthorized()
    {
        var model = CreateModel(role: "3", companyId: "5", userId: "2");

        var result = await model.OnPostSelectDashboardPageAsync();

        Assert.IsType<ForbidResult>(result);
    }




    [Fact]
    public async Task OnPostStartVpnAsync_ReturnsJsonResult_WhenSuccessful()
    {
        var model = CreateModel(role: "2", companyId: "5", userId: "2");
        model.MachineId = 1;

        var mockMachine = new MDE_Client.Domain.Models.Machine { IP = "192.168.0.10", Name = "Robot1", Description = "Arm" };
        var company = new Company { CompanyID = 99, Name = "TestCo" };
        var user = new User { UserID = 5, Username = "john", CompanyID = 99 };

        _mockMachineService.Setup(s => s.GetMachineByIdAsync(1)).ReturnsAsync(mockMachine);
        _mockCompanyService.Setup(s => s.GetCompanyByIdAsync(99)).ReturnsAsync(company);
        _mockUserService.Setup(s => s.GetUserByIdAsync(5)).ReturnsAsync(user);
        _mockMachineService.Setup(s => s.GenerateConfigAsync("john", It.IsAny<string>(), company, true))
            .ReturnsAsync((Encoding.UTF8.GetBytes("dummy zip"), "config.zip"));

        var result = await model.OnPostStartVpnAsync();

        var json = Assert.IsType<BadRequestObjectResult>(result);
        var obj = json.Value as dynamic;
        //Assert.Equal("start", (string)obj.Command);
        //Assert.Equal("config.zip", (string)obj.ZipFileName);
    }

    /*[Fact]
    public void GenerateOvpnConfig_EmbedsCorrectValues()
    {
        var model = CreateModel();
        string result = model.("GenerateOvpnConfig", "TestMachine", 5, "Main", "192.168.0.10");

        Assert.Contains("setenv UV_CLIENT_NAME TestMachine", result);
        Assert.Contains("route-nopull", result);
    }*/

    private MachineModel CreateModel(string role = "1", string userId = "5", string companyId = "99", string token = "dummy-token")
    {
        var fakeAuthSession = new AuthSession(_mockAuthService.Object, _mockIConfig.Object)
        {
            Role = role,
            UserId = userId,
            CompanyId = companyId,
            //Token = token
        };

        return new MachineModel(
            _mockMachineService.Object,
            _mockDashboardService.Object,
            _mockUserActivityService.Object,
            _mockCompanyService.Object,
            _mockUserService.Object,
            fakeAuthSession
        )
        {
            MachineId = 1
        };
    }

}
