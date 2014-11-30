using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MySensors.Message;
using MySensors.MessageProcessor;

namespace MySensors
{
    public class SPI_MySensorsGateway : IMySensorsGateway
    {
        private const int C_PROCESS_RADIO_MESSSAGE_INTERVALE = 100;
        private IntPtr _unmanagedGateway;
        private bool _isRunning;
        private Timer _processRadioMessageTimer;
        
        #region Creators

        public SPI_MySensorsGateway()
        {
            _processRadioMessageTimer = new Timer(OnProcessRadioMessage, null, Timeout.Infinite, C_PROCESS_RADIO_MESSSAGE_INTERVALE);
        }
        #endregion

        public event Action<BaseMessage> OnReceiveMessageFromSensors;

        #region Propeties
        #endregion

        #region Public Methods
        public void Start()
        {
            Console.WriteLine("Starting Gateway...");
            _unmanagedGateway = GatewayWrapper.CreateGateway(CE_Pin.RPI_P1_22, SPI_Port.SPI_0, SPI_Speed.SPI_Speed_8MHZ, 1);
            GatewayWrapper.CallBegin(_unmanagedGateway, PA_Level.RF24_PA_MAX, 76, RF24_DataRate.RF24_250KBPS, OnReceiveMessage);
            _isRunning = true;
            _processRadioMessageTimer.Change(0, C_PROCESS_RADIO_MESSSAGE_INTERVALE);
        }

        public void Stop()
        {
            if (_unmanagedGateway == IntPtr.Zero)
                return;

            Console.WriteLine("Stop Gateway...");
            try
            {
                _processRadioMessageTimer.Change(Timeout.Infinite, C_PROCESS_RADIO_MESSSAGE_INTERVALE);
                _isRunning = false;
                GatewayWrapper.DisposeGateway(_unmanagedGateway);
            }
            finally
            {
                _unmanagedGateway = IntPtr.Zero;
            }
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
                    GatewayWrapper.CallParseAndSend(_unmanagedGateway, strMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending message to {0} Type {1} SubType {2}", destNodeId, MessageType.SET, SensorDataType.V_LIGHT);
            }
        }
        #endregion

        #region Private Methods
        private void OnProcessRadioMessage(object state)
        {
            try
            {
                if (_isRunning && _unmanagedGateway != IntPtr.Zero)
                {
                    GatewayWrapper.CallProcessRadioMessage(_unmanagedGateway);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error during CallProcessRadioMessage: {0}\n{1}", ex.Message, ex.StackTrace);
            }
        }

        private void OnReceiveMessage(string messageFromGateway)
        {
            Task.Factory.StartNew(() => 
            { 
            var parsedMessage = MessageFactory.ConstructMessageFromRaw(messageFromGateway);

            Console.WriteLine("Receive Message from {0} Type {1} SubType {2}", parsedMessage.NodeId == 0 ? "Gateway" : parsedMessage.NodeId.ToString(), parsedMessage.GetType().Name, parsedMessage.SubType);
            var messageProcessor = MessageProcessorFactory.CreateMessageProcessor(parsedMessage);
            if (messageProcessor != null)
            {
                string responseMessageString = null;
                var raiseEvent = messageProcessor.ProcessIncomingMessage(parsedMessage, out responseMessageString);
                if (!string.IsNullOrEmpty(responseMessageString))
                    GatewayWrapper.CallParseAndSend(_unmanagedGateway, responseMessageString);
                if (raiseEvent && OnReceiveMessageFromSensors != null)
                    OnReceiveMessageFromSensors(parsedMessage);
            }
            });
        }
        #endregion

        #region IDisposable Implementation
        public void Dispose()
        {
            if (_unmanagedGateway == IntPtr.Zero)
                return;
            Stop();
        } 
        #endregion
    }
}
