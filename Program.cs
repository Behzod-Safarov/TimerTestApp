using ActualLab.Fusion;
using ActualLab.Fusion.Blazor;
using ActualLab.Fusion.Extensions;
using ActualLab.Fusion.Server;
using ActualLab.Fusion.Server.Middlewares;
using Microsoft.EntityFrameworkCore;
using TimerTestApp.Components.Pages;
using TimerTestApp.Data;
using TimerTestApp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddFusion()
                .AddFusionTime()
                .AddService<ITimerService, TimerService>()
                .AddBlazor();

builder.Services.AddSingleton(_ => SessionMiddleware.Options.Default);
builder.Services.AddScoped(c => new SessionMiddleware(c.GetRequiredService<SessionMiddleware.Options>(), c));

builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();


var app = builder.Build();


app.UseExceptionHandler("/Error", createScopeForErrors: true);

app.UseFusionSession();
app.UseAntiforgery();
app.UseStaticFiles();
app.MapRazorComponents<_HostPage>()
    .AddInteractiveServerRenderMode();

app.Run();
