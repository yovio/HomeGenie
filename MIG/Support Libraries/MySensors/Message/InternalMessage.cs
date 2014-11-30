using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MySensors.Message
{
    public class InternalMessage : BaseMessage
    {
        public override MessageType MessageType { get{ return MessageType.INTERNAL;} }

        public InternalSubType InternalSubType
        {
            get { return (InternalSubType) SubType; }
        }
    }
}