using Logto.AspNetCore.Authentication;
using Lucidly.UI.Components;
using Lucidly.UI.Utils;
using Magic.IndexedDb;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<OAuthFlowService>();
builder.Services.AddScoped<OAuthHandler>();
builder.Services.AddLogtoAuthentication(options =>
{
    options.Scopes = [
         LogtoParameters.Scopes.Phone,
         LogtoParameters.Scopes.CustomData,
        LogtoParameters.Scopes.Identities,
        LogtoParameters.Scopes.Email
        ];
    options.CallbackPath = "/oidc-callback";
    options.Endpoint = builder.Configuration["Logto:Endpoint"]!;
    options.AppId = builder.Configuration["Logto:AppId"]!;
    options.AppSecret = builder.Configuration["Logto:AppSecret"];
    options.GetClaimsFromUserInfoEndpoint = true;
    options.Resource = builder.Configuration["Logto:Resource"];
});
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddDataProtection()
    .SetApplicationName("LucidlyUi") ;

builder.Services.AddMagicBlazorDB(BlazorInteropMode.SignalR, builder.Environment.IsDevelopment());



builder.Services.AddSignalR(e =>
{
    e.EnableDetailedErrors = true;
    e.MaximumReceiveMessageSize = 102400000;
});
builder.Services.AddHttpClient();
// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddAntDesign();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.MapStaticAssets();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();


app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

 