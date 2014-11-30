using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySensors.Message;
using MySensors.MessageProcessor;

namespace MySensors
{
    public class Serial_MySensorsGateway : IMySensorsGateway
    {
        public event Action<BaseMessage> OnReceiveMessageFromSensors;

        private System.IO.Ports.SerialPort _serialPort;
        private bool _gatewayReady;

        public Serial_MySensorsGateway()
        {
            _serialPort = new SerialPort("COM3", 115200);
            _serialPort.DtrEnable = false;
            _serialPort.DataReceived +=_serialPort_DataReceived;
        }

        public void Start()
        {
            _serialPort.DtrEnable = true;
            _serialPort.Open();
            _serialPort.NewLine = "\n";
        }

        public void Stop()
        {
            _serialPort.Close();   
        }

        public IEnumerable<Sensor> GetSensors()
        {
            return DataAccess.GetSensors();
        }

        public void SendSetMessage(byte destNodeId, byte sensorId, string payload)
        {
            try
            {
                var strMessage = BaseMessage.BuildMessageString(destNodeId, sensorId, MessageType.SET, 1,
                        (byte)SensorDataType.V_LIGHT, payload);
                if (!string.IsNullOrEmpty(strMessage))
                    _serialPort.Write(strMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending message to {0} Type {1} SubType {2}", destNodeId, MessageType.SET, SensorDataType.V_LIGHT);
            }
        }

        #region Private Methods
        private void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            //0;0;3;0;14;Gateway startup complete
            string indata = _serialPort.ReadLine();

            if (indata.StartsWith("0;0;3;0;14;"))
                _gatewayReady = true;

            Task.Factory.StartNew(() =>
            {
                var parsedMessage = MessageFactory.ConstructMessageFromRaw(indata);

                Console.WriteLine("Receive Message from {0} Type {1} SubType {2}", parsedMessage.NodeId == 0 ? "Gateway" : parsedMessage.NodeId.ToString(), parsedMessage.GetType().Name, parsedMessage.SubType);
                var messageProcessor = MessageProcessorFactory.CreateMessageProcessor(parsedMessage);
                if (messageProcessor != null)
                {
                    string responseMessageString = null;
                    var raiseEvent = messageProcessor.ProcessIncomingMessage(parsedMessage, out responseMessageString);
                    if (!string.IsNullOrEmpty(responseMessageString))
                        _serialPort.Write(responseMessageString);
                    if (raiseEvent && OnReceiveMessageFromSensors != null)
                        OnReceiveMessageFromSensors(parsedMessage);
                }
            });
        }
        #endregion

        public void Dispose()
        {
            if (_serialPort == null)
                return;
            Stop();
        }
    }
}
