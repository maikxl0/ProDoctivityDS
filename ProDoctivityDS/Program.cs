using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi;
using ProDoctivityDS.Application;
using ProDoctivityDS.Persistence;
using ProDoctivityDS.Shared;
using ProDoctivityDS.Persistence.Seeds;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddPersistenceDependencies(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddSharedServices();

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:4200" };
        
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .WithExposedHeaders("X-Session-Id");
    });
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// Configurar Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Prodoctivity Processor API",
        Version = "v1",
        Description = "API para procesamiento inteligente de documentos PDF en Productivity Cloud"
    });

    // Definir el header X-Session-Id
    c.AddSecurityDefinition("SessionId", new OpenApiSecurityScheme
    {
        Name = "X-Session-Id",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "Identificador de sesión. Si no se envía, el backend generará uno nuevo."
    });

    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        // Aquí usamos OpenApiSecuritySchemeReference en lugar de Reference
        [new OpenApiSecuritySchemeReference("SessionId", document)] = new List<string>()
    });
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var appDataPath = builder.Configuration["AppData:BasePath"];
if (string.IsNullOrWhiteSpace(appDataPath))
{
    appDataPath = Environment.GetEnvironmentVariable("APP_DATA_DIR");
}

if (string.IsNullOrWhiteSpace(appDataPath))
{
    appDataPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
}
else if (!Path.IsPathRooted(appDataPath))
{
    appDataPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, appDataPath));
}

Directory.CreateDirectory(appDataPath);

var keyRingPath = builder.Configuration["Persistence:DataProtectionKeysPath"];
if (string.IsNullOrWhiteSpace(keyRingPath))
{
    keyRingPath = Path.Combine(appDataPath, "DataProtectionKeys");
}
else if (!Path.IsPathRooted(keyRingPath))
{
    keyRingPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, keyRingPath));
}

Directory.CreateDirectory(keyRingPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keyRingPath))
    .SetApplicationName("ProDoctivityDS");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await StoredConfigurationSeeder.SeedDefaultConfigurationAsync(scope.ServiceProvider);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Prodoctivity Processor API v1");
        c.RoutePrefix = string.Empty; // Para que Swagger UI sea la página principal (opcional)
    });
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowFrontend");
app.UseSession();

app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).ExcludeFromDescription();
app.MapControllers();

app.Run();
