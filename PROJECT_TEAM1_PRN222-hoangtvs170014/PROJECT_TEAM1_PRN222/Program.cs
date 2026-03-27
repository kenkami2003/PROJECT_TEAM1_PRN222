using BoardingHouseManagement.Models;
using BoardingHouseManagement.Services.Invoice;
using BoardingHouseManagement.Services.Payment;
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
builder.Services.AddMemoryCache();
builder.Services.AddControllersWithViews();

// Payment module (QR static + manual confirmation)
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IInvoiceAdminService, InvoiceAdminService>();

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
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Seed test user on startup
try
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var userRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleName.ToLower() == "user");
        if (userRole == null)
        {
            userRole = new Role { Id = Guid.NewGuid(), RoleName = "User" };
            context.Roles.Add(userRole);
            await context.SaveChangesAsync();
            Console.WriteLine("✓ Created User role");
        }

        var testUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "testuser");
        if (testUser == null)
        {
            var newUser = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                PasswordHash = "testuser123",
                FullName = "Test User",
                Email = "test@example.com",
                Phone = "0123456789",
                IdentityNumber = "123456789",
                RoleId = userRole.Id,
                IsActive = true
            };
            context.Users.Add(newUser);
            await context.SaveChangesAsync();
            Console.WriteLine("✓ Created test user: testuser / testuser123");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Seed error: {ex.Message}");
}

app.Run();
