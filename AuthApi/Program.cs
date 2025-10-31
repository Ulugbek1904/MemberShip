using WebCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.WebHost.ConfigureKestrel(x => x.ListenAnyIP(8081));
}
//builder.Services.AddStyxClient("http://172.21.180.14");
builder.ConfigureDefaults("AuthApi");
builder.ConfiguredDbContext();
//builder.Services.ConfigureServicesFromTypeAssembly<IAuthService>();
//builder.Services.ConfigureServicesFromTypeAssembly<IMailBroker>();

var app = builder.Build();

#if !DEBUG
     app.ConfigureDefaults("/api-auth");
#else
//app.ConfigureDefaults("/");
#endif

app.MapControllers();

app.Run();
