using API.Middleware;
using Application.DI;
using Infrastructure.AppDbContext;
using Infrastructure.Repository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;


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

//configuration default schema for authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})

//check JWT bearer token
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
        RoleClaimType = System.Security.Claims.ClaimTypes.Role
    };
});




// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });

    // 1. Cấu hình khung nhập Token (Giữ nguyên)
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập Token theo định dạng: Bearer {your_token}"
    };

    options.AddSecurityDefinition("Bearer", securityScheme);

    // 2. SỬA Ở ĐÂY: Thêm "document =>" để biến nó thành một Func như trình biên dịch yêu cầu
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer"),
            new List<string>()
        }
    });
});

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    //app.MapOpenApi();
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

