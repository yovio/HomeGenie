using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MySensors.Message
{
    public class PresentationMessage : BaseMessage
    {
        public override MessageType MessageType { get { return MessageType.PRESENTATION; } }

        public SensorPresentationType SensorPresentationType
        {
            get { return (SensorPresentationType)SubType; }
        }
    }
}
