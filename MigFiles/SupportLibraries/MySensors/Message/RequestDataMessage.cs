using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MySensors.Message
{
    public class RequestDataMessage : BaseMessage
    {
        public override MessageType MessageType { get { return MessageType.REQ; } }

        public SensorDataType SensorDataType
        {
            get { return (SensorDataType)SubType; }
        }
    }
}
