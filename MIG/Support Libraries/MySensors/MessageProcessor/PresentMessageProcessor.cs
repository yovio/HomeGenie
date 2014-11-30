using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySensors.Message;

namespace MySensors.MessageProcessor
{
    public class PresentMessageProcessor : IncomingMessageProcessor
    {
        public override bool ProcessIncomingMessage(BaseMessage incomingMessage, out string responseMessage)
        {
            responseMessage = null;
            var presentMessage = incomingMessage as PresentationMessage;
            if (presentMessage == null)
            {
                return false;
            }

            var sourceNodeId = presentMessage.NodeId;

            if (sourceNodeId < 255)
            {
                DataAccess.UpdateSensor(sourceNodeId, presentMessage.SensorId, (byte)presentMessage.SensorPresentationType, null, DateTime.Now);
            }

            return false;
        }
    }
}
