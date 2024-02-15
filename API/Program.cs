using API.Hubs;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();
builder.Services
    .AddSignalR()
    .AddStackExchangeRedis("localhost:6379", options => {
        options.Configuration.ChannelPrefix = RedisChannel.Literal("DotNetSignalRDemo");
        options.Configuration.AbortOnConnectFail = false;
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
