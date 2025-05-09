using MDE_Client.Services;
using Microsoft.AspNetCore.Mvc;
using MDE_Client.Services;
using MDE_Client.Models;

namespace MDE_Client.Controllers
{
    public class MachineController : Controller
    {
        private readonly MachineService _machineService;
        private readonly DashboardService _dashboardService;

        public MachineController(MachineService machineService, DashboardService dashboardService)
        {
            _machineService = machineService;
            _dashboardService = dashboardService;
        }

        [HttpGet("select/{machineId}")]
        public ActionResult<List<DashboardPage>> SelectMachine(int machineId)
        {
            var dashboardPages = _dashboardService.GetDashboardPages(machineId);
            if (dashboardPages == null || dashboardPages.Count == 0)
            {
                return NotFound("No dashboard pages found for this machine.");
            }

            return Ok(dashboardPages);
        }

        /* public IActionResult List()
         {
            // var machines = _machineService.GetMachines();
           //  return View(machines);
         }*/
    }
}
