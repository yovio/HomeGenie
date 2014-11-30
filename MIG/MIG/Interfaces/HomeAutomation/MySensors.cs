using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MIG.Interfaces.HomeAutomation.Commons;
using MySensors;
using MySensors.Message;

namespace MIG.Interfaces.HomeAutomation
{
    public class MySensors : MIGInterface
    {
        IMySensorsGateway _gateway = new Serial_MySensorsGateway(); 
        
        #region MIGInterface Implementation
        public string Domain
        {
            get
            {
                string domain = this.GetType().Namespace.ToString();
                domain = domain.Substring(domain.LastIndexOf(".") + 1) + "." + this.GetType().Name.ToString();
                return domain;
            }
        }

        public List<InterfaceModule> GetModules()
        {
            List<InterfaceModule> modules = new List<InterfaceModule>();
            if (IsConnected)
            {
                foreach (var sensor in _gateway.GetSensors())
                {
                    if (sensor.NodeId == 0) // My Sensors Gateway
                        continue;
                    //
                    // add new module
                    InterfaceModule module = new InterfaceModule
                    {
                        Domain = this.Domain,
                        Address = string.Format("{0}.{1}", sensor.NodeId, sensor.Id)
                    };
                    //module.Description = "ZWave Node";
                    switch (sensor.SensorType)
                    {
                        case SensorPresentationType.S_DOOR:
                            {
                                module.Description = "MySensors Door/Window Sensor";
                                module.ModuleType = ModuleTypes.DoorWindow;
                            }
                            break;
                        case SensorPresentationType.S_MOTION:
                            {
                                module.Description = "MySensors Motion Sensor";
                                module.ModuleType = ModuleTypes.Motion;
                            }
                            break;
                        case SensorPresentationType.S_SMOKE:
                            break;
                        case SensorPresentationType.S_LIGHT:
                            {
                                module.Description = "MySensors Light Switch";
                                module.ModuleType = ModuleTypes.Light;
                            }
                            break;
                        case SensorPresentationType.S_DIMMER:
                            break;
                        case SensorPresentationType.S_COVER:
                            break;
                        case SensorPresentationType.S_TEMP:
                            break;
                        case SensorPresentationType.S_HUM:
                            break;
                        case SensorPresentationType.S_BARO:
                            break;
                        case SensorPresentationType.S_WIND:
                            break;
                        case SensorPresentationType.S_RAIN:
                            break;
                        case SensorPresentationType.S_UV:
                            break;
                        case SensorPresentationType.S_WEIGHT:
                            break;
                        case SensorPresentationType.S_POWER:
                            break;
                        case SensorPresentationType.S_HEATER:
                            break;
                        case SensorPresentationType.S_DISTANCE:
                            break;
                        case SensorPresentationType.S_LIGHT_LEVEL:
                            break;
                        case SensorPresentationType.S_ARDUINO_NODE:
                            break;
                        case SensorPresentationType.S_ARDUINO_REPEATER_NODE:
                            break;
                        case SensorPresentationType.S_LOCK:
                            break;
                        case SensorPresentationType.S_IR:
                            break;
                        case SensorPresentationType.S_WATER:
                            break;
                        case SensorPresentationType.S_AIR_QUALITY:
                            break;
                        case SensorPresentationType.S_CUSTOM:
                            break;
                        case SensorPresentationType.S_DUST:
                            break;
                        case SensorPresentationType.S_SCENE_CONTROLLER:
                            break;
                        default:
                            module.ModuleType = ModuleTypes.Generic;
                            break;
                    }
                    modules.Add(module);
                }
            }
            return modules;
        }

        public List<MIGServiceConfiguration.Interface.Option> Options { get; set; }
        public event Action<InterfacePropertyChangedAction> InterfacePropertyChangedAction;
        public event Action<InterfaceModulesChangedAction> InterfaceModulesChangedAction;
        public object InterfaceControl(MIGInterfaceCommand request)
        {
            string response = "[{ ResponseValue : 'OK' }]";

            var address = request.NodeId.Split('.');
            if (address.Length == 2)
            {
                byte nodeId = byte.Parse(address[0]);
                byte sensorId = byte.Parse(address[1]);
                var command = request.Command;

                if (command.ToLower() == "control.on")
                {
                    _gateway.SendSetMessage(nodeId, sensorId, "1");
                }
                else if (command.ToLower() == "control.off")
                {
                    _gateway.SendSetMessage(nodeId, sensorId, "0");
                }
            }

