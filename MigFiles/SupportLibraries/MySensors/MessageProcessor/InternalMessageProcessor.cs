using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySensors.Message;

namespace MySensors.MessageProcessor
{
    public class InternalMessageProcessor : IncomingMessageProcessor
    {
        public override bool ProcessIncomingMessage(BaseMessage incomingMessage, out string responseMessage)
        {
            var internalMessage = incomingMessage as InternalMessage;
            if (internalMessage == null)
            {
                responseMessage = null;
                return false;
            }

            var sourceNodeId = internalMessage.NodeId;
            responseMessage = null;
            bool raiseExternalEvent = false;
            switch (internalMessage.InternalSubType)
            {
                case InternalSubType.I_BATTERY_LEVEL:
                    break;
                case InternalSubType.I_TIME:
                    break;
                case InternalSubType.I_VERSION:
                    break;
                case InternalSubType.I_ID_REQUEST:
                    {
                        var newId = DataAccess.GetNewNodeId();
                        responseMessage = BaseMessage.BuildMessageString(sourceNodeId, 0, MessageType.INTERNAL, 0,(byte) InternalSubType.I_ID_RESPONSE, newId.ToString());
                    }
                    break;
                case InternalSubType.I_INCLUSION_MODE:
                    break;
                case InternalSubType.I_CONFIG:
                    break;
                case InternalSubType.I_FIND_PARENT:
                    break;
                case InternalSubType.I_LOG_MESSAGE:
                    break;
                case InternalSubType.I_CHILDREN:
                    break;
                case InternalSubType.I_SKETCH_NAME:
                    if (sourceNodeId < 255)
                    {
                        var sketchName = Encoding.Default.GetString(internalMessage.Payload);
                        DataAccess.UpdateSketchName(sourceNodeId, sketchName);
                    }
                    break;
                case InternalSubType.I_SKETCH_VERSION:
                    if (sourceNodeId < 255)
                    {
                        var sketchVersion = Encoding.Default.GetString(internalMessage.Payload);
                        DataAccess.UpdateSketchVersion(sourceNodeId, sketchVersion);
                    }
                    break;
                case InternalSubType.I_REBOOT:
                    break;
                case InternalSubType.I_GATEWAY_READY:
                    break;
            }

            return raiseExternalEvent;
        }
    }
}
