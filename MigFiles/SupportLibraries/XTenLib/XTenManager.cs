﻿/*
    This file is part of HomeGenie Project source code.

    HomeGenie is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    HomeGenie is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with HomeGenie.  If not, see <http://www.gnu.org/licenses/>.  
*/

/*
 *     Author: Generoso Martello <gene@homegenie.it>
 *     Project Homepage: http://homegenie.it
 */


//
// references: X10 protocol documentation from http://www.linuxha.com/USB/cm15a.html
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;

using XTenLib.Drivers;

namespace XTenLib
{
    public class RfDataReceivedAction
    {
        public byte[] RawData;
    }

    public class XTenManager
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event Action<RfDataReceivedAction> RfDataReceived;

        private Thread readerTask;
        private Thread connectionWatcher;

        private string monitoredHouseCode = "A";
        private Dictionary<string, X10Module> moduleStatus = new Dictionary<string, X10Module>();
        private Dictionary<string, List<X10Module>> addressedModules = new Dictionary<string, List<X10Module>>();

        private string portName = "USB";
        private XTenInterface x10interface;

        private bool isInterfaceReady = false;
        private X10CommState communicationState = X10CommState.Ready;
        private byte expectedChecksum = 0x00;
        //TODO: rename the following two as constants
        private double commandTimeoutSeconds = 5.0;
        private int commandResendMax = 1;
        private byte[] commandLastMessage = new byte[0];
        private int commandResendAttempts = 0;
        private object waitAckMonitor = new object();
        private DateTime lastReceivedTs = DateTime.Now;

        private bool gotReadWriteError = true;
        private bool keepConnectionAlive = false;

        // this is used on Linux for detecting when the link gets disconnected
        private int zeroChecksumCount = 0;

        public XTenManager()
        {
            HouseCode = "A";
            x10interface = new CM15();
        }


        public string HouseCode
        {
            get { return monitoredHouseCode; }
            set
            {
                monitoredHouseCode = value;
                for (int i = 0; i < moduleStatus.Keys.Count; i++)
                {
                    moduleStatus[moduleStatus.Keys.ElementAt(i)].PropertyChanged -= ModulePropertyChanged;
                }
                moduleStatus.Clear();
                //
                string[] hc = monitoredHouseCode.Split(',');
                for (int i = 0; i < hc.Length; i++)
                {
                    for (int x = 1; x <= 16; x++)
                    {
                        var module = new X10Module() { Code = hc[i] + x.ToString(), /*Status = "OFF",*/ Level = 0.0 };
                        //
                        module.PropertyChanged += ModulePropertyChanged;
                        //
                        moduleStatus.Add(hc[i] + x.ToString(), module);
                    }
                }
                //
                if (!gotReadWriteError && x10interface != null && x10interface.GetType().Equals(typeof(CM15)))
                {
                    SetCm15Codes();
                }
            }
        }


        public string PortName
        {
            get { return portName; }
            set
            {
                if (portName != value)
                {
                    Close();
                    //
                    if (value.ToUpper() == "USB")
                    {
                        x10interface = new CM15();
                    }
                    else
                    {
                        x10interface = new CM11(value);
                    }
                    //
                    gotReadWriteError = true;
                }
                portName = value;
            }
        }

        public bool Connect()
        {
            Disconnect();
            //
            bool returnValue = Open();
            //
            keepConnectionAlive = true;
            connectionWatcher = new Thread(new ThreadStart(delegate()
            {
                gotReadWriteError = !returnValue;
                //
                while (keepConnectionAlive)
                {
                    if (gotReadWriteError)
                    {
                        isInterfaceReady = false;
                        try
                        {
                            ResetCurrentData();
                            //
                            Close();
                            //
                            // wait 5 secs before reconnecting
                            Thread.Sleep(5000);
                            if (keepConnectionAlive)
                            {
                                try
                                {
                                    gotReadWriteError = !Open();
                                }
                                catch
                                {
                                }
                            }
                        }
                        catch
                        {
                            //Console.WriteLine(unex.Message + "\n" + unex.StackTrace);
                        }
                    }
                    Thread.Sleep(2000);
                }
            }));
            connectionWatcher.Start();
            //    
            return returnValue;
        }

        public void Disconnect()
        {
            keepConnectionAlive = false;
            try
            {
                connectionWatcher.Abort();
            }
            catch
            {
            }
            connectionWatcher = null;
            //
            Close();
        }

