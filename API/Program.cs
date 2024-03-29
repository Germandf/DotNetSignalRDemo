using API.Hubs;
using API.Models;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();
builder.Services
    .AddSignalR()
    .AddStackExchangeRedis(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString(ConnectionStrings.Redis)!;
        options.Configuration = ConfigurationOptions.Parse(connectionString);
        options.Configuration.ChannelPrefix = RedisChannel.Literal("DotNetSignalRDemo");
    });

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.UseCors(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
app.MapHub<ChatHub>("/chatHub");

app.Run();

public partial class Program { }
