using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lails.MQ.Rabbit.Tests.Model;

[Table("Points")]
public class Point
{
    public long Id { get; set; }
    public DateTimeOffset DateTimeOffset { get; set; }
    public double Value { get; set; }
    public string Comment { get; set; }

    public dynamic MapToEvent()
    {
        return new Point
        {
            Id = Id
        }; ;
    }
}
