using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MySensors.Message
{
    public static class MessageFactory
    {
        public static BaseMessage ConstructMessageFromRaw(string rawMessage)
        {
            if(string.IsNullOrEmpty(rawMessage))
                throw new ArgumentNullException("rawMessage");

            var messageParts = rawMessage.Split(';');
            if(messageParts.Count() < 5)
                throw new ArgumentException("Message should be minimal 5 parts.");

            BaseMessage message = null;
            switch ((MessageType) byte.Parse(messageParts[2]))
            {
                case MessageType.PRESENTATION:
                    message = new PresentationMessage();
                    break;
                case MessageType.SET:
                    message = new SetDataMessage();
                    break;
                case MessageType.REQ:
                    message = new RequestDataMessage();
                    break;
                case MessageType.INTERNAL:
                    message = new InternalMessage();
                    break;
                case MessageType.STREAM:
                    break;
            }

            message.NodeId = byte.Parse(messageParts[0]);
            message.SensorId = byte.Parse(messageParts[1]);
            message.Ack = byte.Parse(messageParts[3]);
            message.SubType = byte.Parse(messageParts[4]);

            message.Payload = ASCIIEncoding.ASCII.GetBytes(messageParts[5]).Take(25).ToArray();

            return message;
        }
    }
}
