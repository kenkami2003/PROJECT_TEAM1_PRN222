using BoardingHouseManagement.Models;
using BoardingHouseManagement.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Index";
        options.AccessDeniedPath = "/Login/Index";
        options.Cookie.Name = "PRN222.Auth";
    })
    .AddCookie("ExternalCookie")
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
        options.SignInScheme = "ExternalCookie";
    });

// Add services to the container.
builder.Services.Configure<BoardingHouseManagement.Models.VnPay.VnPayOptions>(
    builder.Configuration.GetSection(BoardingHouseManagement.Models.VnPay.VnPayOptions.ConfigName));

builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<BoardingHouseManagement.Services.VnPay.IVnPayService, BoardingHouseManagement.Services.VnPay.VnPayService>();
builder.Services.AddMemoryCache();
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();


app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "admim",
    pattern: "Admin/{controller=Portal}/{action=Index}/{id?}");

app.MapControllers();

app.Run();
