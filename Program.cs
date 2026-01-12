using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using SpecificationPatternDemo;
using SpecificationPatternDemo.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register DbContext with in-memory provider for demo
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("SpecificationDemoDb"));

// Configure options for cleanup service from configuration (appsettings)
builder.Services.Configure<RefreshTokenCleanupOptions>(builder.Configuration.GetSection("RefreshTokenCleanup"));

// Authentication config - demo symmetric key
var jwtKey = "super_secret_demo_key_please_change";
var issuer = "spec-demo";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanModifyPosts", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("CanModifyOwnOrAdmin", policy =>
    {
        policy.RequireAuthenticatedUser();
        // ownership is checked in endpoints; this keeps only authentication here
    });
});

// Simple token helper service for demo
builder.Services.AddSingleton(new JwtOptions { Key = jwtKey, Issuer = issuer });

// Register cleanup background service
builder.Services.AddHostedService<RefreshTokenCleanupService>();

var app = builder.Build();

// Seed data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    DbSeeder.Seed(db);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Serve static files from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public record JwtOptions
{
    public string Key { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
}

// Expose Program for integration tests
public partial class Program { }
