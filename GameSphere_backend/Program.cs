using GameSphere_backend.Data;
using GameSphere_backend.Authorization;
using GameSphere_backend.Interfaces;
using GameSphere_backend.Models.FrontendModels;
using GameSphere_backend.Security;
using GameSphere_backend.Services;
using GameSphere_backend.Services.QuizAdministration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
//DotNetEnv.Env.Load();

var connectionString = GetRequiredConfiguration(config, "ConnectionStrings:GameSphereDB");
var jwtSecret = GetRequiredConfiguration(config, "JwtSettings:SecretKey");
var jwtIssuer = GetRequiredConfiguration(config, "JwtSettings:Issuer");
var jwtAudience = GetRequiredConfiguration(config, "JwtSettings:Audience");
var configuredCorsOrigins = config.GetSection("Cors:AllowedOrigins")
    .GetChildren()
    .Select(section => section.Value)
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin!.TrimEnd('/'))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

var corsOrigins = configuredCorsOrigins.Length > 0
    ? configuredCorsOrigins
    : builder.Environment.IsDevelopment()
        ? ["http://localhost:5173", "http://127.0.0.1:5173"]
        : throw new InvalidOperationException("Configuration section 'Cors:AllowedOrigins' is required outside Development.");

if (Encoding.UTF8.GetByteCount(jwtSecret) < 32)
{
    throw new InvalidOperationException("Configuration key 'JwtSettings:SecretKey' must contain at least 32 bytes.");
}

//var specificOrgins = "AppOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy("App", builder =>
    {
        builder.WithOrigins(corsOrigins)
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});
// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
options.UseNpgsql(connectionString)
);

// Adicionar os servi�os MVC e outras configura��es necess�rias
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserQueryService, UserQueryService>();
builder.Services.AddScoped<IUserRegistrationService, UserRegistrationService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<IUserDeletionService, UserDeletionService>();
builder.Services.AddScoped<IPasswordLoginService, PasswordLoginService>();
builder.Services.AddScoped<ISocialLoginService, SocialLoginService>();
builder.Services.AddScoped<IPasswordRecoveryService, PasswordRecoveryService>();
builder.Services.AddScoped<IPasswordResetInputValidator, PasswordResetInputValidator>();
builder.Services.AddScoped<PasswordResetCodeService>();
builder.Services.AddScoped<EmailChangeCodeService>();
builder.Services.AddScoped<AccountReauthenticator>();
builder.Services.AddScoped<IPasswordChangeService, PasswordChangeService>();
builder.Services.AddScoped<IEmailChangeService, EmailChangeService>();
builder.Services.AddScoped<AuthenticationCookieService>();
builder.Services.AddScoped<QuizAttemptValidator>();
builder.Services.AddScoped<QuizAttemptScorer>();
builder.Services.AddScoped<QuizAttemptPersistenceService>();
builder.Services.AddScoped<IQuizCatalogService, QuizCatalogService>();
builder.Services.AddScoped<IQuestionShapeValidator, QuestionShapeValidator>();
builder.Services.AddScoped<IQuestionAnswersValidator, QuestionAnswersValidator>();
builder.Services.AddScoped<ICorrectAnswerValidator, CorrectAnswerValidator>();
builder.Services.AddScoped<QuestionRequestNormalizer>();
builder.Services.AddScoped<QuizRequestNormalizer>();
builder.Services.AddScoped<GetQuizzesHandler>();
builder.Services.AddScoped<GetQuizByIdHandler>();
builder.Services.AddScoped<CreateQuizHandler>();
builder.Services.AddScoped<UpdateQuizHandler>();
builder.Services.AddScoped<DeleteQuizHandler>();
builder.Services.AddScoped<CreateQuestionHandler>();
builder.Services.AddScoped<UpdateQuestionHandler>();
builder.Services.AddScoped<DeleteQuestionHandler>();
builder.Services.AddScoped<IQuizAdministrationService, QuizAdministrationService>();
builder.Services.AddScoped(provider =>
    InitialAdminConfiguration.Load(provider.GetRequiredService<IConfiguration>()));
builder.Services.AddScoped<InitialAdminProvisioner>();
builder.Services.AddScoped<InitialAdminBootstrapper>();
builder.Services.AddScoped<IAuthorizationHandler, ActiveAdminAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ActiveUserAuthorizationHandler>();
builder.Services.AddSingleton<IFirebaseTokenVerifier, FirebaseTokenVerifier>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

builder.Services.AddOptions<EmailSettings>()
    .Bind(config.GetSection("EmailSettings"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddTransient<IEmailService, EmailService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.Zero // Expira no tempo exato
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (string.IsNullOrWhiteSpace(context.Token) &&
                    context.Request.Cookies.TryGetValue("gamesphere_access_token", out var cookieToken))
                {
                    context.Token = cookieToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(ActiveAdminRequirement.PolicyName, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new ActiveAdminRequirement());
    });
    options.AddPolicy(ActiveUserRequirement.PolicyName, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new ActiveUserRequirement());
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "GameSphere API", Version = "v1.3", Description = "Backend endpoints of GameSphere" });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "JWT Authentication",
        Description = "Enter your JWT token in this field",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    };

    c.AddSecurityDefinition("Bearer", securityScheme);

    var securityRequirement = new OpenApiSecurityRequirement
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
    };

    c.AddSecurityRequirement(securityRequirement);

    // Inclui os coment�rios XML se estiverem ativados
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

if (!app.Environment.IsEnvironment("DesignTime"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var bootstrapper = scope.ServiceProvider.GetRequiredService<InitialAdminBootstrapper>();
    await bootstrapper.EnsureAdminAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "GameSphere v1.3");
    });
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseMiddleware<OriginProtectionMiddleware>();
app.UseCors("App");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static string GetRequiredConfiguration(IConfiguration configuration, string key)
{
    var value = configuration[key];

    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException($"Configuration key '{key}' is required.");
    }

    return value;
}

public partial class Program { }
