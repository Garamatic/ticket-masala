using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqConnector
{
    public enum RoutingKeys
    {
        None = -1,
        Ticket = 0,
        Project = 2,
        Test = 3,
        //Add more as needed
        All = 500
    }
}
