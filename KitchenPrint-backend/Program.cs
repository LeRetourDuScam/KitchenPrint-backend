using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;
using System.Text;
using KitchenPrint.Contracts.DataAccess;
using KitchenPrint.API.Core.Configuration;
using KitchenPrint.API.Core.DataAccess;
using KitchenPrint.Core.Models;
using KitchenPrint.ENTITIES;

var builder = WebApplication.CreateBuilder(args);


builder.Configuration.Sources
    .OfType<Microsoft.Extensions.Configuration.Json.JsonConfigurationSource>()
    .ToList()
    .ForEach(s => s.ReloadOnChange = false);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

var corsOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>() 
    ?? new[] { "http://localhost:4200","https://kitchenprint-frontend.onrender.com" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("RestrictedCors", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
});

var cacheSizeLimitMB = builder.Configuration.GetValue<int>("Ai:CacheSizeLimitMB", 100);
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = cacheSizeLimitMB * 1024 * 1024; 
});

var globalRateConfig = builder.Configuration.GetSection("RateLimiting:Global");
var authRateConfig = builder.Configuration.GetSection("RateLimiting:Auth");

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                PermitLimit = globalRateConfig.GetValue<int>("PermitLimit", 100),
                Window = TimeSpan.FromMinutes(globalRateConfig.GetValue<int>("WindowMinutes", 1)),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            }
        )
    );

    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authRateConfig.GetValue<int>("PermitLimit", 5),
                Window = TimeSpan.FromMinutes(authRateConfig.GetValue<int>("WindowMinutes", 5)),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }
        )
    );

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        
        var errorResponse = new KitchenPrint.Core.Models.ErrorResponse(
            ErrorCodes.RATE_LIMIT_EXCEEDED,
            "Too many requests. Please wait before trying again.",
            retryAfter: 60
        );
        
        await context.HttpContext.Response.WriteAsJsonAsync(errorResponse, cancellationToken);
    };
});

builder.Services.Configure<AiSettings>(builder.Configuration.GetSection("Ai"));
builder.Services.Configure<AgribalyseSettings>(builder.Configuration.GetSection("Agribalyse"));

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? builder.Configuration["JwtSettings:SecretKey"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "FytAI",
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"] ?? "FytAI-Users",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

builder.Services.AddDbContext<kitchenPrintDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("PostgreSQL");
    options.UseNpgsql(connectionString);
});

// Authentication & User Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();


// Carbon Footprint Services - Ingredients
builder.Services.AddScoped<IIngredientRepository, IngredientRepository>();
builder.Services.AddScoped<IIngredientService, IngredientService>();
builder.Services.AddScoped<IExternalIngredientService, ExternalIngredientService>();

// Carbon Footprint Services - Recipes
builder.Services.AddScoped<IRecipeRepository, RecipeRepository>();
builder.Services.AddScoped<IRecipeService, RecipeService>();

// Alternatives & Nutrition Services
builder.Services.AddHttpClient<IAlternativesService, AlternativesService>();
builder.Services.AddHttpClient<IEcoChatService, EcoChatService>();
builder.Services.AddScoped<INutritionService, NutritionService>();

builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Seed database with ingredient data
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<kitchenPrintDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await dbContext.Database.MigrateAsync();
        var excelPath = Path.Combine(AppContext.BaseDirectory, "Resources", "agribalyse.xlsx");
        await IngredientDataSeeder.SeedAsync(dbContext, logger, excelPath);
    }    catch (Exception ex)
    {
        logger.LogWarning(ex, "KitchenPrint: Could not connect to database during startup. " +
            "The application will start but database features will be unavailable. " +
            "Check your network connection and ensure port 5432 is not blocked.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseResponseCompression();

app.UseCors("RestrictedCors");

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Content-Security-Policy", 
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:;");
    await next();
});

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
