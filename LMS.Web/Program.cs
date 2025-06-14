using Microsoft.EntityFrameworkCore;
using LMS.Infrastructure.Data;
using LMS.Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// LMS Services
builder.Services.AddLMSServices();

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // ??? ??? SPA
app.UseRouting();
app.UseSession();
app.UseAuthorization();

// Default MVC Routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// SPA Route (??? ????? ??!)
app.MapGet("/spa", async context =>
{
    var filePath = Path.Combine(app.Environment.WebRootPath, "spa", "index.html");
    if (File.Exists(filePath))
    {
        var fileContent = await File.ReadAllTextAsync(filePath);
        context.Response.ContentType = "text/html";
        await context.Response.WriteAsync(fileContent);
    }
    else
    {
        context.Response.StatusCode = 404;
        await context.Response.WriteAsync("SPA not found - Please create wwwroot/spa/index.html");
    }
});

// Fallback ??? SPA routes (??? client-side routing)
app.MapFallback("/spa/{**path}", async context =>
{
    var filePath = Path.Combine(app.Environment.WebRootPath, "spa", "index.html"); // app ?? builder
    if (File.Exists(filePath))
    {
        var fileContent = await File.ReadAllTextAsync(filePath);
        context.Response.ContentType = "text/html";
        await context.Response.WriteAsync(fileContent);
    }
    else
    {
        context.Response.StatusCode = 404;
        await context.Response.WriteAsync("SPA fallback - index.html not found");
    }
});

app.Run();