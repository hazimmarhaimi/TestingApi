using log4net.Config;
using log4net;
using System.Reflection;
using TestingIV.Services;

var builder = WebApplication.CreateBuilder(args);

var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));


builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IPartnerService, PartnerService>();
builder.Services.AddScoped<ISignatureService, SignatureService>();
builder.Services.AddSingleton<ILog>(sp => LogManager.GetLogger(typeof(Program)));


var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
