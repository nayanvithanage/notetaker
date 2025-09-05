using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Notetaker.Api.Configuration;
using Notetaker.Api.Data;
using Notetaker.Api.Services;
using Serilog;
using System.Text;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/notetaker-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger with JWT authentication
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Notetaker API", Version = "v1" });
    
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
            Array.Empty<string>()
        }
    });
});

// Configure database
builder.Services.AddDbContext<NotetakerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Configure settings
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("App"));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JWT"));
builder.Services.Configure<GoogleSettings>(builder.Configuration.GetSection("Google"));
builder.Services.Configure<RecallAiSettings>(builder.Configuration.GetSection("RecallAi"));
builder.Services.Configure<LinkedInSettings>(builder.Configuration.GetSection("LinkedIn"));
builder.Services.Configure<FacebookSettings>(builder.Configuration.GetSection("Facebook"));
builder.Services.Configure<OpenAiSettings>(builder.Configuration.GetSection("OpenAI"));
builder.Services.Configure<BotSettings>(builder.Configuration.GetSection("Bot"));

// Register configuration classes directly for services
builder.Services.AddSingleton(builder.Configuration.GetSection("JWT").Get<JwtSettings>() ?? new JwtSettings());
builder.Services.AddSingleton(builder.Configuration.GetSection("Google").Get<GoogleSettings>() ?? new GoogleSettings());
builder.Services.AddSingleton(builder.Configuration.GetSection("RecallAi").Get<RecallAiSettings>() ?? new RecallAiSettings());
builder.Services.AddSingleton(builder.Configuration.GetSection("LinkedIn").Get<LinkedInSettings>() ?? new LinkedInSettings());
builder.Services.AddSingleton(builder.Configuration.GetSection("Facebook").Get<FacebookSettings>() ?? new FacebookSettings());
builder.Services.AddSingleton(builder.Configuration.GetSection("OpenAI").Get<OpenAiSettings>() ?? new OpenAiSettings());
builder.Services.AddSingleton(builder.Configuration.GetSection("Bot").Get<BotSettings>() ?? new BotSettings());

// Configure JWT authentication
var jwtSettings = builder.Configuration.GetSection("JWT").Get<JwtSettings>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings?.Issuer,
            ValidAudience = jwtSettings?.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings?.SigningKey ?? ""))
        };
    });

// Configure Google authentication
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        var googleSettings = builder.Configuration.GetSection("Google").Get<GoogleSettings>();
        options.ClientId = googleSettings?.ClientId ?? "";
        options.ClientSecret = googleSettings?.ClientSecret ?? "";
    });

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configure Data Protection
builder.Services.AddDataProtection();

// Configure Hangfire
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddHangfireServer();

// Register services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IMeetingService, MeetingService>();
builder.Services.AddScoped<IAutomationService, AutomationService>();
builder.Services.AddScoped<ICalendarService, NotetakerCalendarService>();
builder.Services.AddScoped<IRecallAiService, RecallAiService>();
builder.Services.AddScoped<Notetaker.Api.Jobs.BackgroundJobs>();

// Configure HTTP clients
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// Configure Hangfire dashboard
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<NotetakerDbContext>();
    context.Database.EnsureCreated();
}

// Configure background jobs
using (var scope = app.Services.CreateScope())
{
    var backgroundJobs = scope.ServiceProvider.GetRequiredService<Notetaker.Api.Jobs.BackgroundJobs>();
    Notetaker.Api.Jobs.BackgroundJobs.ConfigureRecurringJobs();
}

app.Run();

// Simple authorization filter for Hangfire dashboard
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // In production, implement proper authorization
        return true;
    }
}