            return response;
        }

        public bool IsConnected { get; private set; }
        public bool Connect()
        {
            _gateway.Start();
            _gateway.OnReceiveMessageFromSensors += gateway_OnReceiveMessageFromSensors;
            IsConnected = true;
            return IsConnected;
        }

        

        public void Disconnect()
        {
            _gateway.OnReceiveMessageFromSensors -= gateway_OnReceiveMessageFromSensors;
            _gateway.Stop();
            IsConnected = false;
        }

        public bool IsDevicePresent()
        {
            return true;
        } 
        #endregion

        #region Private Methods
        private void gateway_OnReceiveMessageFromSensors(BaseMessage baseMessage)
        {
            if (baseMessage == null)
                return;

            if (baseMessage is SetDataMessage)
            {
                var setDataMsg = baseMessage as SetDataMessage;
                var propChangeAction = new InterfacePropertyChangedAction()
                {
                    Domain = this.Domain,
                    SourceId = string.Format("{0}.{1}", setDataMsg.NodeId, setDataMsg.SensorId),
                    SourceType = "MySensors Node"
                };

                switch (setDataMsg.SensorDataType)
                {
                    case SensorDataType.V_TEMP:
                        break;
                    case SensorDataType.V_HUM:
                        break;
                    case SensorDataType.V_LIGHT:
                        break;
                    case SensorDataType.V_DIMMER:
                        break;
                    case SensorDataType.V_PRESSURE:
                        break;
                    case SensorDataType.V_FORECAST:
                        break;
                    case SensorDataType.V_RAIN:
                        break;
                    case SensorDataType.V_RAINRATE:
                        break;
                    case SensorDataType.V_WIND:
                        break;
                    case SensorDataType.V_GUST:
                        break;
                    case SensorDataType.V_DIRECTION:
                        break;
                    case SensorDataType.V_UV:
                        break;
                    case SensorDataType.V_WEIGHT:
                        break;
                    case SensorDataType.V_DISTANCE:
                        break;
                    case SensorDataType.V_IMPEDANCE:
                        break;
                    case SensorDataType.V_ARMED:
                        break;
                    case SensorDataType.V_TRIPPED:
                        break;
                    case SensorDataType.V_WATT:
                        {
                            propChangeAction.Path = ModuleParameters.MODPAR_METER_WATTS;
                            propChangeAction.Value = Double.Parse(Encoding.Default.GetString(baseMessage.Payload));
                        }
                        break;
                    case SensorDataType.V_KWH:
                        break;
                    case SensorDataType.V_SCENE_ON:
                        break;
                    case SensorDataType.V_SCENE_OFF:
                        break;
                    case SensorDataType.V_HEATER:
                        break;
                    case SensorDataType.V_HEATER_SW:
                        break;
                    case SensorDataType.V_LIGHT_LEVEL:
                        break;
                    case SensorDataType.V_VAR1:
                        break;
                    case SensorDataType.V_VAR2:
                        break;
                    case SensorDataType.V_VAR3:
                        break;
                    case SensorDataType.V_VAR4:
                        break;
                    case SensorDataType.V_VAR5:
                        break;
                    case SensorDataType.V_UP:
                        break;
                    case SensorDataType.V_DOWN:
                        break;
                    case SensorDataType.V_STOP:
                        break;
                    case SensorDataType.V_IR_SEND:
                        break;
                    case SensorDataType.V_IR_RECEIVE:
                        break;
                    case SensorDataType.V_FLOW:
                        break;
                    case SensorDataType.V_VOLUME:
                        break;
                    case SensorDataType.V_LOCK_STATUS:
                        break;
                    case SensorDataType.V_DUST_LEVEL:
                        break;
                    case SensorDataType.V_VOLTAGE:
                        break;
                    case SensorDataType.V_CURRENT:
                        break;
                    default:
                        break;
                }
                InterfacePropertyChangedAction(propChangeAction);
                Console.WriteLine("Raise Event To HomeGenie");
            }
        } 
        #endregion
    }
}
