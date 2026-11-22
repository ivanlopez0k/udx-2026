using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using UDX.Server.Client.Services;


var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped(sp =>
    new HttpClient
    {
        BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
    });

builder.Services.AddScoped<ProfilePhotoStateService>();



await builder.Build().RunAsync();
