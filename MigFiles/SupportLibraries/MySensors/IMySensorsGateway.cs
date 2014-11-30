using System;
using System.Collections.Generic;
using MySensors.Message;

namespace MySensors
{
    public interface IMySensorsGateway : IDisposable
    {
        event Action<BaseMessage> OnReceiveMessageFromSensors;
        void Start();
        void Stop();
        IEnumerable<Sensor> GetSensors();
        void SendSetMessage(byte destNodeId, byte sensorId, string payload);
    }
}