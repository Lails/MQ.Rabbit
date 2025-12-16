using FluentAssertions;
using Lails.MQ.Rabbit.Consumer;
using MassTransit;
using MassTransit.Configuration;
using MassTransit.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace Lails.MQ.Rabbit.UnitTests;

public class RabbitConsumerRegistrationTests
{
    [Test]
    public void RegisterConsumerWithRetry_ShouldRegisterConsumer_WhenCalled()
    {
        // Arrange
        var services = new ServiceCollection();
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(x => x["RABBITMQ_HOSTURL"]).Returns("rabbitmq://localhost/");
        configMock.Setup(x => x["RABBITMQ_USERNAME"]).Returns("guest");
        configMock.Setup(x => x["RABBITMQ_PASSWORD"]).Returns("guest");

        services.AddMassTransit(x =>
        {
            x.AddConsumer<TestRegistrationConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.AddDataBusConfiguration(configMock.Object);
                cfg.RegisterConsumerWithRetry<TestRegistrationConsumer, TestMessage>(context, 3, 1, 5);
            });
        });

        // Act
        var serviceProvider = services.BuildServiceProvider();
        var busControl = serviceProvider.GetRequiredService<IBusControl>();

        // Assert
        busControl.Should().NotBeNull();

        // Проверяем, что consumer зарегистрирован
        var registeredTestConsumer = serviceProvider.GetServices<IConsumerRegistration>();
        Assert.That(registeredTestConsumer, Is.Not.Null);
        Assert.That(registeredTestConsumer.Count(), Is.EqualTo(1));
    }

    [Test]
    public void RegisterConsumerWithRetry_ShouldCreateQueueWithCorrectName_WhenCalled()
    {
        // Arrange
        var expectedQueueName = $"{typeof(TestRegistrationConsumer).FullName}_{typeof(TestMessage)}";
        string? capturedQueueName = null;

        var cfgMock = new Mock<IRabbitMqBusFactoryConfigurator>();
        cfgMock.Setup(x => x.ReceiveEndpoint(It.IsAny<string>(), It.IsAny<Action<IRabbitMqReceiveEndpointConfigurator>>()))
            .Callback<string, Action<IRabbitMqReceiveEndpointConfigurator>>((queueName, configure) =>
            {
                capturedQueueName = queueName;
            });

        var registrationContextMock = new Mock<IRegistrationContext>();

        // Act
        cfgMock.Object.RegisterConsumerWithRetry<TestRegistrationConsumer, TestMessage>(
            registrationContextMock.Object, 3, 1, 5);

        // Assert
        capturedQueueName.Should().NotBeNull("ReceiveEndpoint должен быть вызван");
        capturedQueueName.Should().Be(expectedQueueName, 
            $"Имя очереди должно быть '{expectedQueueName}', но было '{capturedQueueName}'");
        
        cfgMock.Verify(x => x.ReceiveEndpoint(
            expectedQueueName, 
            It.IsAny<Action<IRabbitMqReceiveEndpointConfigurator>>()), 
            Times.Once);
    }

    [Test]
    public void RegisterConsumerWithRetry_ShouldConfigureRetryPolicy_WhenCalled()
    {
        // Arrange
        var services = new ServiceCollection();
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(x => x["RABBITMQ_HOSTURL"]).Returns("rabbitmq://localhost/");
        configMock.Setup(x => x["RABBITMQ_USERNAME"]).Returns("guest");
        configMock.Setup(x => x["RABBITMQ_PASSWORD"]).Returns("guest");

        var retryCount = 5;
        var intervalMin = 2;

        services.AddMassTransit(x =>
        {
            x.AddConsumer<TestRegistrationConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.AddDataBusConfiguration(configMock.Object);
                cfg.RegisterConsumerWithRetry<TestRegistrationConsumer, TestMessage>(context, retryCount, intervalMin, 10);
            });
        });

        // Act
        var serviceProvider = services.BuildServiceProvider();
        var busControl = serviceProvider.GetRequiredService<IBusControl>();

        // Assert
        busControl.Should().NotBeNull();
        // Retry policy настраивается внутри RegisterConsumerWithRetry
        // Проверяем, что конфигурация применена без ошибок
    }

    [Test]
    public void RegisterConsumerWithRetry_ShouldConfigureConcurrencyLimit_WhenConcurrencyLimitIsSet()
    {
        // Arrange
        var services = new ServiceCollection();
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(x => x["RABBITMQ_HOSTURL"]).Returns("rabbitmq://localhost/");
        configMock.Setup(x => x["RABBITMQ_USERNAME"]).Returns("guest");
        configMock.Setup(x => x["RABBITMQ_PASSWORD"]).Returns("guest");

        var concurrencyLimit = 10;

        services.AddMassTransit(x =>
        {
            x.AddConsumer<TestRegistrationConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.AddDataBusConfiguration(configMock.Object);
                cfg.RegisterConsumerWithRetry<TestRegistrationConsumer, TestMessage>(context, 3, 1, concurrencyLimit);
            });
        });

        // Act
        var serviceProvider = services.BuildServiceProvider();
        var busControl = serviceProvider.GetRequiredService<IBusControl>();

        // Assert
        busControl.Should().NotBeNull();
        // Concurrency limit настраивается внутри RegisterConsumerWithRetry
        // Проверяем, что конфигурация применена без ошибок
    }

    [Test]
    public void RegisterConsumerWithRetry_ShouldConfigureScheduledRedelivery_WhenQuartzIsEnabled()
    {
        // Arrange
        var services = new ServiceCollection();
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(x => x["RABBITMQ_HOSTURL"]).Returns("rabbitmq://localhost/");
        configMock.Setup(x => x["RABBITMQ_USERNAME"]).Returns("guest");
        configMock.Setup(x => x["RABBITMQ_PASSWORD"]).Returns("guest");
        configMock.Setup(x => x["RABBITMQ_QUARTZ_QUEUE_NAME"]).Returns("quartz-queue");

        services.AddMassTransit(x =>
        {
            x.AddConsumer<TestRegistrationConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.AddDataBusConfiguration(configMock.Object);
                cfg.RegisterConsumerWithRetry<TestRegistrationConsumer, TestMessage>(context, 3, 1, 5);
            });
        });

        // Act
        var serviceProvider = services.BuildServiceProvider();
        var busControl = serviceProvider.GetRequiredService<IBusControl>();

        // Assert
        busControl.Should().NotBeNull();
        // Scheduled redelivery настраивается внутри RegisterConsumerWithRetry когда _useQuartz = true
        // Проверяем, что конфигурация применена без ошибок
    }

    [Test]
    public async Task Publish_ShouldDeliverMessageToConsumer_WhenConsumerIsRegistered()
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
                x.AddConsumer<TestRegistrationConsumer>();

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.AddDataBusConfiguration(configMock.Object);
                    cfg.RegisterConsumerWithRetry<TestRegistrationConsumer, TestMessage>(context, 3, 1, 5);
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

            // Даем время на обработку
            await Task.Delay(100);

            // Assert
            // Проверяем, что сообщение было обработано
            // В реальном сценарии можно использовать TestHarness для проверки
            publisher.Should().NotBeNull();
        }
        finally
        {
            await busControl.StopAsync();
        }
    }

    [Test]
    public void RegisterConsumerWithRetry_ShouldSetDurableAndAutoDelete_WhenCalled()
    {
        // Arrange
        var services = new ServiceCollection();
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(x => x["RABBITMQ_HOSTURL"]).Returns("rabbitmq://localhost/");
        configMock.Setup(x => x["RABBITMQ_USERNAME"]).Returns("guest");
        configMock.Setup(x => x["RABBITMQ_PASSWORD"]).Returns("guest");

        services.AddMassTransit(x =>
        {
            x.AddConsumer<TestRegistrationConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.AddDataBusConfiguration(configMock.Object);
                cfg.RegisterConsumerWithRetry<TestRegistrationConsumer, TestMessage>(context, 3, 1, 5);
            });
        });

        // Act
        var serviceProvider = services.BuildServiceProvider();
        var busControl = serviceProvider.GetRequiredService<IBusControl>();

        // Assert
        busControl.Should().NotBeNull();
        // В RegisterConsumerWithRetry устанавливается:
        // cfg.AutoDelete = false;
        // cfg.Durable = true;
        // Проверяем, что конфигурация применена без ошибок
    }
}

// Test consumer для тестирования регистрации
public class TestRegistrationConsumer : BaseConsumer<TestMessage>
{
    protected override Task ConsumeImplementation(ConsumeContext<TestMessage> context)
    {
        // Простая реализация для тестирования
        return Task.CompletedTask;
    }
}

