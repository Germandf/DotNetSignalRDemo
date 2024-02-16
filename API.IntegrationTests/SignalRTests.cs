using API.Models;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;

namespace API.IntegrationTests;

public class SignalRTests(WebApplicationFixture factory) : WebApplicationTestsBase(factory)
{
    [Fact]
    public async Task SignalRHubE2ETest()
    {
        var client = _factory.CreateClient();
        var url = new Uri(client.BaseAddress!, "chatHub");
        var hubConnection = new HubConnectionBuilder()
            .WithUrl(url, o => o.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler())
            .Build();
        await hubConnection.StartAsync();

        var messageReceived = new TaskCompletionSource<(string, string)>();
        hubConnection.On<string, string>("ReceiveMessage", (user, message) => messageReceived.SetResult((user, message)));
        await hubConnection.InvokeAsync("SendMessage", "user", "Test message");

        var message = await messageReceived.Task;
        message.Item1.Should().Be("user");
        message.Item2.Should().Be("Test message");
        await hubConnection.StopAsync();
    }
}

[CollectionDefinition(nameof(WebApplicationFixture), DisableParallelization = true)]
public class WebApplicationCollection : ICollectionFixture<WebApplicationFixture>
{
}

[Collection(nameof(WebApplicationFixture))]
public abstract class WebApplicationTestsBase
{
    protected readonly IFixture _fixture;
    protected readonly WebApplicationFixture _factory;
    protected readonly HttpClient _httpClient;

    protected WebApplicationTestsBase(WebApplicationFixture factory)
    {
        _fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        _factory = factory;
        _httpClient = factory.CreateClient();
    }
}

public class WebApplicationFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly IContainer _redisContainer = new ContainerBuilder()
        .WithImage("redis:7.2.4")
        .WithPortBinding(6379, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(6379))
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            var redisConnectionString = $"{_redisContainer.Hostname}:{_redisContainer.GetMappedPublicPort(6379)}";
            var integrationTestConfig = new Dictionary<string, string?>
            {
                {$"{nameof(ConnectionStrings)}:{nameof(ConnectionStrings.Redis)}", redisConnectionString}
            };
            configBuilder.AddInMemoryCollection(integrationTestConfig);
        });
    }

    public async Task InitializeAsync()
    {
        await _redisContainer.StartAsync();
    }

    public async new Task DisposeAsync()
    {
        await _redisContainer.DisposeAsync();
    }
}
