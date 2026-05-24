using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using SmartKnowledgeDesk.Data;
using SmartKnowledgeDesk.Models;
using SmartKnowledgeDesk.Plugins;
using SmartKnowledgeDesk.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var dataProtectionKeyPath = Path.Combine(
    builder.Environment.ContentRootPath,
    "App_Data",
    "DataProtectionKeys");
Directory.CreateDirectory(dataProtectionKeyPath);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeyPath));

builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));
        
builder.Services.Configure<AutomationSettings>(
    builder.Configuration.GetSection("Automation"));

builder.Services.AddHttpClient<AIService>();
builder.Services.AddScoped<GmailService>();
builder.Services.AddSingleton<IAutomationRunRecorder, FileAutomationRunRecorder>();
builder.Services.AddScoped<ITicketAutomationPlugin, HighPriorityRoutingPlugin>();
builder.Services.AddScoped<ITicketAutomationPlugin, AuditLogTicketPlugin>();
builder.Services.AddHostedService<EmailTicketIngestionService>();
builder.Services.AddHostedService<TicketTriageAutomationService>();
builder.Services.AddHostedService<StaleTicketEscalationService>();
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
