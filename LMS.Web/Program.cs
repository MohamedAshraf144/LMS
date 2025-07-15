// LMS.Web/Program.cs - Updated with proper authentication
using Microsoft.EntityFrameworkCore;
using LMS.Infrastructure.Data;
using LMS.Shared.Extensions;
using LMS.Core.Models.PayMob;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// LMS Services (includes PayMob services)
builder.Services.AddLMSServices();

// PayMob Configuration
builder.Services.Configure<PayMobConfiguration>(
    builder.Configuration.GetSection("PayMob"));

// JWT Authentication Configuration
var jwtKey = builder.Configuration["Jwt:Key"];
if (!string.IsNullOrEmpty(jwtKey))
{
    var key = Encoding.ASCII.GetBytes(jwtKey);

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ClockSkew = TimeSpan.Zero
        };
    });
}

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "LMS.Session";
    options.Cookie.SameSite = SameSiteMode.None; // مهم جداً مع HTTPS
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // مهم جداً مع HTTPS
});

// Add HTTP Client for external API calls
builder.Services.AddHttpClient();

// CORS for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins("https://localhost:7278", "http://localhost:5163")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Seed Database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        context.Database.EnsureCreated();
        SeedData.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Use CORS
app.UseCors("AllowLocalhost");

// Use Session
app.UseSession();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Default MVC Routes
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Payment callback routes (for PayMob redirects)
app.MapGet("/payment/success", async (HttpContext context) =>
{
    var paymentId = context.Request.Query["payment_id"].FirstOrDefault();
    context.Response.Redirect($"/Payment/Success?payment_id={paymentId}");
});

app.MapGet("/payment/cancel", async (HttpContext context) =>
{
    var paymentId = context.Request.Query["payment_id"].FirstOrDefault();
    context.Response.Redirect($"/Payment/Cancel?payment_id={paymentId}");
});

app.MapGet("/payment/error", async (HttpContext context) =>
{
    context.Response.Redirect("/Payment/Error");
});

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    Timestamp = DateTime.UtcNow,
    Environment = app.Environment.EnvironmentName
}));

// API endpoints for AJAX calls (if needed)
app.MapGet("/api/payment/status/{id}", async (int id, IServiceProvider services) =>
{
    try
    {
        var paymentService = services.GetRequiredService<LMS.Core.Interfaces.Services.IPaymentService>();
        var payment = await paymentService.GetPaymentByIdAsync(id);

        if (payment == null)
            return Results.NotFound();

        return Results.Ok(new
        {
            Id = payment.Id,
            Status = payment.Status.ToString(),
            Amount = payment.Amount,
            Currency = payment.Currency
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.Run();