        public bool IsConnected
        {
            get { return isInterfaceReady || (!gotReadWriteError && x10interface.GetType().Equals(typeof(CM15))); }
        }




        public Dictionary<string, X10Module> ModulesStatus
        {
            get { return moduleStatus; }
        }



        public void StatusRequest(X10HouseCodes housecode, X10UnitCodes unitcode)
        {
            lock (this)
            {
                //string hcunit = String.Format("{0:X}{1:X}", (int)housecode, (int)unitcode);
                string hcfuntion = String.Format("{0:x1}{1:x1}", (int)housecode, (int)X10Command.Status_Request);
                SendModuleAddress(housecode, unitcode);
                SendMessage(new byte[] {
                    (int)X10CommandType.Function,
                    byte.Parse(
                        hcfuntion,
                        System.Globalization.NumberStyles.HexNumber
                    )
                });
            }
        }

        public void Dim(X10HouseCodes housecode, X10UnitCodes unitcode, int percentage)
        {
            lock (this)
            {
                string huc = Utility.HouseUnitCodeFromEnum(housecode, unitcode);
                string hcfuntion = String.Format("{0:x1}{1:x1}", (int)housecode, (int)X10Command.Dim);
                //
                SendModuleAddress(housecode, unitcode);
                if (x10interface.GetType().Equals(typeof(CM15)))
                {
                    double normalized = ((double)percentage / 100D);
                    SendMessage(new byte[] {
                        (int)X10CommandType.Function,
                        byte.Parse(
                            hcfuntion,
                            System.Globalization.NumberStyles.HexNumber
                        ),
                        (byte)(normalized * 210)
                    });
                    double newLevel = moduleStatus[huc].Level - normalized;
                    if (newLevel < 0) newLevel = 0;
                    moduleStatus[huc].Level = newLevel;
                }
                else
                {
                    byte dimvalue = Utility.GetDimValue(percentage);
                    SendMessage(new byte[] {
                        (byte)((int)X10CommandType.Function | dimvalue | 0x04),
                        byte.Parse(
                            hcfuntion,
                            System.Globalization.NumberStyles.HexNumber
                        )
                    });
                    double newLevel = moduleStatus[huc].Level - Utility.GetPercentageValue(dimvalue);
                    if (newLevel < 0) newLevel = 0;
                    moduleStatus[huc].Level = newLevel;
                }
            }
        }

        public void Bright(X10HouseCodes housecode, X10UnitCodes unitcode, int percentage)
        {
            lock (this)
            {
                string huc = Utility.HouseUnitCodeFromEnum(housecode, unitcode);
                //string hcunit = String.Format("{0:X}{1:X}", (int)housecode, (int)unitcode);
                string hcfuntion = String.Format("{0:x1}{1:x1}", (int)housecode, (int)X10Command.Bright);
                //
                SendModuleAddress(housecode, unitcode);
                if (x10interface.GetType().Equals(typeof(CM15)))
                {
                    double normalized = ((double)percentage / 100D);
                    SendMessage(new byte[] {
                        (int)X10CommandType.Function,
                        byte.Parse(
                            hcfuntion,
                            System.Globalization.NumberStyles.HexNumber
                        ),
                        (byte)(normalized * 210)
                    });
                    double newLevel = moduleStatus[huc].Level + normalized;
                    if (newLevel > 1) newLevel = 1;
                    moduleStatus[huc].Level = newLevel;
                }
                else
                {
                    byte dimvalue = Utility.GetDimValue(percentage);
                    SendMessage(new byte[] {
                        (byte)((int)X10CommandType.Function | dimvalue | 0x04),
                        byte.Parse(
                            hcfuntion,
                            System.Globalization.NumberStyles.HexNumber
                        )
                    });
                    double newLevel = moduleStatus[huc].Level + Utility.GetPercentageValue(dimvalue);
                    if (newLevel > 1) newLevel = 1;
                    moduleStatus[huc].Level = newLevel;
                }
            }
        }

