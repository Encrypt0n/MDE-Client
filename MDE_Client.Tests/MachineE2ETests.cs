using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MDE_Client.Tests
{
    using System.Diagnostics;
    using System.IO.Compression;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Microsoft.Playwright;
    using Xunit;

    [Collection("E2E")]
    public class MachineE2ETests : IAsyncLifetime
    {
        private IPlaywright _playwright;
        private IBrowser _browser;
        private string _downloadPath;

        public async Task InitializeAsync()
        {
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });

            _downloadPath = Path.Combine(Path.GetTempPath(), "vpn-download-playwright");
            Directory.CreateDirectory(_downloadPath);
        }

        [Fact]
        public async Task AdminCanDownloadVpnConfigZip()
        {
            var context = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                AcceptDownloads = true
            });

            var page = await context.NewPageAsync();
            await page.GotoAsync("https://mde-client-hsamfdfncfc8edea.canadacentral-01.azurewebsites.net/Auth/Login");

            // Login using name attribute instead of id
            await page.FillAsync("[name=\"username\"]", "admin");
            await page.FillAsync("[name=\"password\"]", "admin123");
            await page.ClickAsync("button[type=submit]");

            await page.FillAsync("[name=\"username\"]", "admin");
            await page.FillAsync("[name=\"password\"]", "admin123");
            await page.ClickAsync("button[type=submit]");


            // Navigate to Machine page
            await page.GotoAsync("https://mde-client-hsamfdfncfc8edea.canadacentral-01.azurewebsites.net/Admin/AdminPanel");

            // Select user
            await page.SelectOptionAsync("[name=\"SelectedUserId\"]", new[] { "4" }); // replace "1" with real user ID
            await page.FillAsync("[name=\"MachineName\"]", "test-machine");
            await page.FillAsync("[name=\"Description\"]", "Test config");

            // Intercept the download
            var download = await page.RunAndWaitForDownloadAsync(async () =>
            {
                await page.ClickAsync("button[type=submit]");
            });

            // Save file
            var path = Path.Combine(_downloadPath, download.SuggestedFilename);
            await download.SaveAsAsync(path);

            // Inspect ZIP
            using var zip = ZipFile.OpenRead(path);
            var ovpnEntry = zip.GetEntry("client.ovpn");
            Assert.NotNull(ovpnEntry);

            using var reader = new StreamReader(ovpnEntry.Open(), Encoding.UTF8);
            var content = await reader.ReadToEndAsync();
            Assert.Contains("test-machine", content);
            Assert.Contains("Test config", content);
        }

        [Fact]
        public async Task UserWithCorrectRole_CanStartVpn_AndReceiveJsonCommand()
        {
            var context = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                AcceptDownloads = true
            });
            var _page = await context.NewPageAsync();
            await _page.GotoAsync("https://mde-client-hsamfdfncfc8edea.canadacentral-01.azurewebsites.net/Auth/Login");
            // Login
            await _page.FillAsync("[name=\"username\"]", "engineer");
            await _page.FillAsync("[name=\"password\"]", "Welkom01");
            await _page.ClickAsync("button[type=submit]");
            // Navigate to Machine page
            await _page.GotoAsync("https://mde-client-hsamfdfncfc8edea.canadacentral-01.azurewebsites.net/Machine/Machine/1027");

            // Start VPN
            await _page.ClickAsync("button#startVpnBtn");

            // Intercept the POST response
            var responseTask = _page.WaitForResponseAsync(resp =>
                resp.Url.Contains("/Machine/Machine/18?handler=StartVpn") && resp.Status == 200);

            var response = await responseTask;
            var jsonString = await response.TextAsync();
            Console.WriteLine("Received JSON:");
            Console.WriteLine(jsonString);

            var json = JsonDocument.Parse(jsonString).RootElement;

            if (json.TryGetProperty("command", out var commandProp))
            {
                var command = commandProp.GetString();
                Console.WriteLine($"VPN command: {command}");
                Assert.Equal("start", command);
            }
            else if (json.TryGetProperty("error", out var errorProp))
            {
                var error = errorProp.GetString();
                Assert.False(true, $"Server returned error: {error}");
            }
            else
            {
                Assert.False(true, json.ToString());
            }

            var base64 = json.GetProperty("zipContentBase64").GetString();
            Assert.False(string.IsNullOrEmpty(base64));
            Assert.Matches(@"^[A-Za-z0-9+/=]+$", base64);

            // ---- CHECK IF A ROUTE TO 192.168.0.0 EXISTS ----
            bool routeFound = false;
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "route",
                        Arguments = "print",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                process.WaitForExit();

                Console.WriteLine("Routing Table:");
                Console.WriteLine(output);

                routeFound = output.Contains("192.168.0.0");
            }
            catch (Exception ex)
            {
                Assert.False(true, $"Failed to check routing table: {ex.Message}");
            }

            Assert.True(routeFound, "No route to 192.168.0.0 was found after VPN startup.");
        }



        public async Task DisposeAsync()
        {
            await _browser.DisposeAsync();
            _playwright.Dispose();

            if (Directory.Exists(_downloadPath))
                Directory.Delete(_downloadPath, true);
        }
    }

}
