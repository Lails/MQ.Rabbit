using Lails.MQ.Rabbit.Tests.Consumers;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Lails.MQ.Rabbit.Tests.Controllers
{
    [Route("api/mq-tests")]
    [ApiController]
    public class MQTestsController : ControllerBase
    {
        private readonly IRabbitPublisher _busPublisher;
        public MQTestsController(IRabbitPublisher busPublisher)
        {
            _busPublisher = busPublisher;
        }

        [HttpPost]
        [Route("publisher")]
        public async Task TestPublisher(int count)
        {
            for (int i = 0; i < count; i++)
            {
                await _busPublisher.PublishAsync(new AddPointsEventDto { Count = count });
            }
        }

        [HttpPost]
        [Route("publish-and-cancel")]//TODO: check
        public async Task InitCancelTest(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var id = await _busPublisher.SendScheduledMessageAsync<AddPointsEventDto>(new AddPointsEventDto() { Count = count }, DateTime.UtcNow.AddMinutes(0));
                await _busPublisher.CancelScheduledMessageAsync(id, typeof(AddPointsEventDto));
            }
        }
    }
}