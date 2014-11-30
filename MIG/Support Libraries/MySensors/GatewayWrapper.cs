using System;
using System.Runtime.InteropServices;

namespace MySensors
{
    public class GatewayWrapper
    {
        public delegate void Func([In, MarshalAs(UnmanagedType.LPStr)] string messageFromGateway);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cePin">RPi Pin used for CE</param>
        /// <param name="spiPort">RPI SPI port</param>
        /// <param name="spiSpeed"></param>
        /// <param name="inclusionTime">Inclusion time in minute</param>
        /// <returns></returns>
        [DllImport("libCsharpGateway")]
        static public extern IntPtr CreateGateway(CE_Pin cePin, SPI_Port spiPort, SPI_Speed spiSpeed, byte inclusionTime);

        [DllImport("libCsharpGateway")]
        static public extern void DisposeGateway(IntPtr pGateway);

        /// <summary>
        /// Start Sensor network
        /// </summary>
        /// <param name="pGateway">Pointer to Gateway created by calling CreateGateway</param>
        /// <param name="pa_Level">RF Power level</param>
        /// <param name="rfChannel">RF Channel, default 76, possible value 0-127</param>
        [DllImport("libCsharpGateway", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
        static public extern void CallBegin(IntPtr pGateway, PA_Level pa_Level, byte rfChannel, RF24_DataRate rf24DataRate, Func dataFromGatewayCallback);

        [DllImport("libCsharpGateway", CallingConvention = CallingConvention.StdCall)]
        static public extern void CallProcessRadioMessage(IntPtr pGateway);

        [DllImport("libCsharpGateway", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
        static public extern void CallParseAndSend(IntPtr pGateway, [MarshalAs(UnmanagedType.LPStr)]string message);
    }
}
