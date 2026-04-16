using AgricultureAPI.Application.Interfaces;
using AgricultureAPI.Application.Services;
using AgricultureAPI.Infrastructure.MongoDB;
using AgricultureAPI.Infrastructure.Repositories;
using AgricultureAPI.Infrastructure.Settings;
using AgricultureAPI.API.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// MongoDB
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDB"));
builder.Services.AddSingleton<MongoDbContext>();

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IHistoryRepository, HistoryRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IHistoryService, HistoryService>();

// Controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Replace AddSwaggerGen with this simpler version that works with Swashbuckle 8:
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Agriculture API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token here"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// CORS — localhost vs 127.0.0.1 are different browser origins; allow both for Vite/React dev on any port.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
        policy
            .SetIsOriginAllowed(static origin =>
            {
                if (string.IsNullOrWhiteSpace(origin)) return false;
                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)) return false;
                if (uri.Scheme != "http") return false;
                var h = uri.Host;
                if (string.Equals(h, "localhost", StringComparison.OrdinalIgnoreCase) || h == "127.0.0.1")
                    return true;
                if (System.Net.IPAddress.TryParse(h, out var ip) && ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    var b = ip.GetAddressBytes();
                    if (b[0] == 10) return true;
                    if (b[0] == 192 && b[1] == 168) return true;
                }
                return false;
            })
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// JWT Authentication
var jwtKey = builder.Configuration["JwtSettings:SecretKey"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseCors("AllowReact");
//app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();