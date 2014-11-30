using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySensors.Message;

namespace MySensors.MessageProcessor
{
    public static class MessageProcessorFactory
    {
        public static IncomingMessageProcessor CreateMessageProcessor(BaseMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.INTERNAL:
                    return new InternalMessageProcessor();
                    break;
                case MessageType.PRESENTATION:
                    return new PresentMessageProcessor();
                    break;
                case MessageType.REQ:
                    break;
                case MessageType.SET:
                    return new SetDataMessageProcessor();
                    break;
                case MessageType.STREAM:
                    break;
            }
            return null;
        }
    }
}
