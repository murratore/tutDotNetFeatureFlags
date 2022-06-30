using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddFeatureManagement(builder.Configuration.GetSection("FeatureFlags"))
        .AddFeatureFilter<PercentageFilter>()
        .AddFeatureFilter<TimeWindowFilter>()
        .AddFeatureFilter<MyCustomFeatureFilter>()
        .AddFeatureFilter<MyCustomContextFeatureFilter>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", async (IFeatureManager manager, [FromHeader(Name = "X-Lucky-Number")] int? inputNumber) =>
{
    //if (!await manager.IsEnabledAsync("WeatherForecastLuckyNumber"))   //WeatherForecastTimeWindow, WeatherForecastPercentage
    if (!await manager.IsEnabledAsync("WeatherForecastLuckyNumber", new MyCustomFilterContext { InputNumber = inputNumber ?? 0 }))   //WeatherForecastTimeWindow, WeatherForecastPercentage
    {        
        return Results.Content("not found");
    }

    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateTime.Now.AddDays(index),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    
    return Results.Ok(forecast);
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateTime Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}


[FilterAlias(nameof(MyCustomFeatureFilter))]
public class MyCustomFeatureFilter : IFeatureFilter
{
    public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
    {
         var settings = context.Parameters.Get<MyCustomFeatureFilterSettings>()
            ?? throw new ArgumentNullException(nameof(MyCustomFeatureFilterSettings));
        return Task.FromResult(settings.LuckyNumber == 47);
    }
}

public class MyCustomFeatureFilterSettings
{
    public int LuckyNumber { get; set; }
}

[FilterAlias(nameof(MyCustomContextFeatureFilter))]
public class MyCustomContextFeatureFilter : IContextualFeatureFilter<MyCustomFilterContext>
{
    public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext featureFilterContext, MyCustomFilterContext appContext)
    {
        var settings = featureFilterContext.Parameters.Get<MyCustomFeatureFilterSettings>()
            ?? throw new ArgumentNullException(nameof(MyCustomFeatureFilterSettings));
        return Task.FromResult(settings.LuckyNumber == appContext.InputNumber);
    }
}

public class MyCustomFilterContext
{
    public int InputNumber { get; set; }
}