using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MySensors.Message
{
    public class StreamMessage : BaseMessage
    {
        public override MessageType MessageType { get { return MessageType.STREAM; } }

        public StreamSubType StreamSubType
        {
            get { return (StreamSubType)SubType; }
        }
    }
}
