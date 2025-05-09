using MDE_Client.Application.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient(); // ✅ Register IHttpClientFactory

builder.Services.AddSingleton<AuthenticationService>(); // Or scoped/transient as needed


// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSingleton<AuthenticationService>();
builder.Services.AddSingleton<MachineService>();
builder.Services.AddSingleton<DashboardService>();

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
app.Urls.Add($"http://0.0.0.0:{port}");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}


app.UseStaticFiles();
app.UseRouting();

app.UseCors("AllowAll");

app.UseAuthorization();
app.MapRazorPages();

app.Run();
