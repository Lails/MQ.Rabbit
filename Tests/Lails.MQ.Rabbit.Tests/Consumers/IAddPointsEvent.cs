using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lails.MQ.Rabbit.Tests.Consumers
{
	public interface IAddPointsEvent
	{
		int Count { get; set; }
	}
}
