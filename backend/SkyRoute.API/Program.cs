var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRouting();

var app = builder.Build();

app.UseRouting();

app.MapGet("/", () => Results.Ok("SkyRoute API is running."));

app.Run();

public partial class Program;
