using FluentAssertions;
using NUnit.Framework;

namespace Lails.MQ.Rabbit.UnitTests;

public class RabbitPublisherStubTests
{
    private readonly RabbitPublisherStub _stub;

    public RabbitPublisherStubTests()
    {
        _stub = new RabbitPublisherStub();
    }

    [Test]
    public async Task PublishAsync_ShouldReturnCompletedTask_WhenMessageIsValid()
    {
        // Arrange
        var message = new TestMessage { Id = 1, Name = "Test" };

        // Act
        var task = _stub.PublishAsync(message);

        // Assert
        task.Should().NotBeNull();
        await task; // Should complete immediately
        task.IsCompleted.Should().BeTrue();
    }

    [Test]
    public async Task SendScheduledMessageAsync_ShouldReturnEmptyGuid_WhenCalled()
    {
        // Arrange
        var message = new TestMessage { Id = 1, Name = "Test" };
        var scheduledTime = DateTime.UtcNow.AddMinutes(5);

        // Act
        var result = await _stub.SendScheduledMessageAsync(message, scheduledTime);

        // Assert
        result.Should().Be(Guid.Empty);
    }

    [Test]
    public async Task CancelScheduledMessageAsync_ShouldReturnCompletedTask_WhenCalled()
    {
        // Arrange
        var tokenId = Guid.NewGuid();
        var messageType = typeof(TestMessage);

        // Act
        var task = _stub.CancelScheduledMessageAsync(tokenId, messageType);

        // Assert
        task.Should().NotBeNull();
        await task; // Should complete immediately
        task.IsCompleted.Should().BeTrue();
    }
}

