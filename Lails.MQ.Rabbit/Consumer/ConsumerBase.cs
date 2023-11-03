using MassTransit;
using System.Threading.Tasks;

namespace Lails.MQ.Rabbit.Consumer
{
    public abstract class ConsumerBase<TEvent> : IConsumer<TEvent> where TEvent : class
    {
        public async Task Consume(ConsumeContext<TEvent> context)
        {
            await ConsumeImpl(context);
        }

        protected abstract Task ConsumeImpl(ConsumeContext<TEvent> context);
    }
}