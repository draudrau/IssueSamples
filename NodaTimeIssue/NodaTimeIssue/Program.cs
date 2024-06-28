using Microsoft.EntityFrameworkCore;

using NodaTime;
using NodaTime.Serialization.SystemTextJson;

using System.Text.Json.Serialization;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
    options.SerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
});

string dbConnectionString = "Your postgres database connection string";

builder.Services.AddDbContext<DataDbContext>(options => options.UseNpgsql($"{dbConnectionString};ApplicationName=DataApp"));
builder.Services.AddDbContext<InstantDbContext>(options => options.UseNpgsql($"{dbConnectionString};ApplicationName=InstantApp", options => options.UseNodaTime()));
builder.Services.AddHealthChecks().AddNpgSql($"{dbConnectionString};ApplicationName=InstantApp", name: "db");

List<Data> GetData(IServiceProvider serviceProvider)
{
    var db = serviceProvider.GetRequiredService<DataDbContext>();
    return db.MyData.FromSql($"SELECT 1 Id, 'something' Text").ToList();
}

List<InstantData> GetInstantData(IServiceProvider serviceProvider)
{
    var db = serviceProvider.GetRequiredService<InstantDbContext>();
    return db.MyData.FromSql($"SELECT 1 Id, current_timestamp MyDate").ToList();
}

var app = builder.Build();
app.MapHealthChecks("/health");
var mydataApi = app.MapGroup("/data");

mydataApi.MapGet("/", () =>
{
    using var scope = app.Services.CreateScope();
    return GetData(scope.ServiceProvider);
});

mydataApi.MapGet("/instant", () =>
{
    using var scope = app.Services.CreateScope();
    return GetInstantData(scope.ServiceProvider);
});

app.Run();


[JsonSerializable(typeof(InstantData[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}

class DataDbContext(DbContextOptions<DataDbContext> options) : DbContext(options)
{
    public DbSet<Data> MyData { get; set; }
}

class InstantDbContext(DbContextOptions<InstantDbContext> options) : DbContext(options)
{
    public DbSet<InstantData> MyData { get; set; }
}

public class Data()
{
    public int Id { get; set; }
    public string Text { get; set; }
}

public class InstantData()
{
    public int Id { get; set; }
    public Instant MyDate { get; set; }
}
