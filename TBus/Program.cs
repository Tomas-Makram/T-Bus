using Asp.Versioning;
using BusinessLayer.Filters;
using BusinessLayer.Functions;
using BusinessLayer.Services;
using DataLayer.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using StackExchange.Redis;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace TBus
{
    public class Program
    {
        [SupportedOSPlatform("windows")]
        public static async Task Main(string[] args)
        {

            var builder = WebApplication.CreateBuilder(args);

            //var secureConfigPath = Path.Combine(builder.Environment.ContentRootPath, "appsettings.secure");

            //if (File.Exists(secureConfigPath))
            //{
            //    var decryptedJson =
            //        ConfigEncryptionHelper.DecryptFile(secureConfigPath);

            //    using var jsonStream =
            //        new MemoryStream(Encoding.UTF8.GetBytes(decryptedJson));

            //    builder.Configuration.AddJsonStream(jsonStream);
            //}

            //ConfigEncryptionHelper.EncryptFile("appsettings.json", "appsettings.secure");

            builder.Services.AddControllers();

            builder.Services
                .AddApiVersioning(options =>
                {
                    options.DefaultApiVersion = new ApiVersion(1, 0);
                    options.AssumeDefaultVersionWhenUnspecified = true;
                    options.ReportApiVersions = true;
                    options.ApiVersionReader = new UrlSegmentApiVersionReader();
                })
                .AddApiExplorer(options =>
                {
                    options.GroupNameFormat = "'v'V";
                    options.SubstituteApiVersionInUrl = true;
                });

            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "TBus API",
                    Version = "v1",
                    Description = "TBus Secure API - Version 1"
                });

                options.DocInclusionPredicate((docName, apiDesc) =>
                {
                    var versions = apiDesc.ActionDescriptor.EndpointMetadata
                        .OfType<ApiVersionAttribute>()
                        .SelectMany(attr => attr.Versions);

                    return versions.Any(v => $"v{v.MajorVersion}" == docName);
                });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Name = "Authorization",
                    Description = "Enter Bearer Token"
                });

                options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
                });

                options.OperationFilter<CsrfHeaderOperationFilter>();
            });

            builder.Services.AddDbContext<DBContext>(options =>
            {
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("Connection"),
                    sql => sql.MigrationsAssembly("TBus"));
            });

            //builder.Services.AddDbContext<DBContext>(options =>
            //{
            //    options.UseSqlite(builder.Configuration.GetConnectionString("Connection"),
            //            sql => sql.MigrationsAssembly("TBus"));
            //});

            var jwtSettings =
                builder.Configuration.GetSection("Jwt")
                    .Get<JwtSettings>() ?? new JwtSettings();

            if (string.IsNullOrWhiteSpace(jwtSettings.Key) ||
                Encoding.UTF8.GetByteCount(jwtSettings.Key) < 32)
            {
                throw new InvalidOperationException(
                    "Jwt:Key must be at least 32 bytes.");
            }

            builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme =
                        JwtBearerDefaults.AuthenticationScheme;

                    options.DefaultChallengeScheme =
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
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,

                            ValidIssuer = jwtSettings.Issuer,
                            ValidAudience = jwtSettings.Audience,

                            IssuerSigningKey =
                                new SymmetricSecurityKey(
                                    Encoding.UTF8.GetBytes(jwtSettings.Key)),

                            ClockSkew = TimeSpan.Zero
                        };

                    options.Events = new JwtBearerEvents
                    {
                        OnChallenge = async context =>
                        {
                            context.HandleResponse();

                            context.Response.StatusCode = 401;
                            context.Response.ContentType = "application/json";

                            var response = new
                            {
                                success = false,
                                message = "Unauthorized"
                            };

                            await context.Response.WriteAsync(
                                JsonSerializer.Serialize(response));
                        }
                    };
                });

            builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

            builder.Services.Configure<SessionSettings>(builder.Configuration.GetSection("SessionSettings"));

            builder.Services.AddAuthorization();

            builder.Services.AddScoped<IAuthenticateManager, AuthenticateManager>();
            builder.Services.AddScoped<IBusesManager, BusesManager>();
            builder.Services.AddScoped<IDriversManager, DriversManager>();
            builder.Services.AddScoped<ITripsManager, TripsManager>();
            builder.Services.AddScoped<IPaymentAlsoManager, PaymentAlsoManager>();

            builder.Services.AddScoped<RequireActiveLoginFilter>();

            var keysPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "Keys");
            Directory.CreateDirectory(keysPath);
            builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keysPath)).SetApplicationName("TBus");

            builder.Services.AddScoped<CairoTimeService>();

            builder.Services.AddSingleton<IDataCiphers, DataCiphers>();

            builder.Services.AddSingleton<IDataHasher, DataHasher>();

            builder.Services.AddScoped<ITokenSessionService, TokenSessionService>();

            builder.Services.AddAppRateLimiting(builder.Configuration);

            builder.Services.AddMemoryCache();

            var cacheSettings = builder.Configuration.GetSection("CacheSettings").Get<CacheSettings>() ?? new CacheSettings();

            if (cacheSettings.UseRedis)
            {
                var redisOptions = ConfigurationOptions.Parse(cacheSettings.RedisConnection);

                redisOptions.AbortOnConnectFail = false;

                var redis = ConnectionMultiplexer.Connect(redisOptions);

                builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

                builder.Services.AddSingleton<ISecurityCounterStore, RedisSecurityCounterStore>();
            }
            else
            {
                builder.Services.AddSingleton<ISecurityCounterStore, MemorySecurityCounterStore>();
            }

            builder.Services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
            });

            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("DefaultCorsPolicy", policy =>
                {
                    policy.WithOrigins(allowedOrigins ?? Array.Empty<string>()).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                });
            });

            builder.Services.AddAntiforgery(options =>
            {
                options.HeaderName = "X-CSRF-TOKEN";
            });

            builder.Services.AddDirectoryBrowser();

            builder.Services.AddHttpClient<FirebaseService>();
            builder.Services.AddHostedService<LicenseResetStartupService>();

            var app = builder.Build();

            app.UseSwagger();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "TBus API V1");


                    options.RoutePrefix = "swagger";
                });
            }

            app.UseHttpsRedirection();

            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.UseResponseCompression();

            // app.UseMiddleware<SecurityHeadersMiddleware>();

            app.UseStaticFiles();

            app.UseCors("DefaultCorsPolicy");

            app.UseRateLimiter();

            app.UseAuthentication();

            app.UseMiddleware<SessionActivityMiddleware>();

            app.UseMiddleware<DistributedUserThrottleMiddleware>();

            app.UseAuthorization();

            //app.UseMiddleware<LicenseCheckMiddleware>();

            app.MapControllers();

            app.MapGet("/", context =>
            {
                context.Response.Redirect("/pages/login.html");
                return Task.CompletedTask;
            });

            await AdminSeeder.SeedAsync(app.Services);

            //app.Lifetime.ApplicationStarted.Register(() =>
            //{
            //    try
            //    {
            //        var addressesFeature = app.Services
            //            .GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>()
            //            .Features
            //            .Get<IServerAddressesFeature>();

            //        var baseUrl = addressesFeature?
            //            .Addresses
            //            .FirstOrDefault(x => x.StartsWith("https://"))
            //            ?? addressesFeature?.Addresses.FirstOrDefault();

            //        if (string.IsNullOrWhiteSpace(baseUrl))
            //            return;

            //        var url = $"{baseUrl.TrimEnd('/')}/pages/login.html";

            //        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            //        {
            //            FileName = url,
            //            UseShellExecute = true
            //        });
            //    }
            //    catch
            //    {
            //    }
            //});

            await app.RunAsync();
        }
    }
}