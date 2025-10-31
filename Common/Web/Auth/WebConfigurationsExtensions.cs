namespace Common.Web.Auth;

public static class WebConfigurationsExtensions
{
    //public static void UseDefaultSwagger(this WebApplication app, string? appUrl = null)
    //{
    //    app.UseSwagger(delegate (SwaggerOptions options)
    //    {
    //        if (!appUrl.IsNullOrEmpty())
    //        {
    //            options.PreSerializeFilters.Add(delegate (OpenApiDocument swagger, HttpRequest req)
    //            {
    //                swagger.Servers = new List<OpenApiServer>
    //                {
    //                    new OpenApiServer
    //                    {
    //                        Description = app.Environment.EnvironmentName + " server",
    //                        Url = appUrl
    //                    }
    //                };
    //            });
    //        }
    //    });
    //    app.UseSwaggerUI(delegate (SwaggerUIOptions options)
    //    {
    //        options.ConfigObject.AdditionalItems.Add("persistAuthorization", true);
    //    });
    //}

    //public static void ConfigureJwtAuthentication(this IServiceCollection serviceCollection, JwtAuthConfiguration configuration)
    //{
    //    serviceCollection.AddAuthentication(delegate (AuthenticationOptions options)
    //    {
    //        options.DefaultScheme = "Bearer";
    //        options.DefaultAuthenticateScheme = "Bearer";
    //        options.DefaultChallengeScheme = "Bearer";
    //    }).AddJwtBearer(delegate (JwtBearerOptions options)
    //    {
    //        JwtAuthConfiguration jwtAuthConfiguration = configuration;
    //        string issuer = jwtAuthConfiguration.Issuer;
    //        string audience = jwtAuthConfiguration.Audience;
    //        string secretKey = jwtAuthConfiguration.SecretKey;
    //        options.TokenValidationParameters = new TokenValidationParameters();
    //        if (!issuer.IsNullOrEmpty())
    //        {
    //            options.TokenValidationParameters.ValidateIssuer = true;
    //            options.TokenValidationParameters.ValidIssuer = issuer;
    //        }
    //        else
    //        {
    //            options.TokenValidationParameters.ValidateIssuer = false;
    //        }

    //        if (!audience.IsNullOrEmpty())
    //        {
    //            options.TokenValidationParameters.ValidateAudience = true;
    //            options.TokenValidationParameters.ValidAudience = audience;
    //        }
    //        else
    //        {
    //            options.TokenValidationParameters.ValidateAudience = false;
    //        }

    //        options.TokenValidationParameters.ValidateIssuerSigningKey = true;
    //        options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
    //        options.TokenValidationParameters.ValidateLifetime = true;
    //        options.TokenValidationParameters.ClockSkew = TimeSpan.Zero;
    //    });
    //    serviceCollection.Configure(delegate (SwaggerGenOptions options)
    //    {
    //        OpenApiSecurityScheme openApiSecurityScheme = new OpenApiSecurityScheme
    //        {
    //            Type = SecuritySchemeType.ApiKey,
    //            Description = "JWT Authorization header using the Bearer scheme.",
    //            Name = "Authorization",
    //            In = ParameterLocation.Header,
    //            Scheme = "Bearer",
    //            Reference = new OpenApiReference
    //            {
    //                Type = ReferenceType.SecurityScheme,
    //                Id = "Bearer"
    //            },
    //            BearerFormat = "JWT"
    //        };
    //        options.SwaggerGeneratorOptions.SecuritySchemes.Add(new KeyValuePair<string, OpenApiSecurityScheme>("Bearer", openApiSecurityScheme));
    //        options.SwaggerGeneratorOptions.SecurityRequirements.Add(new OpenApiSecurityRequirement {
    //        {
    //            openApiSecurityScheme,
    //            new List<string>()
    //        } });
    //    });
    //}
}