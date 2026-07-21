using API.Common.BackgroundServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Security.Claims;
using System.Text;

namespace API
{
    public static class DependencyInjection
    {
        public const string CorsPolicyName = "ConfiguredCors";

        public static IServiceCollection AddApiServices(
            this IServiceCollection services,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            string jwtKey = GetRequiredConfiguration(
                configuration,
                "Jwt:Key");

            string jwtIssuer = GetRequiredConfiguration(
                configuration,
                "Jwt:Issuer");

            string jwtAudience = GetRequiredConfiguration(
                configuration,
                "Jwt:Audience");

            services.AddControllers();
            services.AddHttpContextAccessor();

            services
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
                    options.RequireHttpsMetadata =
                        !environment.IsDevelopment();
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

            services.AddAuthorization();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
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
                        Description =
                            "Dán access token, không nhập chữ Bearer."
                    });

                options.AddSecurityRequirement(
                    document => new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecuritySchemeReference(
                                "Bearer",
                                document),
                            new List<string>()
                        }
                    });
            });

            AddConfiguredCors(
                services,
                configuration,
                environment);

            services.AddHostedService<
                BookingReminderBackgroundService>();
            services.AddHostedService<
                BookingLifecycleBackgroundService>();
            services.AddHostedService<
                WaitlistExpiryBackgroundService>();
            services.AddHostedService<
                RecurringMaintenanceBackgroundService>();

            return services;
        }

        private static void AddConfiguredCors(
            IServiceCollection services,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            string[] allowedOrigins = configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>()
                ?? Array.Empty<string>();

            services.AddCors(options =>
            {
                options.AddPolicy(
                    CorsPolicyName,
                    policy =>
                    {
                        if (environment.IsDevelopment()
                            && allowedOrigins.Length == 0)
                        {
                            policy
                                .AllowAnyOrigin()
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                            return;
                        }

                        if (allowedOrigins.Length == 0)
                        {
                            throw new InvalidOperationException(
                                "Phải cấu hình Cors:AllowedOrigins "
                                + "khi không chạy Development.");
                        }

                        policy
                            .WithOrigins(allowedOrigins)
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                    });
            });
        }

        private static string GetRequiredConfiguration(
            IConfiguration configuration,
            string key)
        {
            string? value = configuration[key];

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    $"Thiếu cấu hình {key}.");
            }

            return value;
        }
    }
}
