using Infrastructure.Repository;
using API.Middleware;
using Infrastructure.AppDbContext;
using Microsoft.EntityFrameworkCore;
using Application.DI;


var builder = WebApplication.CreateBuilder(args);

// Validate JWT configuration early to fail fast if missing
var configuredJwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(configuredJwtKey))
{
    throw new InvalidOperationException("Missing JWT configuration: 'Jwt:Key' must be set. Set it in appsettings.json, appsettings.Development.json, dotnet user-secrets, or as an environment variable named JWT__Key.");
}

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();

builder.Services.AddRepositories();
builder.Services.AddApplicationServices();



// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

builder.Services.AddHttpContextAccessor();


var app = builder.Build();

// Nối middleware xử lý lỗi toàn cục 
app.UseMiddleware<GlobalExceptionMiddleware>();

// kích hoạt trigger ở trong db



   



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

// kích hoạt trigger ở trong db
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    Console.WriteLine("Dang ket noi database: " + db.Database.GetConnectionString());

    Console.WriteLine("Dang chay migration...");
    await db.Database.MigrateAsync();

    Console.WriteLine("Dang tao trigger...");
    await db.EnsureDatabaseGuardsCreatedAsync();

    Console.WriteLine("Da tao trigger xong.");
}
app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

