using Lails.MQ.Rabbit.Consumer;
using Lails.MQ.Rabbit.Tests.Model;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lails.MQ.Rabbit.Tests.Consumers;

public class AddPointConsumer : BaseConsumer<IAddPointsEvent>
{
    readonly LailsMQTestDbContext _db;
    public AddPointConsumer(LailsMQTestDbContext db)
    {
        _db = db;
    }

    protected override async Task ConsumeImplementation(ConsumeContext<IAddPointsEvent> context)
    {
        var currentCount = 0;
        var points = new List<Point>();
        while (context.Message.Count > currentCount)
        {
            currentCount++;

            var point = new Point
            {
                Comment = "",
                DateTimeOffset = DateTimeOffset.Now.UtcDateTime,
                Value = currentCount
            };
            points.Add(point);
        }

        await _db.AddRangeAsync(points);
        await _db.SaveChangesAsync();
    }
}
