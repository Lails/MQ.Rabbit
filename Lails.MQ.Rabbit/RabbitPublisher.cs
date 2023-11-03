using MassTransit;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Lails.MQ.Rabbit
{
    public class RabbitPublisher : IRabbitPublisher
    {
        private readonly IBus _bus;

        public RabbitPublisher(IBus bus)
        {
            _bus = bus;
        }

        public void Publish<T>(T message) where T : class
        {
            try
            {
                Log.Debug("Starting publishing the event {EventName} via publisher {Publisher}", typeof(T).Name, nameof(RabbitPublisher));
                _bus.Publish(message);
                Log.Debug("Ending publishing the event {EventName} via publisher {Publisher}", typeof(T).Name, nameof(RabbitPublisher));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while publishing the event via publisher {Publisher}. Event: {Event}, Name: {EventName}", nameof(RabbitPublisher), message, typeof(T).Name);

                throw;
            }
        }

        public async Task PublishAsync<T>(T message) where T : class
        {
            try
            {
                Log.Debug("Starting async publishing the event {EventName} via publisher {Publisher}", typeof(T).Name, nameof(RabbitPublisher));
                await _bus.Publish(message);
                Log.Debug("Ending async publishing the event {EventName} via publisher {Publisher}", typeof(T).Name, nameof(RabbitPublisher));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while async publishing the event via publisher {Publisher}. Event: {Event}, Name: {EventName}", nameof(RabbitPublisher), message, typeof(T).Name);

                throw;
            }
        }

        public async Task<Guid> SendScheduledMessageAsync<TMessage>(TMessage message, DateTime scheduledTime) where TMessage : class
        {
            try
            {
                return await _bus.SendScheduledMessage(message, scheduledTime);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"An error occurred while SendScheduledMessage the message: {message}, scheduledTime: {scheduledTime}");

                throw;
            }
        }

        public async Task CancelScheduledMessageAsync(Guid tokenId, Type messageType)
        {
            try
            {
                await _bus.CancelScheduledMessage(tokenId, messageType);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"An error occurred while CancelScheduledSend the tokenId: {tokenId}");

                throw;
            }
        }
    }
}
