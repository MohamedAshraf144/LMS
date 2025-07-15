// LMS.API/Program.cs (Updated with PayMob Webhook Support)
using LMS.Core.Models.PayMob;
using LMS.Infrastructure.Data;
using LMS.Shared.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using LMS.Core.Interfaces.Services;
using LMS.Core.Enums;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Custom LMS Services
builder.Services.AddLMSServices();

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"];
var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
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

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LMS API",
        Version = "v1",
        Description = "Learning Management System API"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebAndSPA", policy =>
    {
        policy.WithOrigins("https://localhost:7095") // Web App port
             .AllowAnyMethod()
             .AllowAnyHeader()
             .AllowCredentials();
    });
});

// PayMob Configuration
builder.Services.Configure<PayMobConfiguration>(
    builder.Configuration.GetSection("PayMob"));

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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LMS API V1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowWebAndSPA");
app.UseAuthentication();
app.UseAuthorization();

// ================================================================================================
// PayMob Webhook Endpoint
// ================================================================================================
app.MapPost("/api/payments/webhook", async (
    HttpContext context,
    IPaymentService paymentService,
    IPayMobService payMobService,
    ILogger<Program> logger) =>
{
    try
    {
        // Read the raw payload
        var payload = await new StreamReader(context.Request.Body).ReadToEndAsync();
        var signature = context.Request.Headers["X-PayMob-Signature"].FirstOrDefault();

        logger.LogInformation("Received PayMob webhook. Payload length: {PayloadLength}", payload.Length);
        logger.LogDebug("Webhook payload: {Payload}", payload);

        // Verify webhook signature (if configured)
        var isValid = await payMobService.VerifyWebhookAsync(payload, signature ?? "");
        if (!isValid)
        {
            logger.LogWarning("Invalid webhook signature received");
            return Results.Unauthorized();
        }

        // Process webhook data
        var transaction = await payMobService.ProcessWebhookAsync(payload);
        if (transaction == null)
        {
            logger.LogError("Failed to parse webhook transaction data");
            return Results.BadRequest("Invalid webhook data");
        }

        logger.LogInformation("Processing transaction: ID={TransactionId}, Success={Success}, Amount={Amount}",
            transaction.id, transaction.success, transaction.amount_cents);

        // Update payment status based on transaction result
        var paymentStatus = transaction.success ? PaymentStatus.Completed : PaymentStatus.Failed;

        var processed = await paymentService.ProcessWebhookPaymentAsync(
            transaction.id.ToString(),
            paymentStatus);

        if (processed)
        {
            logger.LogInformation("Successfully processed webhook for transaction {TransactionId}", transaction.id);
            return Results.Ok(new
            {
                message = "Webhook processed successfully",
                transactionId = transaction.id,
                status = paymentStatus.ToString()
            });
        }
        else
        {
            logger.LogWarning("Failed to process webhook for transaction {TransactionId}", transaction.id);
            return Results.BadRequest("Failed to process webhook");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error processing PayMob webhook");
        return Results.StatusCode(500);
    }
})
.AllowAnonymous(); // Allow anonymous access for webhooks

// ================================================================================================
// PayMob Callback Endpoints (for redirect URLs)
// ================================================================================================

// Success callback
app.MapGet("/api/payments/callback/success", async (
    HttpContext context,
    ILogger<Program> logger) =>
{
    try
    {
        var paymentId = context.Request.Query["payment_id"].FirstOrDefault();
        var orderId = context.Request.Query["order_id"].FirstOrDefault();
        var transactionId = context.Request.Query["id"].FirstOrDefault();

        logger.LogInformation("PayMob success callback: PaymentId={PaymentId}, OrderId={OrderId}, TransactionId={TransactionId}",
            paymentId, orderId, transactionId);

        // Redirect to web app success page
        var redirectUrl = $"https://localhost:7095/Payment/Success?payment_id={paymentId}&order_id={orderId}";
        return Results.Redirect(redirectUrl);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error handling PayMob success callback");
        return Results.Redirect("https://localhost:7095/Payment/Error");
    }
})
.AllowAnonymous();

// Cancel callback
app.MapGet("/api/payments/callback/cancel", async (
    HttpContext context,
    IPaymentService paymentService,
    ILogger<Program> logger) =>
{
    try
    {
        var paymentId = context.Request.Query["payment_id"].FirstOrDefault();
        var orderId = context.Request.Query["order_id"].FirstOrDefault();

        logger.LogInformation("PayMob cancel callback: PaymentId={PaymentId}, OrderId={OrderId}",
            paymentId, orderId);

        // Mark payment as cancelled if we have the payment ID
        if (!string.IsNullOrEmpty(paymentId) && int.TryParse(paymentId, out int paymentIdInt))
        {
            await paymentService.FailPaymentAsync(paymentIdInt, "Payment cancelled by user");
        }

        // Redirect to web app cancel page
        var redirectUrl = $"https://localhost:7095/Payment/Cancel?payment_id={paymentId}&order_id={orderId}";
        return Results.Redirect(redirectUrl);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error handling PayMob cancel callback");
        return Results.Redirect("https://localhost:7095/Payment/Error");
    }
})
.AllowAnonymous();

// Error callback
app.MapGet("/api/payments/callback/error", async (
    HttpContext context,
    ILogger<Program> logger) =>
{
    try
    {
        var paymentId = context.Request.Query["payment_id"].FirstOrDefault();
        var error = context.Request.Query["error"].FirstOrDefault();

        logger.LogError("PayMob error callback: PaymentId={PaymentId}, Error={Error}",
            paymentId, error);

        // Redirect to web app error page
        var redirectUrl = $"https://localhost:7095/Payment/Error?payment_id={paymentId}&error={error}";
        return Results.Redirect(redirectUrl);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error handling PayMob error callback");
        return Results.Redirect("https://localhost:7095/Payment/Error");
    }
})
.AllowAnonymous();

// ================================================================================================
// Additional API Endpoints for Payment Status Updates
// ================================================================================================

// Endpoint to manually update payment status (for admin/testing)
app.MapPut("/api/payments/{id}/status", async (
    int id,
    PaymentStatusUpdateRequest request,
    IPaymentService paymentService,
    ILogger<Program> logger) =>
{
    try
    {
        var success = await paymentService.ProcessWebhookPaymentAsync(
            id.ToString(),
            request.Status);

        if (success)
        {
            logger.LogInformation("Payment status updated: PaymentId={PaymentId}, Status={Status}",
                id, request.Status);
            return Results.Ok(new { message = "Payment status updated successfully" });
        }
        else
        {
            return Results.BadRequest("Failed to update payment status");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error updating payment status for payment {PaymentId}", id);
        return Results.StatusCode(500);
    }
})
.RequireAuthorization(); // Require authentication for manual updates

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    Timestamp = DateTime.UtcNow,
    Environment = app.Environment.EnvironmentName,
    Version = "1.0.0"
}))
.AllowAnonymous();

// API status endpoint
app.MapGet("/api/status", () => Results.Ok(new
{
    API = "LMS API",
    Status = "Running",
    Timestamp = DateTime.UtcNow,
    PayMobIntegration = "Active"
}))
.AllowAnonymous();

app.MapControllers();

app.Run();

// ================================================================================================
// Supporting Classes
// ================================================================================================

public class PaymentStatusUpdateRequest
{
    public PaymentStatus Status { get; set; }
    public string? Reason { get; set; }
}