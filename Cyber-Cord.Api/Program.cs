using System.Net;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text;
using Cyber_Cord.Api.Constants;
using Cyber_Cord.Api.Data;
using Cyber_Cord.Api.Entities;
using Cyber_Cord.Api.Extensions;
using Cyber_Cord.Api.Jobs;
using Cyber_Cord.Api.Middleware;
using Cyber_Cord.Api.Options;
using Cyber_Cord.Api.Repositories;
using Cyber_Cord.Api.Services;
using Cyber_Cord.Api.Types.Interfaces;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.EventLog;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Sinks.MSSqlServer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var connectionString = builder.Configuration.GetConnectionString("Default");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString)
    );

builder.Services.AddIdentityCore<User>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;

    // User settings
    options.User.RequireUniqueEmail = false;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddRoles<IdentityRole<int>>()
.AddEntityFrameworkStores<AppDbContext>()
.AddSignInManager()
.AddDefaultTokenProviders();

builder.Services.AddLocalization();

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    string[] cultures = [
        "en-US",
        "cs-CZ"
    ];

    options.SetDefaultCulture("en-US")
        .AddSupportedCultures(cultures)
        .AddSupportedUICultures(cultures);
});

// Configure Hangfire with sql storage
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connectionString));

// Add Hangfire Server (background job worker)
builder.Services.AddHangfireServer();

builder.Services.Configure<EmailSenderOptions>(builder.Configuration.GetSection("EmailSender"));
builder.Services.Configure<ActivationCodeOptions>(builder.Configuration.GetSection("ActivationCode"));
builder.Services.Configure<Cyber_Cord.Api.Options.PasswordOptions>(builder.Configuration.GetSection("PasswordOptions"));
builder.Services.AddScoped<ICustomEmailSender, EmailSender>();
builder.Services.AddScoped<ICustomPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenProvider, TokenProvider>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddMemoryCache();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUsersRepository, UsersRepository>();
builder.Services.AddScoped<IUsersService,UsersService>();
builder.Services.AddScoped<IChatsRepository, ChatsRepository>();
builder.Services.AddScoped<IChatsService, ChatsService>();
builder.Services.AddScoped<IServersRepository, ServersRepository>();
builder.Services.AddScoped<IServersService, ServersService>();
builder.Services.AddScoped<IManagementService, ManagementService>();
builder.Services.AddSingleton<WebSocketHandler>();
builder.Services.AddSingleton<IWebSocketHandler>(sp => sp.GetRequiredService<WebSocketHandler>());
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();
builder.Services.AddScoped<IUserJobs, UserJobs>();
builder.Services.AddScoped<IVoiceService, VoiceService>();


builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.Configure<LiveKitSettings>(
    builder.Configuration.GetSection(LiveKitSettings.Section));

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    var settings = new EventLogSettings();
    settings.SourceName = "Cyber-Cord";

    builder.Logging.AddEventLog(settings);
}
else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
    builder.Logging.AddSystemdConsole();
}

var logger = new LoggerConfiguration()
    .MinimumLevel.Warning()
    .WriteTo.File(
        Path.Combine(Path.GetDirectoryName(Environment.ProcessPath)!, "log.txt"), 
        rollingInterval: RollingInterval.Month,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .AuditTo.MSSqlServer(
        connectionString: connectionString,
        sinkOptions: new MSSqlServerSinkOptions { TableName = "LogEvents" }
        )
    .CreateLogger();

builder.Logging.AddSerilog(logger);

builder.Services.AddWebSockets(options =>
    {
        options.KeepAliveInterval = TimeSpan.FromMinutes(2);
    });

builder.Services.AddControllers()
    .AddNewtonsoftJson();
builder.Services.AddSwaggerGenWithAuth();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var client = builder.Configuration.GetValue<string>("ClientAddress");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor", policy => policy
            .WithOrigins(client!)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
        );
});

builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration[ConfigurationConstants.SecretKeyPath]!)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration[ConfigurationConstants.TokenIssuerPath],
            ValidateAudience = true,
            ValidAudience = builder.Configuration[ConfigurationConstants.TokenAudiencePath],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = ClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.TryGetValue(CookieConstants.JwtName, out var token))
                {
                    context.Token = token;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddHttpClient();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

SeedService.SeedAdminAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting(); 

app.UseCors("AllowBlazor");

app.UseExceptionHandler();

app.UseRequestLocalization();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<PasswordAuthenticationMiddleware>();

app.UseWebSockets();

app.Map("/ws", async context =>
{
    var service = context.RequestServices.GetRequiredService<IWebSocketHandler>();

    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        return;
    }

    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
    await service.StartSessionAsync(webSocket);
});

app.MapControllers();

app.Run();
