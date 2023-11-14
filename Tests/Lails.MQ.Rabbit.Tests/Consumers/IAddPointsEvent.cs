namespace Lails.MQ.Rabbit.Tests.Consumers
{
    public interface IAddPointsEvent
    {
        int Count { get; set; }
    }
}
