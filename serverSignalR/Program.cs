using Server.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Server.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<GameServices>();
builder.Services.AddSignalR();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();

app.MapControllers();
app.MapHub<GameHub>("/game");

Console.Clear();
app.Run();
Console.Clear();
