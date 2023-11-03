using System;
using System.Threading.Tasks;

namespace Lails.MQ.Rabbit
{
	public class RabbitPublisherStub : IRabbitPublisher
	{
		public void Publish<T>(T message) where T : class { }
		public Task PublishAsync<T>(T message) where T : class
		{
			return Task.CompletedTask;
		}
		public Task CancelScheduledMessageAsync(Guid tokenId, Type messageType)
		{
			return Task.CompletedTask;
		}

		public Task<Guid> SendScheduledMessageAsync<TMessage>(TMessage message, DateTime scheduledTime) where TMessage : class
		{
			return Task.FromResult(Guid.Empty);
		}
	}
}
