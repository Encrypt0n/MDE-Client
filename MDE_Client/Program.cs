using MDE_Client;
using MDE_Client.Application;
using MDE_Client.Application.Interfaces;
using MDE_Client.Application.Services;
using MDE_Client.Domain;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient(); // ✅ Register IHttpClientFactory

builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
builder.Services.AddSingleton<AuthSession>();

// Add services to the container.
builder.Services.AddRazorPages();


builder.Services.AddSingleton<IUserActivityService, UserActivityService>();
builder.Services.AddSingleton<IUserService, UserService>();
builder.Services.AddSingleton<ICompanyService, CompanyService>();
builder.Services.AddSingleton<IMachineService, MachineService>();
builder.Services.AddSingleton<IDashboardService, DashboardService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("1"));
});



// ✅ Configure CORS to allow external access
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();
//builder.Services.AddSingleton<AuthenticationService>();
//builder.Services.AddHttpClient<AuthenticationService>();


// ✅ Allow the app to listen on all network interfaces
var port = "7029"; // Change if needed
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}


app.UseStaticFiles();
app.UseRouting();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();



app.Run();
