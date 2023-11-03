namespace Lails.MQ.Rabbit.Tests.Consumers
{
	public class AddPointsEventDto : IAddPointsEvent
	{
		public int Count { get; set; }
	}
}
