using System.Text;
using InovaGAB.API.Repositories;
using InovaGAB.API.Services;
using InovaGAB.API.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

LoadEnvIfExists(".env");
LoadEnvIfExists("atlas-credentials.env");

static void LoadEnvIfExists(string fileName)
{
    var path = Path.Combine(Directory.GetCurrentDirectory(), fileName);
    if (!File.Exists(path)) return;

    foreach (var line in File.ReadAllLines(path))
    {
        var trimmed = line.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#")) continue;

        var parts = trimmed.Split('=', 2);
        if (parts.Length == 2)
        {
            var key = parts[0].Trim();
            var val = parts[1].Trim().Trim('"').Trim('\'');
            if (!string.IsNullOrEmpty(val) && string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, val);
            }
        }
    }
}

var mongoUriEnv = Environment.GetEnvironmentVariable("MONGODB_URI");
if (!string.IsNullOrWhiteSpace(mongoUriEnv))
{
    var user = Environment.GetEnvironmentVariable("MONGODB_USERNAME");
    var pass = Environment.GetEnvironmentVariable("MONGODB_PASSWORD");
    if (!string.IsNullOrWhiteSpace(user)) mongoUriEnv = mongoUriEnv.Replace("<db_username>", user).Replace("<username>", user);
    if (!string.IsNullOrWhiteSpace(pass)) mongoUriEnv = mongoUriEnv.Replace("<password>", pass);

    builder.Configuration["MongoDbSettings:ConnectionString"] = mongoUriEnv;
}

var mongoDbEnv = Environment.GetEnvironmentVariable("MONGODB_DATABASE");
if (!string.IsNullOrWhiteSpace(mongoDbEnv))
{
    builder.Configuration["MongoDbSettings:DatabaseName"] = mongoDbEnv;
}

builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

builder.Services.Configure<GeminiSettings>(
    builder.Configuration.GetSection("GeminiSettings"));

var jwtSettings = builder.Configuration
    .GetSection("JwtSettings")
    .Get<JwtSettings>()!;

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtSettings.Issuer,
            ValidAudience            = jwtSettings.Audience,
            IssuerSigningKey         = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ClockSkew                = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSingleton<IUserRepository,     UserRepository>();
builder.Services.AddSingleton<IStrategyRepository, StrategyRepository>();
builder.Services.AddSingleton<IIdeaRepository,     IdeaRepository>();
builder.Services.AddSingleton<IProjectRepository,  ProjectRepository>();

builder.Services.AddScoped<IAuthService,     AuthService>();
builder.Services.AddScoped<IStrategyService, StrategyService>();
builder.Services.AddScoped<IIdeaService,     IdeaService>();
builder.Services.AddScoped<IProjectService,  ProjectService>();

builder.Services.AddHttpClient<IGeminiService, GeminiService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "InovaGAB API",
        Version     = "v1",
        Description = "API da Plataforma de Inovação Corporativa do Grupo Águia Branca."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.ApiKey,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Digite: Bearer {seu_token_jwt}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "InovaGAB API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
