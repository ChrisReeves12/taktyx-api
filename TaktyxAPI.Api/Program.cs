using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NetTopologySuite;
using TaktyxAPI.Data.Data;
using TaktyxAPI.DTO;
using TaktyxAPI.Service;
using TaktyxAPI.Service.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Configure Entity Framework
builder.Services.AddDbContext<TaktyxDbContext>(
    options => options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        x => x.UseNetTopologySuite())
);

builder.Services.AddControllers();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IPasswordService, BCryptPasswordService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Configure HttpClient with connection pooling
builder.Services.AddHttpClient<IRouteLocationService, GoogleRouteLocationService>(client =>
{
    // Configure default headers, timeout, etc. if needed
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var maxConnections = builder.Configuration.GetValue<int>("HttpClientMaxConnections", 100);

    return new HttpClientHandler
    {
        MaxConnectionsPerServer = maxConnections,
        AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
        UseProxy = false,
        UseCookies = false
    };
});

// Mail service (Mailgun)
builder.Services.AddHttpClient<IMailService, MailGunService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var maxConnections = builder.Configuration.GetValue<int>("HttpClientMaxConnections", 100);

    return new HttpClientHandler
    {
        MaxConnectionsPerServer = maxConnections,
        AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
        UseProxy = false,
        UseCookies = false
    };
});

var jwtSettings = new JwtSettingsDto();
builder.Configuration.GetSection("JwtSettings").Bind(jwtSettings);
builder.Services.AddScoped<IAuthTokenService>(_ => new JwtTokenService(jwtSettings));

// Add Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("AdminOrModerator", policy => policy.RequireRole("Admin", "Moderator"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();