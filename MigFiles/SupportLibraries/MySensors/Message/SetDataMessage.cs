using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MySensors.Message
{
    public class SetDataMessage : BaseMessage
    {
        public override MessageType MessageType { get { return MessageType.SET; } }

        public SensorDataType SensorDataType
        {
            get { return (SensorDataType)SubType; }
        }
    }
}
