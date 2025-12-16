using FluentAssertions;
using Lails.MQ.Rabbit.Consumer;
using MassTransit;
using Moq;
using NUnit.Framework;

namespace Lails.MQ.Rabbit.UnitTests;

public class BaseConsumerTests
{
    [Test]
    public async Task Consume_ShouldCallConsumeImplementation_WhenContextIsValid()
    {
        // Arrange
        var contextMock = new Mock<ConsumeContext<TestMessage>>();
        var message = new TestMessage { Id = 1, Name = "Test" };
        contextMock.Setup(x => x.Message).Returns(message);

        var consumer = new TestConsumer();

        // Act
        await consumer.Consume(contextMock.Object);

        // Assert
        consumer.ConsumedMessage.Should().BeSameAs(message);
        consumer.ConsumeImplementationCalled.Should().BeTrue();
    }

    [Test]
    public void Consume_ShouldPropagateException_WhenConsumeImplementationThrows()
    {
        // Arrange
        var contextMock = new Mock<ConsumeContext<TestMessage>>();
        var message = new TestMessage { Id = 1, Name = "Test" };
        contextMock.Setup(x => x.Message).Returns(message);

        var consumer = new TestConsumerWithException();

        // Act & Assert
        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await consumer.Consume(contextMock.Object));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Message, Is.EqualTo("Test exception"));
    }
}

// Test implementation of BaseConsumer
public class TestConsumer : BaseConsumer<TestMessage>
{
    public TestMessage? ConsumedMessage { get; private set; }
    public bool ConsumeImplementationCalled { get; private set; }

    protected override Task ConsumeImplementation(ConsumeContext<TestMessage> context)
    {
        ConsumedMessage = context.Message;
        ConsumeImplementationCalled = true;
        return Task.CompletedTask;
    }
}

// Test implementation that throws exception
public class TestConsumerWithException : BaseConsumer<TestMessage>
{
    protected override Task ConsumeImplementation(ConsumeContext<TestMessage> context)
    {
        throw new InvalidOperationException("Test exception");
    }
}

