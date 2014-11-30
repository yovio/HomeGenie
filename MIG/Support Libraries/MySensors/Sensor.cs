using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MySensors
{
    public class Sensor
    {
        public ushort NodeId { get; set; }
        public ushort Id { get; set; }
        public SensorPresentationType SensorType { get; set; }
        public string Name { get; set; }
        public DateTime LastConnectTime { get; set; }
    }
}
