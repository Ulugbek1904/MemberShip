using Common.Web.Filters;
using Common.Web.Middlewares;
using Infrastructure.Brokers.Email.Extensions;
using Infrastructure.Brokers.FileService.Extensions;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using WebCore.SwaggerFilters;

namespace WebCore.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static void ConfigureDefaults(this WebApplicationBuilder builder, string appName = "DefaultAppName")
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        builder
            .ConfigureKestrel()
            .ConfigureHostConfigurations(appName)
            .ConfigureBrokers()
            .ConfigureSwagger(appName)
            .ConfigureControllers()
            .ConfigureCors()
            .ConfigureGlobalExceptionHandler()
            .ConfigureAuth()
            //.ConfigureConverter()
            .ConfigureHealthCheck();
        //.AddServices(appName)
        //.ConfigureOpenTelemetry(appName)
        //.ConfigureLogger(appName, builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
    }

    public static void ConfiguredDbContext(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContextPool<AppDbContext>(optionsBuilder =>
        {
            var dataSourceBuilder =
                new Npgsql.NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("DefaultConnectionString"))
                    .EnableDynamicJson();

            optionsBuilder
                .UseNpgsql(
                    dataSourceBuilder.Build(),
                    options => { }).UseSnakeCaseNamingConvention();
        });
    }

    private static WebApplicationBuilder ConfigureKestrel(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<KestrelServerOptions>(options => { options.Limits.MaxRequestBodySize = null; });

        return builder;
    }

    private static WebApplicationBuilder ConfigureHostConfigurations(this WebApplicationBuilder builder, string appName)
    {
        builder.Configuration.AddJsonFile(
            Path.Join(AppContext.BaseDirectory, $"appsettings.{builder.Environment.EnvironmentName}.json"),
            optional: false);

        builder.Configuration.AddJsonFile(
            Path.Join(AppContext.BaseDirectory, $"appsettings.json"),
            optional: false);

        builder.Configuration.AddIniFile(
            $"{appName}.ini",
            optional: true);

        builder.Configuration.AddEnvironmentVariables("APP");

        return builder;
    }

    private static WebApplicationBuilder ConfigureGlobalExceptionHandler(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<GlobalExceptionHandlerMiddleware>();
        return builder;
    }

    private static WebApplicationBuilder ConfigureBrokers(this WebApplicationBuilder builder)
    {
        //builder.ConfigureNotify();

        builder.Services
            .AddFileBroker()
            .AddMailBroker();
        //.AddDocumentGeneratorBroker()
        //.AddOneSignal()

        return builder;
    }

    private static WebApplicationBuilder ConfigureSwagger(this WebApplicationBuilder builder, string appName)
    {
        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1",
                new OpenApiInfo()
                {
                    Title = appName,
                    Version = "v1"
                });

            var xmlFile = $"{Assembly.GetEntryAssembly()!.GetName().Name}.xml";
            var xmlPath = Path.Join(AppContext.BaseDirectory, xmlFile);

            options.CustomSchemaIds(type => type.FullName);
            options.IncludeXmlComments(xmlPath);
            options.OperationFilter<MlfHeaderFilter>();
            options.OperationFilter<PermissionFilter>();

            OpenApiSecurityScheme securityScheme = new()
            {
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Description = "Header based Authentication",
                Name = "Jwt",
                Scheme = JwtBearerDefaults.AuthenticationScheme,
                Reference = new OpenApiReference()
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme
                }
            };

            //options.AddSecurityDefinition("Bearer", securityScheme);

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    securityScheme, new List<string>
                    {
                        "Bearer"
                    }
                }
            });
        });

        builder.Services.Configure<ApiBehaviorOptions>(options => { options.SuppressModelStateInvalidFilter = true; });
        builder.Services.AddCookiePolicy(options => { options.Secure = CookieSecurePolicy.Always; });

        return builder;
    }

    private static WebApplicationBuilder ConfigureControllers(this WebApplicationBuilder builder)
    {
        IHttpContextAccessor httpContextAccessor = new HttpContextAccessor();

        builder.Services.AddSingleton(httpContextAccessor);

        builder.Services.AddControllers(options =>
        {
            options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
            options.Filters.Add<ModelValidationFilter>();
        }).AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
            //options.JsonSerializerOptions.Converters.Add(new MultiLanguageFieldConverter(httpContextAccessor));
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        });


        return builder;
    }

    private static WebApplicationBuilder ConfigureCors(this WebApplicationBuilder builder)
    {
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowedOrigins",
                builder =>
                {
                    builder
                        .SetIsOriginAllowed(_ => true)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
        });

        return builder;
    }

    private static WebApplicationBuilder ConfigureAuth(this WebApplicationBuilder builder)
    {
        var authSection = builder.Configuration.GetRequiredSection("Auth");
        //builder.Services.ConfigureJwtAuthentication(new JwtAuthConfiguration()
        //{
        //    Issuer = authSection.GetValueOrThrowsNotFound("Issuer")!,
        //    Audience = authSection.GetValueOrThrowsNotFound("Audience")!,
        //    SecretKey = authSection.GetValueOrThrowsNotFound("SecretKey")!,
        //    ExpireTimeInMinutes = authSection.GetValueOrThrowsNotFound<int>("ExpireInMinutes")
        //});

        return builder;
    }

    //private static WebApplicationBuilder ConfigureConverter(this WebApplicationBuilder builder)
    //{
    //    var config = builder.Configuration.GetSection("DocxToPdfConfig")
    //        .Get<DocxToPdfConfig>()!;

    //    builder.AddDocConverter(config);

    //    builder.Services
    //        .AddOptions<AppConfig>()
    //        .BindConfiguration("AppConfig")
    //        .ValidateOnStart();

    //    builder.Services.AddOptions<BRBEImzoConfig>().BindConfiguration("BRBEImzoConfig").ValidateOnStart();

    //    return builder;
    //}

    private static WebApplicationBuilder ConfigureHealthCheck(this WebApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks();
        return builder;
    }

    private static WebApplicationBuilder AddServices(this WebApplicationBuilder builder, string projectName)
    {
        builder.Services.AddMemoryCache()
            .AddHttpClient()
            .AddHttpContextAccessor()
            .AddRateLimiter();
        //.AddNetworkLoggerService(projectName);

        return builder;
    }

    //private static void ConfigureLogger(this WebApplicationBuilder builder, string appName, string? collectorUrl = null)
    //{
    //    var loggerConfiguration = new LoggerConfiguration()
    //        .MinimumLevel.ControlledBy(new LoggingLevelSwitch())
    //        .Enrich.FromLogContext()
    //        .WriteTo
    //        .Console(LogEventLevel.Debug)
    //        .WriteTo
    //        .File("logs/log-.log", rollingInterval: RollingInterval.Day);

    //    loggerConfiguration.WriteTo.OpenTelemetry(options =>
    //    {
    //        options.Endpoint = collectorUrl;
    //        options.ResourceAttributes = ResourceBuilder
    //            .CreateDefault()
    //            .AddEnvironmentVariableDetector()
    //            .AddService(serviceName: appName, serviceNamespace: "BRBBusiness")
    //            .AddAttributes([
    //                new KeyValuePair<string, object>("deployment.environment", builder.Environment.EnvironmentName),
    //                new KeyValuePair<string, object>("resource_deployment_environment",
    //                    builder.Environment.EnvironmentName),
    //                new KeyValuePair<string, object>("resource_k8s_cluster_name", appName),
    //                new KeyValuePair<string, object>("resource_k8s_cluster_namespace", "BRBBusiness"),
    //            ])
    //            .Build().Attributes.ToDictionary();
    //    });

    //    Log.Information("Project started at {0} with PID: {1}",
    //        DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss"),
    //        Environment.ProcessId);


    //    Log.Logger = loggerConfiguration.CreateLogger();

    //    builder.Logging.ClearProviders();
    //    builder.Logging.AddSerilog();
    //}

    private static IServiceCollection AddRateLimiter(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                Log.Information(
                    "X-Forwarded-For: {0} Remote IP: {1}",
                    httpContext.Request.Headers["X-Forwarded-For"].ToString(),
                    httpContext.Connection.RemoteIpAddress?.ToString());
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Request.Headers["X-Forwarded-For"].Any()
                        ? httpContext.Request.Headers["X-Forwarded-For"].ToString()
                        : httpContext.Connection.RemoteIpAddress?.ToString()!,
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 1000,
                        QueueLimit = 0,
                        Window = TimeSpan.FromHours(1)
                    });
            });
        });

        return serviceCollection;
    }

    //public static WebApplicationBuilder ConfigureOpenTelemetry(this WebApplicationBuilder builder, string serviceName)
    //{
    //    builder.Services
    //        //.AddOpenTelemetry()
    //        .ConfigureResource(resource =>
    //            resource
    //                .AddService(serviceName, serviceNamespace: "BRBBusiness", serviceVersion: "1.0.0")
    //                .AddTelemetrySdk()
    //                .AddEnvironmentVariableDetector()
    //                .AddAttributes(new List<KeyValuePair<string, object>>()
    //                {
    //                    new("deployment.environment", builder.Environment.EnvironmentName),
    //                    new("host.name", Environment.MachineName),
    //                    new("resource_deployment_environment", builder.Environment.EnvironmentName),
    //                    new("resource_k8s_cluster_name", serviceName),
    //                    new("resource_k8s_cluster_namespace", "BRBBusiness"),
    //                })
    //        )
    //        .WithMetrics(providerBuilder =>
    //        {
    //            providerBuilder
    //                .AddHttpClientInstrumentation()
    //                .AddOtlpExporter();
    //        })
    //        .WithTracing(tracing =>
    //        {
    //            tracing
    //                .AddAspNetCoreInstrumentation(options =>
    //                {
    //                    options.RecordException = true;
    //                    options.EnableAspNetCoreSignalRSupport = true;
    //                    options.EnrichWithHttpRequest = (activity, request) =>
    //                    {
    //                        activity.SetTag("http.request.host", request.Host.Host);
    //                        activity.SetTag("http.request.remoteIpAddress",
    //                            request.HttpContext.Connection.RemoteIpAddress);
    //                        activity.SetTag("http.request.remotePort", request.HttpContext.Connection.RemotePort);
    //                    };
    //                    options.Filter = context => !context.Request.Path.Value?.StartsWith("/health") ?? true;
    //                })
    //                .AddHttpClientInstrumentation(options =>
    //                {
    //                    options.RecordException = true;
    //                    options.EnrichWithHttpRequestMessage = (activity, message) =>
    //                    {
    //                        activity.SetTag("net.peer.name", message.RequestUri?.Host);
    //                        activity.SetTag("http.url", message.RequestUri?.ToString());
    //                        activity.SetTag("http.target", message.RequestUri?.AbsolutePath);
    //                    };
    //                })
    //                .AddEntityFrameworkCoreInstrumentation(options =>
    //                {
    //                    options.SetDbStatementForStoredProcedure = true;
    //                    options.SetDbStatementForText = true;
    //                })
    //                .AddHangfireInstrumentation(options => { options.RecordException = true; })
    //                .AddGrpcClientInstrumentation()
    //                .AddNpgsql()
    //                .SetErrorStatusOnException()
    //                .SetSampler(new AlwaysOnSampler())
    //                .AddOtlpExporter();
    //        });

    //    return builder;
    //}
}
