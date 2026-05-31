using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqConnector.Interfaces
{

    public interface IProducer { }

    [AttributeUsage(AttributeTargets.Class)]
    public class RabbitExchangeAttribute : Attribute
    {
        public string Name { get; }
        public string Type { get; }

        public RabbitExchangeAttribute(string name, string type = "topic")
        {
            Name = name;
            Type = type;
        }
    }
}
