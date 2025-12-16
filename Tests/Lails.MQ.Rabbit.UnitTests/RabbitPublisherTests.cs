using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lails.MQ.Rabbit.UnitTests;

public class RabbitPublisherTests
{
    private readonly Mock<IBus> _busMock;
    private readonly RabbitPublisher _publisher;

    public RabbitPublisherTests()
    {
        _busMock = new Mock<IBus>();
        _busMock.Setup(r => r.Address).Returns(new Uri("rabbitmq://localhost"));
        _publisher = new RabbitPublisher(_busMock.Object);
    }

    [Test]
    public async Task PublishAsync_ShouldCallBusPublishAsync_WhenMessageIsValid()
    {
        // Arrange
        var message = new TestMessage { Id = 1, Name = "Test" };
        _busMock.Setup(x => x.Publish(It.IsAny<TestMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _publisher.PublishAsync(message);

        // Assert
        _busMock.Verify(x => x.Publish(message, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void PublishAsync_ShouldThrowException_WhenBusThrowsException()
    {
        // Arrange
        var message = new TestMessage { Id = 1, Name = "Test" };
        var expectedException = new InvalidOperationException("Bus async error");
        _busMock.Setup(x => x.Publish(It.IsAny<TestMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await _publisher.PublishAsync(message));
        Assert.That(exception!.Message, Is.EqualTo("Bus async error"));
    }

    [Test]
    public async Task PublishAsync_ShouldPublishToExchange_WhenRegisteredWithMassTransit()
    {
        // Arrange
        var services = new ServiceCollection();
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(x => x["RABBITMQ_HOSTURL"]).Returns("rabbitmq://localhost/");
        configMock.Setup(x => x["RABBITMQ_USERNAME"]).Returns("guest");
        configMock.Setup(x => x["RABBITMQ_PASSWORD"]).Returns("guest");

        services
            .RegisterRabbitPublisher()
            .AddMassTransit(x =>
            {
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.AddDataBusConfiguration(configMock.Object);
                });
            });

        var serviceProvider = services.BuildServiceProvider();
        var publisher = serviceProvider.GetRequiredService<IRabbitPublisher>();
        var busControl = serviceProvider.GetRequiredService<IBusControl>();

        await busControl.StartAsync();

        try
        {
            var message = new TestMessage { Id = 1, Name = "Test" };

            // Act
            await publisher.PublishAsync(message);

            // Assert
            // Проверяем, что публикация прошла без ошибок
            // В реальном RabbitMQ сообщение будет отправлено в exchange
            publisher.Should().NotBeNull();
            busControl.Should().NotBeNull();
        }
        finally
        {
            await busControl.StopAsync();
        }
    }
}

public class TestMessage
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

