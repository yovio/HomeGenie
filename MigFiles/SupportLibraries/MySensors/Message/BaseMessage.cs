using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MySensors.Message
{
    public abstract class BaseMessage
    {
        public byte NodeId { get; set; }

        public byte SensorId { get; set; }

        public abstract MessageType MessageType { get; }

        public byte SubType { get; set; }

        public byte Ack { get; set; }

        public byte[] Payload { get; set; }

        public static string BuildMessageString(byte destNode, byte sensorId, MessageType messageType, byte ack, byte subType, string payLoad)
        {
            if (!string.IsNullOrEmpty(payLoad) && payLoad.Length > 25)
                payLoad = payLoad.Substring(0, 25);

            return string.Format("{0};{1};{2};{3};{4};{5}\n", destNode, sensorId, (byte)messageType, ack, subType, payLoad);
        }
    }
}
