using MassTransit;
using System.Threading.Tasks;

namespace Lails.MQ.Rabbit.Consumer
{
    public abstract class BaseConsumer<TEvent> : IConsumer<TEvent> where TEvent : class
    {
        public async Task Consume(ConsumeContext<TEvent> context)
        {
            await ConsumeImplementation(context);
        }

        protected abstract Task ConsumeImplementation(ConsumeContext<TEvent> context);
    }
}