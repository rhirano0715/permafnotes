using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using permafnotes;
using PermafnotesDomain.Services;
using PermafnotesRepositoryByFile;
using Microsoft.Graph;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
    options.ProviderOptions.DefaultAccessTokenScopes.Add("openid");
    options.ProviderOptions.DefaultAccessTokenScopes.Add("offline_access");
});

builder.Services.AddGraphClient("https://graph.microsoft.com/User.Read", "https://graph.microsoft.com/Files.ReadWrite");

#if DEBUG
builder.Logging.SetMinimumLevel(LogLevel.Trace);
#else
builder.Logging.SetMinimumLevel(LogLevel.Information);
#endif

builder.Services.AddAntDesign();

builder.Services.AddScoped<NoteService>();
builder.Services.AddScoped<IPermafnotesRepository, Repositoy>(provider => 
{
    var client = provider.GetRequiredService<GraphServiceClient>();
    var logger = provider.GetRequiredService<ILogger<NoteService>>();
    return Repositoy.CreateRepositoryUsingMsGraph(client, logger);
});

await builder.Build().RunAsync();
