using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySensors.Message;

namespace MySensors.MessageProcessor
{
    public abstract class IncomingMessageProcessor
    {
        public abstract bool ProcessIncomingMessage(BaseMessage incomingMessage, out string responseMessage);
    }
}