        public void LightOn(X10HouseCodes housecode, X10UnitCodes unitcode)
        {
            lock (this)
            {
                //string hcunit = String.Format("{0:X}{1:X}", (int)housecode, (int)unitcode);
                string hcfuntion = String.Format("{0:x1}{1:x1}", (int)housecode, (int)X10Command.On);
                SendModuleAddress(housecode, unitcode);
                SendMessage(new byte[] {
                    (int)X10CommandType.Function,
                    byte.Parse(
                        hcfuntion,
                        System.Globalization.NumberStyles.HexNumber
                    )
                });
                //
                string huc = Utility.HouseUnitCodeFromEnum(housecode, unitcode);
                if (moduleStatus[huc].Level == 0.0)
                {
                    moduleStatus[huc].Level = 1.0;
                }
            }
        }

        public void LightOff(X10HouseCodes housecode, X10UnitCodes unitcode)
        {
            lock (this)
            {
                //string hcunit = String.Format("{0:X}{1:X}", (int)housecode, (int)unitcode);
                string hcfuntion = String.Format("{0:x1}{1:x1}", (int)housecode, (int)X10Command.Off);
                SendModuleAddress(housecode, unitcode);
                SendMessage(new byte[] {
                    (int)X10CommandType.Function,
                    byte.Parse(
                        hcfuntion,
                        System.Globalization.NumberStyles.HexNumber
                    )
                });
                //
                string huc = Utility.HouseUnitCodeFromEnum(housecode, unitcode);
                moduleStatus[huc].Level = 0.0;
            }
        }

        public void AllLightsOn(X10HouseCodes housecode)
        {
            lock (this)
            {
                string hcunit = String.Format("{0:X}{1:X}", (int)housecode, 0);
                string hcfuntion = String.Format("{0:x1}{1:x1}", (int)housecode, (int)X10Command.All_Lights_On);
                SendMessage(new byte[] {
                    (int)X10CommandType.Address,
                    byte.Parse(
                        hcunit,
                        System.Globalization.NumberStyles.HexNumber
                    )
                });
                SendMessage(new byte[] {
                    (int)X10CommandType.Function,
                    byte.Parse(
                        hcfuntion,
                        System.Globalization.NumberStyles.HexNumber
                    )
                });
                //
                // TODO: pick only lights module
                AllLightsOn(housecode.ToString());
            }
        }

        public void AllUnitsOff(X10HouseCodes housecode)
        {
            lock (this)
            {
                string hcunit = String.Format("{0:X}{1:X}", (int)housecode, 0);
                string hcfuntion = String.Format("{0:x1}{1:x1}", (int)housecode, (int)X10Command.All_Units_Off);
                SendMessage(new byte[] {
                    (int)X10CommandType.Address,
                    byte.Parse(
                        hcunit,
                        System.Globalization.NumberStyles.HexNumber
                    )
                });
                SendMessage(new byte[] {
                    (int)X10CommandType.Function,
                    byte.Parse(
                        hcfuntion,
                        System.Globalization.NumberStyles.HexNumber
                    )
                });
                //
                // TODO: pick only lights module
                AllUnitsOff(housecode.ToString());
            }
        }

        private void UpdateInterfaceTime(bool batteryClear)
        {
            /*
            The PC must then respond with the following transmission

            Bit range	Description
            55 to 48	timer download header (0x9b)
            47 to 40	Current time (seconds)
            39 to 32	Current time (minutes ranging from 0 to 119)
            31 to 23	Current time (hours/2, ranging from 0 to 11)
            23 to 16	Current year day (bits 0 to 7)
            15	Current year day (bit 8)
            14 to 8		Day mask (SMTWTFS)
            7 to 4		Monitored house code
            3		Reserved
            2		Battery timer clear flag
            1		Monitored status clear flag
            0		Timer purge flag
            */
            var date = DateTime.Now;
            int minute = date.Minute;
            int hour = date.Hour / 2;
            if (Math.IEEERemainder(date.Hour, 2) > 0)
            { // Add remaining minutes 
                minute += 60;
            }
            int wday = Convert.ToInt16(Math.Pow(2, (int)date.DayOfWeek));
            int yearDay = date.DayOfYear - 1;
            if (yearDay > 255)
            {
                yearDay = yearDay - 256;
                // Set current yearDay flag in wday's 7:th bit, since yearDay overflowed...
                wday = wday + Convert.ToInt16(Math.Pow(2, 7));
            }
            // Build message
            byte[] message = new byte[8];
            message[0] = 0x9b;   // cm11 x10 time download header
            message[1] = Convert.ToByte(date.Second);
            message[2] = Convert.ToByte(minute);
            message[3] = Convert.ToByte(hour);
            message[4] = Convert.ToByte(yearDay);
            message[5] = Convert.ToByte(wday);
            message[6] = Convert.ToByte((batteryClear ? 0x07 : 0x03) + Utility.HouseCodeFromString(this.HouseCode)); // Send timer purgeflag + Monitored status clear flag, monitored house code.
            //
            if (x10interface.GetType().Equals(typeof(CM15)))
            {
                // this seems to be needed only with CM15
                message[7] = 0x02;
            }
            //
            ResetCurrentData();
            DebugLog("X10 <", Utility.ByteArrayToString(message));
            //
            SendMessage(message);
        }


