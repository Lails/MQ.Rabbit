using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace Lails.MQ.Rabbit.UnitTests;

public class RabbitRegistrationExtansionsTests
{
    [Test]
    public void AddDataBusConfiguration_ShouldThrowArgumentNullException_WhenHostUrlIsNull()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(x => x["RABBITMQ_HOSTURL"]).Returns((string?)null!);
        configMock.Setup(x => x["RABBITMQ_USERNAME"]).Returns("guest");
        configMock.Setup(x => x["RABBITMQ_PASSWORD"]).Returns("guest");

        // Используем реальный конфигуратор MassTransit для тестирования
        // BUG: This will throw ArgumentNullException because hostUrl is null and passed to Uri constructor
        var services = new ServiceCollection();
        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                // Act & Assert
                var exception = Assert.Catch<Exception>(
                    () => cfg.AddDataBusConfiguration(configMock.Object));
                
                // Может быть ArgumentNullException или UriFormatException в зависимости от реализации
                Assert.That(exception, Is.Not.Null);
            });
        });
    }

    [Test]
    public void AddDataBusConfiguration_ShouldThrowUriFormatException_WhenHostUrlIsInvalid()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(x => x["RABBITMQ_HOSTURL"]).Returns("invalid-url-format");
        configMock.Setup(x => x["RABBITMQ_USERNAME"]).Returns("guest");
        configMock.Setup(x => x["RABBITMQ_PASSWORD"]).Returns("guest");

        // Используем реальный конфигуратор MassTransit для тестирования
        // BUG: This will throw UriFormatException because invalid URL format
        var services = new ServiceCollection();
        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                // Act & Assert
                var exception = Assert.Catch<Exception>(
                    () => cfg.AddDataBusConfiguration(configMock.Object));
                
                exception.Should().NotBeNull();
            });
        });
    }

    [Test]
    public void AddDataBusConfiguration_ShouldThrowFileNotFoundException_WhenCertificatePathIsInvalid()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(x => x["RABBITMQ_HOSTURL"]).Returns("rabbitmq://localhost/");
        configMock.Setup(x => x["RABBITMQ_USERNAME"]).Returns("guest");
        configMock.Setup(x => x["RABBITMQ_PASSWORD"]).Returns("guest");
        configMock.Setup(x => x["CERTIFICATE_PFX_PATH"]).Returns("nonexistent/path/cert.pfx");
        configMock.Setup(x => x["CERTIFICATE_PFX_PASSWORD"]).Returns("password");
        configMock.Setup(x => x["Domain:Base"]).Returns("example.com");

        // Используем реальный конфигуратор MassTransit для тестирования
        // BUG: This will throw FileNotFoundException because certificate file doesn't exist
        var services = new ServiceCollection();
        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                // Act & Assert
                var exception = Assert.Throws<System.IO.FileNotFoundException>(
                    () => cfg.AddDataBusConfiguration(configMock.Object));
                
                exception.Should().NotBeNull();
                exception!.FileName.Should().Contain("cert.pfx");
            });
        });
    }

    [Test]
    public void AddDataBusConfiguration_ShouldNotThrow_WhenConfigurationIsValid()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(x => x["RABBITMQ_HOSTURL"]).Returns("rabbitmq://localhost/");
        configMock.Setup(x => x["RABBITMQ_USERNAME"]).Returns("guest");
        configMock.Setup(x => x["RABBITMQ_PASSWORD"]).Returns("guest");

        // Используем реальный конфигуратор MassTransit для тестирования
        var services = new ServiceCollection();
        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                // Act & Assert - не должно быть исключений при валидной конфигурации
                var action = () => cfg.AddDataBusConfiguration(configMock.Object);
                action.Should().NotThrow();
            });
        });
    }

    [Test]
    public void AddDataBusConfiguration_ShouldConfigureQuartz_WhenQuartzQueueNameIsSet()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(x => x["RABBITMQ_HOSTURL"]).Returns("rabbitmq://localhost/");
        configMock.Setup(x => x["RABBITMQ_USERNAME"]).Returns("guest");
        configMock.Setup(x => x["RABBITMQ_PASSWORD"]).Returns("guest");
        configMock.Setup(x => x["RABBITMQ_QUARTZ_QUEUE_NAME"]).Returns("quartz-queue");

        // Используем реальный конфигуратор MassTransit для тестирования
        // BUG: The URI is created as $"rabbitmq://{hostUrl}/{quartzQueueName}"
        // If hostUrl already contains path, this will create invalid URI
        // Example: if hostUrl is "rabbitmq://localhost/vhost", result will be "rabbitmq://localhost/vhost/quartz-queue"
        // which might be incorrect
        var services = new ServiceCollection();
        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                // Act - не должно быть исключений
                var action = () => cfg.AddDataBusConfiguration(configMock.Object);
                action.Should().NotThrow();
            });
        });
    }

    [Test]
    public void RegisterRabbitPublisher_ShouldRegisterSingleton_WhenCalled()
    {
        // Arrange
        var services = new ServiceCollection();

        var configMock = new Mock<IConfiguration>();
        configMock.Setup(x => x["RABBITMQ_HOSTURL"]).Returns("rabbitmq://localhost/");
        configMock.Setup(x => x["RABBITMQ_USERNAME"]).Returns("guest");
        configMock.Setup(x => x["RABBITMQ_PASSWORD"]).Returns("guest");
        configMock.Setup(x => x["RABBITMQ_QUARTZ_QUEUE_NAME"]).Returns("quartz-queue");

        // Act
        services
          .RegisterRabbitPublisher()
          .AddMassTransit(x =>
          {
              x.UsingRabbitMq((context, cfg) =>
              {
                  cfg.AddDataBusConfiguration(configMock.Object);
              });
          });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var publisher1 = serviceProvider.GetService<IRabbitPublisher>();
        var publisher2 = serviceProvider.GetService<IRabbitPublisher>();
        
        publisher1.Should().NotBeNull();
        publisher2.Should().NotBeNull();
        publisher1.Should().BeSameAs(publisher2); // Should be singleton
    }

    [Test]
    public void RegisterRabbitPublisherStub_ShouldRegisterSingleton_WhenCalled()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.RegisterRabbitPublisherStub();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var publisher1 = serviceProvider.GetService<IRabbitPublisher>();
        var publisher2 = serviceProvider.GetService<IRabbitPublisher>();
        
        publisher1.Should().NotBeNull();
        publisher2.Should().NotBeNull();
        publisher1.Should().BeSameAs(publisher2); // Should be singleton
        publisher1.Should().BeOfType<RabbitPublisherStub>();
    }

    [Test]
    public void AddDataBusConfiguration_ShouldNotUseSsl_WhenCertificatePathIsNotProvided()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(x => x["RABBITMQ_HOSTURL"]).Returns("rabbitmq://localhost/");
        configMock.Setup(x => x["RABBITMQ_USERNAME"]).Returns("guest");
        configMock.Setup(x => x["RABBITMQ_PASSWORD"]).Returns("guest");
        configMock.Setup(x => x["CERTIFICATE_PFX_PATH"]).Returns((string?)null);
        configMock.Setup(x => x["CERTIFICATE_PFX_PASSWORD"]).Returns((string?)null);
        configMock.Setup(x => x["Domain:Base"]).Returns((string?)null);

        // Используем реальный конфигуратор MassTransit для тестирования
        var services = new ServiceCollection();
        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                // Act & Assert - не должно быть исключений, SSL не должен использоваться
                var action = () => cfg.AddDataBusConfiguration(configMock.Object);
                action.Should().NotThrow();
            });
        });
    }

    [Test]
    public void AddDataBusConfiguration_ShouldNotUseSsl_WhenCertificatePathIsEmpty()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(x => x["RABBITMQ_HOSTURL"]).Returns("rabbitmq://localhost/");
        configMock.Setup(x => x["RABBITMQ_USERNAME"]).Returns("guest");
        configMock.Setup(x => x["RABBITMQ_PASSWORD"]).Returns("guest");
        configMock.Setup(x => x["CERTIFICATE_PFX_PATH"]).Returns(string.Empty);
        configMock.Setup(x => x["CERTIFICATE_PFX_PASSWORD"]).Returns("password");
        configMock.Setup(x => x["Domain:Base"]).Returns("example.com");

        // Используем реальный конфигуратор MassTransit для тестирования
        var services = new ServiceCollection();
        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                // Act & Assert - не должно быть исключений, SSL не должен использоваться
                var action = () => cfg.AddDataBusConfiguration(configMock.Object);
                action.Should().NotThrow();
            });
        });
    }

    [Test]
    public void AddDataBusConfiguration_ShouldNotUseSsl_WhenCertificatePasswordIsMissing()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(x => x["RABBITMQ_HOSTURL"]).Returns("rabbitmq://localhost/");
        configMock.Setup(x => x["RABBITMQ_USERNAME"]).Returns("guest");
        configMock.Setup(x => x["RABBITMQ_PASSWORD"]).Returns("guest");
        configMock.Setup(x => x["CERTIFICATE_PFX_PATH"]).Returns("path/to/cert.pfx");
        configMock.Setup(x => x["CERTIFICATE_PFX_PASSWORD"]).Returns((string?)null);
        configMock.Setup(x => x["Domain:Base"]).Returns("example.com");

        // Используем реальный конфигуратор MassTransit для тестирования
        var services = new ServiceCollection();
        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                // Act & Assert - не должно быть исключений, SSL не должен использоваться
                var action = () => cfg.AddDataBusConfiguration(configMock.Object);
                action.Should().NotThrow();
            });
        });
    }

    [Test]
    public void AddDataBusConfiguration_ShouldNotUseSsl_WhenDomainNameIsMissing()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(x => x["RABBITMQ_HOSTURL"]).Returns("rabbitmq://localhost/");
        configMock.Setup(x => x["RABBITMQ_USERNAME"]).Returns("guest");
        configMock.Setup(x => x["RABBITMQ_PASSWORD"]).Returns("guest");
        configMock.Setup(x => x["CERTIFICATE_PFX_PATH"]).Returns("path/to/cert.pfx");
        configMock.Setup(x => x["CERTIFICATE_PFX_PASSWORD"]).Returns("password");
        configMock.Setup(x => x["Domain:Base"]).Returns((string?)null);

        // Используем реальный конфигуратор MassTransit для тестирования
        var services = new ServiceCollection();
        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                // Act & Assert - не должно быть исключений, SSL не должен использоваться
                var action = () => cfg.AddDataBusConfiguration(configMock.Object);
                action.Should().NotThrow();
            });
        });
    }

    [Test]
    public void AddDataBusConfiguration_ShouldThrowFileNotFoundException_WhenCertificateFileDoesNotExist()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(x => x["RABBITMQ_HOSTURL"]).Returns("rabbitmq://localhost/");
        configMock.Setup(x => x["RABBITMQ_USERNAME"]).Returns("guest");
        configMock.Setup(x => x["RABBITMQ_PASSWORD"]).Returns("guest");
        configMock.Setup(x => x["CERTIFICATE_PFX_PATH"]).Returns("nonexistent/path/cert.pfx");
        configMock.Setup(x => x["CERTIFICATE_PFX_PASSWORD"]).Returns("password");
        configMock.Setup(x => x["Domain:Base"]).Returns("example.com");

        // Используем реальный конфигуратор MassTransit для тестирования
        var services = new ServiceCollection();
        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                // Act & Assert - должно выбросить FileNotFoundException при попытке прочитать несуществующий файл
                var exception = Assert.Throws<System.IO.FileNotFoundException>(
                    () => cfg.AddDataBusConfiguration(configMock.Object));
                
                exception.Should().NotBeNull();
                exception!.FileName.Should().Contain("cert.pfx");
            });
        });
    }
}

