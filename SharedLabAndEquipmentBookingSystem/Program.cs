using API;
using API.Common.Middleware;
using Application.DI;
using Infrastructure.AppDbContext;
using Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

string connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Thiếu ConnectionStrings:DefaultConnection.");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Thiếu ConnectionStrings:DefaultConnection.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddRepositories();
builder.Services.AddApplicationServices();
builder.Services.AddApiServices(
    builder.Configuration,
    builder.Environment);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        policy => policy.AllowAnyOrigin()//WithOrigins("http://localhost:4200") // Cổng FE của bạn
                        .AllowAnyMethod() // Cho phép GET, POST, PUT, DELETE
                        .AllowAnyHeader()); // Cho phép gửi Token
});

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "Shared Lab API v1");
        options.DisplayRequestDuration();
    });
}

if (builder.Configuration.GetValue<bool>(
        "Database:ApplyMigrationsOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();

    await db.Database.MigrateAsync();
    await db.EnsureDatabaseGuardsCreatedAsync();
}

app.UseHttpsRedirection();
app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