        private void SendMessage(byte[] message)
        {
            try
            {

                // Wait for message delivery acknowledge
                if (message.Length > 1 && IsConnected)
                {
                    lock(waitAckMonitor)
                    {
                        while((DateTime.Now - lastReceivedTs).TotalMilliseconds < 500)
                        {
                            Thread.Sleep(50);
                        }

                        DebugLog("X10 <", Utility.ByteArrayToString(message));
                        x10interface.WriteData(message);

                        commandLastMessage = message;
                        waitAckTimestamp = DateTime.Now;

                        if (x10interface.GetType().Equals(typeof(CM11)))
                        {
                            expectedChecksum = (byte)((message[0] + message[1]) & 0xff);
                            communicationState = X10CommState.WaitingChecksum;
                        }
                        else
                        {
                            communicationState = X10CommState.WaitingAck;
                        }

                        while (commandResendAttempts < commandResendMax && communicationState != X10CommState.Ready)
                        {
                            var elapsedFromWaitAck = DateTime.Now - waitAckTimestamp;
                            while (elapsedFromWaitAck.TotalSeconds < commandTimeoutSeconds && communicationState != X10CommState.Ready)
                            {
                                Thread.Sleep(50);
                                elapsedFromWaitAck = DateTime.Now - waitAckTimestamp;
                            }
                            if (elapsedFromWaitAck.TotalSeconds >= commandTimeoutSeconds && communicationState != X10CommState.Ready)
                            {
                                // Resend last message
                                commandResendAttempts++;
                                DebugLog(
                                    "X10 >",
                                    "PREVIOUS COMMAND TIMED OUT, RESENDING(" + commandResendAttempts + ")"
                                );
                                x10interface.WriteData(commandLastMessage);
                                waitAckTimestamp = DateTime.Now;
                            }
                        }
                        commandResendAttempts = 0;
                        commandLastMessage = new byte[0];

                    }
                }
                else
                {

                    DebugLog("X10 <", Utility.ByteArrayToString(message));
                    x10interface.WriteData(message);

                }

            }
            catch (Exception ex)
            {
                DebugLog("X10 !", ex.Message);
                DebugLog("X10 !", ex.StackTrace);

                gotReadWriteError = true;
            }
            Thread.Sleep(50);
        }


        private void SetCm15Codes()
        {
            lock (this)
            {
                // BuildTransceivedCodesMessage return byte message for setting transceive codes from given comma separated _monitoredhousecode
                UpdateInterfaceTime(false);
                byte[] trcommand = CM15.BuildTransceivedCodesMessage(monitoredHouseCode);
                SendMessage(trcommand);
                SendMessage(new byte[] { 0x8B });
            }
        }


