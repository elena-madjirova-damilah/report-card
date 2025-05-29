using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using WondeApiAggregator.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<WondeService>();

var app = builder.Build();

app.MapGet("/aggregate", async (WondeService service) =>
{
    var result = await service.AggregateAsync();
    return Results.Json(result);
});

app.Run();
