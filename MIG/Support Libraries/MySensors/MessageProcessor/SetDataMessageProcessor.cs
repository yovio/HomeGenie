using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySensors.Message;

namespace MySensors.MessageProcessor
{
    public class SetDataMessageProcessor : IncomingMessageProcessor
    {
        public override bool ProcessIncomingMessage(BaseMessage incomingMessage, out string responseMessage)
        {
            responseMessage = null;
            var setDataMessage = incomingMessage as SetDataMessage;
            if (setDataMessage == null)
            {
                return false;
            }

            return true;
        }
    }
}
