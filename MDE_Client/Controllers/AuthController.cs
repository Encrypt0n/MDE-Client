using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MDE_Client.Services;

namespace MDE_Client.Controllers
{
    public class AuthController : Controller
    {
        private readonly AuthenticationService _authClient;

        public AuthController(AuthenticationService authClient)
        {
            _authClient = authClient;
        }

        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            bool success = await _authClient.LoginAsync(username, password);
            if (success)
            {
                // Optionally validate token (could be skipped for perf)
                if (_authClient.IsTokenValid())
                {
                    HttpContext.Session.SetString("JWT", _authClient.GetToken());
                    return RedirectToAction("Index", "Home");
                }

                ViewBag.Error = "Token is invalid.";
                return View();
            }

            ViewBag.Error = "Invalid credentials.";
            return View();
        }

        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(string username, string password)
        {
            bool success = await _authClient.RegisterAsync(username, password);
            if (success)
            {
                return RedirectToAction("Login");
            }

            ViewBag.Error = "User already exists or registration failed.";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