        private void ModulePropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            // route event
            if (PropertyChanged != null)
            {
                PropertyChanged(sender, args);
            }
        }



        private bool Open()
        {
            bool success = (x10interface != null && x10interface.Open());
            if (success)
            {
                //
                // set transceived house codes for CM15 X10 RF-->PLC
                if (x10interface.GetType().Equals(typeof(CM15)))
                {
                    SetCm15Codes();
                }
                //
                readerTask = new Thread(new ThreadStart(ReaderThreadLoop));
                readerTask.Start();
            }
            return success;
        }

        private void Close()
        {
            try
            {
                readerTask.Abort();
            }
            catch
            {
            }
            readerTask = null;
            //
            try
            {
                x10interface.Close();
            }
            catch
            {
            }
        }


        private void SendModuleAddress(X10HouseCodes housecode, X10UnitCodes unitcode)
        {
            if (!addressedModules.ContainsKey(housecode.ToString()))
            {
                addressedModules.Add(housecode.ToString(), new List<X10Module>());
            }
            string huc = Utility.HouseUnitCodeFromEnum(housecode, unitcode);
            var mod = moduleStatus[huc];
            // TODO: do more tests about this optimization
            //if (!addressedModules[housecode.ToString()].Contains(mod) || addressedModules[housecode.ToString()].Count > 1)
            {
                addressedModules[housecode.ToString()].Clear();
                addressedModules[housecode.ToString()].Add(mod);
                string hcunit = String.Format("{0:X}{1:X}", (int)housecode, (int)unitcode);
                SendMessage(new byte[] {
                    (int)X10CommandType.Address,
                    byte.Parse(
                        hcunit,
                        System.Globalization.NumberStyles.HexNumber
                    )
                });
            }
        }

        private void ModulesOn(string housecode)
        {
            if (addressedModules.ContainsKey(housecode))
            {
                foreach (X10Module mod in addressedModules[housecode])
                {
                    mod.Level = 1.0;
                }
            }
        }

        private void ModulesOff(string housecode)
        {
            if (addressedModules.ContainsKey(housecode))
            {
                foreach (X10Module mod in addressedModules[housecode])
                {
                    mod.Level = 0.0;
                }
            }
        }

        private void ModulesBright(string housecode, byte parameter)
        {
            if (addressedModules.ContainsKey(housecode))
            {
                foreach (X10Module mod in addressedModules[housecode])
                {

                    var brightLevel = mod.Level + (((double)parameter) / 210D);
                    if (brightLevel > 1) brightLevel = 1;
                    mod.Level = brightLevel;
                }
            }
        }

        private void ModulesDim(string housecode, byte parameter)
        {
            if (addressedModules.ContainsKey(housecode))
            {
                foreach (X10Module mod in addressedModules[housecode])
                {

                    var dimLevel = mod.Level - (((double)parameter) / 210D);
                    if (dimLevel < 0) dimLevel = 0;
                    mod.Level = dimLevel;
                }
            }
        }

        private void AllUnitsOff(string housecode)
        {
            if (addressedModules.ContainsKey(housecode.ToString()))
            {
                addressedModules[housecode.ToString()].Clear();
            }
            // TODO: select only light modules 
            foreach (KeyValuePair<string, X10Module> modkv in moduleStatus)
            {
                if (modkv.Value.Code.StartsWith(housecode))
                {
                    modkv.Value.Level = 0.0;
                }
            }
        }

        private void AllLightsOn(string housecode)
        {
            if (addressedModules.ContainsKey(housecode.ToString()))
            {
                addressedModules[housecode.ToString()].Clear();
            }
            // TODO: pick only light modules 
            foreach (KeyValuePair<string, X10Module> modkv in moduleStatus)
            {
                if (modkv.Value.Code.StartsWith(housecode))
                {
                    modkv.Value.Level = 1.0;
                }
            }
        }



        private DateTime waitAckTimestamp = DateTime.Now;

        private void ReaderThreadLoop()
        {
            while (true)
            {
                try
                {
                    byte[] readData = x10interface.ReadData();
                    if (readData.Length > 0)
                    {
                        DebugLog("X10 >", Utility.ByteArrayToString(readData));

                        var elapsedFromWaitAck = DateTime.Now - waitAckTimestamp;
                        if (elapsedFromWaitAck.TotalSeconds >= commandTimeoutSeconds && communicationState != X10CommState.Ready) 
                        {
                            DebugLog(
                                "X10 >",
                                "COMMAND TIMEOUT"
                            );
                            communicationState = X10CommState.Ready;
                        }
                        //
                        if (communicationState == X10CommState.WaitingAck && readData[0] == (int)X10CommandType.PLC_Ready && readData.Length <= 2) // ack received
                        {
                            DebugLog(
                                "X10 >",
                                "COMMAND SUCCESSFUL"
                            );
                            communicationState = X10CommState.Ready;
                        }
                        else if ((readData.Length >= 13 || (readData.Length == 2 && readData[0] == 0xFF && readData[1] == 0x00)) && !isInterfaceReady)
                        {
                            UpdateInterfaceTime(false);
                            isInterfaceReady = true;
                            communicationState = X10CommState.Ready;
                        }
                        else if (readData.Length == 2 && communicationState == X10CommState.WaitingChecksum && readData[0] == expectedChecksum && readData[1] == 0x00)
                        {
                            // checksum is received only from CM11
                            DebugLog(
                                "X10 >",
                                "CKSUM: " + "Expected [" + Utility.ByteArrayToString(new byte[] { expectedChecksum }) + "] Checksum ==> " + Utility.ByteArrayToString(readData)
                            );
                            //TODO: checksum verification not handled, we just reply 0x00 (OK)
                            SendMessage(new byte[] { 0x00 });
                            communicationState = X10CommState.WaitingAck;
                        }
                        else if (readData[0] == (int)X10CommandType.Macro)
                        {
                            lastReceivedTs = DateTime.Now;
                            DebugLog("X10 >", "MACRO: " + Utility.ByteArrayToString(readData));
                        }
                        else if (readData[0] == (int)X10CommandType.RF)
                        {
                            lastReceivedTs = DateTime.Now;
                            DebugLog("X10 >", "RFCOM: " + Utility.ByteArrayToString(readData));
                            if (RfDataReceived != null)
                            {
                                Thread signal = new Thread(() =>
                                {
                                    addressedModules.Clear();
                                    RfDataReceived(new RfDataReceivedAction() { RawData = readData });
                                });
                                signal.Start();
                            }
                        }
                        else if ((readData[0] == (int)X10CommandType.PLC_Poll) && readData.Length <= 2)
                        {
                            isInterfaceReady = true;
                            SendMessage(new byte[] { (byte)X10CommandType.PLC_ReplyToPoll }); // reply to poll
                        }
                        else if ((readData[0] == (int)X10CommandType.PLC_FilterFail_Poll) && readData.Length <= 2)
                        {
                            isInterfaceReady = true;
                            SendMessage(new byte[] { (int)X10CommandType.PLC_FilterFail_Poll }); // reply to filter fail poll
                        }
                        else if ((readData[0] == (int)X10CommandType.PLC_Poll))
                        {
                            lastReceivedTs = DateTime.Now;
                            DebugLog("X10 >", "PLCRX: " + Utility.ByteArrayToString(readData));
                            //
                            if (readData.Length > 3)
                            {
                                bool newAddressData = true;
                                int messageLength = readData[1];
                                if (readData.Length > messageLength - 2)
                                {
                                    char[] bitmapData = Convert.ToString(readData[2], 2).PadLeft(8, '0').ToCharArray();
                                    byte[] functionBitmap = new byte[messageLength - 1];
                                    for (int i = 0; i < functionBitmap.Length; i++)
                                    {
                                        functionBitmap[i] = byte.Parse(bitmapData[7 - i].ToString());
                                    }

                                    byte[] messageData = new byte[messageLength - 1];
                                    Array.Copy(readData, 3, messageData, 0, messageLength - 1);

                                    // CM15 Extended receive has got inverted data
                                    if (messageLength > 2 && x10interface.GetType().Equals(typeof(CM15)))
                                    {
                                        Array.Reverse(functionBitmap, 0, functionBitmap.Length);
                                        Array.Reverse(messageData, 0, messageData.Length);
                                    }

                                    DebugLog("X10 >", "FNMAP: " + Utility.ByteArrayToString(functionBitmap));
                                    DebugLog("X10 >", " DATA: " + Utility.ByteArrayToString(messageData));

                                    for (int b = 0; b < messageData.Length; b++)
                                    {
                                        // read current byte data (type: 0x00 address, 0x01 function)
                                        if (functionBitmap[b] == (byte)X10FunctionType.Address) // address
                                        {
                                            string housecode = ((X10HouseCodes)Convert.ToInt16(
                                                                   messageData[b].ToString("X2").Substring(
                                                                       0,
                                                                       1
                                                                   ),
                                                                   16
                                                               )).ToString();
                                            string unitcode = ((X10UnitCodes)Convert.ToInt16(
                                                                  messageData[b].ToString("X2").Substring(
                                                                      1,
                                                                      1
                                                                  ),
                                                                  16
                                                              )).ToString();
                                            if (unitcode.IndexOf("_") > 0) unitcode = unitcode.Substring(unitcode.IndexOf("_") + 1);
                                            //
                                            DebugLog("X10 >", "      " + b + ") House code = " + housecode);
                                            DebugLog("X10 >", "      " + b + ")  Unit code = " + unitcode);
                                            //
                                            string currentUnitCode = housecode + unitcode;
                                            if (!moduleStatus.Keys.Contains(currentUnitCode))
                                            {
                                                var module = new X10Module() { Code = currentUnitCode };
                                                module.PropertyChanged += ModulePropertyChanged;
                                                moduleStatus.Add(currentUnitCode, module);
                                            }
                                            var mod = moduleStatus[currentUnitCode];
                                            //
                                            //TODO: this needs more testing....
                                            if (!addressedModules.ContainsKey(housecode))
                                            {
                                                addressedModules.Add(housecode, new List<X10Module>());
                                            }
                                            else if (newAddressData)
                                            {
                                                newAddressData = false;
                                                addressedModules[housecode].Clear();
                                            }
                                            //
                                            if (!addressedModules[housecode].Contains(mod))
                                            {
                                                addressedModules[housecode].Add(mod);
                                            }
                                        }
                                        else if (functionBitmap[b] == (byte)X10FunctionType.Function) // function
                                        {
                                            string currentCommand = ((X10Command)Convert.ToInt16(
                                                                        messageData[b].ToString("X2").Substring(
                                                                            1,
                                                                            1
                                                                        ),
                                                                        16
                                                                    )).ToString().ToUpper();
                                            string currentHouseCode = ((X10HouseCodes)Convert.ToInt16(
                                                                          messageData[b].ToString("X2").Substring(
                                                                              0,
                                                                              1
                                                                          ),
                                                                          16
                                                                      )).ToString();
                                            //
                                            DebugLog("X10 >", "      " + b + ") House code = " + currentHouseCode);
                                            DebugLog("X10 >", "      " + b + ")    Command = " + currentCommand);
                                            //
                                            //
                                            //TODO: this needs more testing....
                                            if (!addressedModules.ContainsKey(currentHouseCode))
                                            {
                                                addressedModules.Add(currentHouseCode, new List<X10Module>());
                                            }
                                            //
                                            switch (currentCommand)
                                            {
                                            case "ALL_UNITS_OFF":
                                                if (currentHouseCode != "") AllUnitsOff(currentHouseCode);
                                                break;
                                            case "ALL_LIGHTS_ON":
                                                if (currentHouseCode != "") AllLightsOn(currentHouseCode);
                                                break;
                                            case "ON":
                                                ModulesOn(currentHouseCode);
                                                break;
                                            case "OFF":
                                                ModulesOff(currentHouseCode);
                                                break;
                                            case "BRIGHT":
                                                ModulesBright(currentHouseCode, messageData[++b]);
                                                break;
                                            case "DIM":
                                                ModulesDim(currentHouseCode, messageData[++b]);
                                                break;
                                            }
                                            //
                                            newAddressData = true;
                                        }
                                    }

                                }


                            }
                        }
                        else if ((readData[0] == (int)X10CommandType.PLC_TimeRequest)) // IS THIS A TIME REQUEST?
                        {
                            UpdateInterfaceTime(false);
                        }
                        else
                        {
                            #region This is an hack for detecting disconnection status on Linux platforms
                            if (readData[0] == 0x00)
                            {
                                zeroChecksumCount++;
                            }
                            else
                            {
                                zeroChecksumCount = 0;
                            }
                            //
                            if (zeroChecksumCount > 10)
                            {
                                zeroChecksumCount = 0;
                                gotReadWriteError = true;
                                Close();
                            }
                            #endregion
                        }
                    }
                }
                catch (Exception e)
                {
                    if (!e.GetType().Equals(typeof(TimeoutException)) && !e.GetType().Equals(typeof(OverflowException)))
                    {
                        DebugLog("X10 !", e.Message);
                        DebugLog("X10 !", e.StackTrace);

                        gotReadWriteError = true;
                    }
                }
            }
        }

        private void ResetCurrentData()
        {
            addressedModules.Clear();
        }

        private void DebugLog(string prefix, string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            if (prefix.Contains(">"))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
            }
            else if (prefix.Contains("!"))
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
            }
            Console.Write("[" + DateTime.Now.ToString("HH:mm:ss.ffffff") + "] ");
            Console.WriteLine(prefix + " " + message);
            Console.ForegroundColor = ConsoleColor.White;
        }

    }

}

