using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MySensors
{
    class Program
    {
        static void Main(string[] args)
        {
            TestSerial();
        }

        private static void TestSPI()
        {
            using (var gatewayInstance = new SPI_MySensorsGateway())
            {
                gatewayInstance.Start();

                Console.WriteLine("Press any key to stop");
                Console.ReadLine();
            }

            //var da = new DataAccess();
        }

        private static void TestSerial()
        {
            using (var gatewayInstance = new Serial_MySensorsGateway())
            {
                gatewayInstance.Start();

                Console.WriteLine("Press any key to stop");
                Console.ReadLine();
            }

            //var da = new DataAccess();
        }
    }
}
