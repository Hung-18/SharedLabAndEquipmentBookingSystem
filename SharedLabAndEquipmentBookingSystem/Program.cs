using API.BackgroundServices;
using API.Middleware;
using Application.DI;
using Infrastructure.AppDbContext;
using Infrastructure.Repository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1. KIỂM TRA CẤU HÌNH JWT
// ============================================================

string? jwtKey = builder.Configuration["Jwt:Key"];
string? jwtIssuer = builder.Configuration["Jwt:Issuer"];
string? jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "Thiếu cấu hình JWT: 'Jwt:Key'.");
}

if (string.IsNullOrWhiteSpace(jwtIssuer))
{
    throw new InvalidOperationException(
        "Thiếu cấu hình JWT: 'Jwt:Issuer'.");
}

if (string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException(
        "Thiếu cấu hình JWT: 'Jwt:Audience'.");
}

// ============================================================
// 2. DATABASE
// ============================================================

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "DefaultConnection"));
});

// ============================================================
// 3. CONTROLLER VÀ DEPENDENCY INJECTION
// ============================================================

builder.Services.AddControllers();

builder.Services.AddRepositories();
builder.Services.AddApplicationServices();

builder.Services.AddHttpContextAccessor();

// ============================================================
// 4. JWT AUTHENTICATION
// ============================================================

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true;
        options.SaveToken = true;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                ValidateAudience = true,
                ValidAudience = jwtAudience,

                ValidateLifetime = true,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)),

                ClockSkew = TimeSpan.Zero,
                NameClaimType = ClaimTypes.NameIdentifier,
                RoleClaimType = ClaimTypes.Role
            };
    });

builder.Services.AddAuthorization();

// ============================================================
// 5. SWAGGER
// ============================================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "Shared Lab And Equipment Booking API",
            Version = "v1"
        });

    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,

            // Vì Type là Http và Scheme là bearer,
            // Swagger sẽ tự thêm chữ "Bearer".
            Description =
                "Dán access token vào đây. "
                + "Không cần nhập chữ Bearer."
        });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecuritySchemeReference(
                    "Bearer",
                    document),
                new List<string>()
            }
        });
});

// ============================================================
// 6. CORS
// ============================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowAll",
        policy =>
        {
            policy
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

// ============================================================
// 7. BACKGROUND SERVICES
// ============================================================

builder.Services.AddHostedService<
    BookingReminderBackgroundService>();

var app = builder.Build();

// ============================================================
// 8. TỰ ĐỘNG MIGRATION VÀ TẠO DATABASE GUARD/TRIGGER
// ============================================================

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();

    Console.WriteLine(
        "Đang kết nối database: "
        + db.Database.GetConnectionString());

    Console.WriteLine("Đang chạy migration...");

    await db.Database.MigrateAsync();

    Console.WriteLine(
        "Đang tạo database guard/trigger...");

    await db.EnsureDatabaseGuardsCreatedAsync();

    Console.WriteLine(
        "Đã tạo database guard/trigger xong.");
}

// ============================================================
// 9. HTTP PIPELINE
// ============================================================

// Middleware lỗi nên đặt sớm để bắt lỗi của các middleware phía sau.
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

app.UseHttpsRedirection();

app.UseCors("AllowAll");

// Authentication phải đứng trước Authorization.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();