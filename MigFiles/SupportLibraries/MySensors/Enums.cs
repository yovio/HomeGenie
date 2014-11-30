using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MySensors
{
    #region Hardware Related
    public enum PA_Level
    {
        RF24_PA_MIN = 0,
        RF24_PA_LOW,
        RF24_PA_HIGH,
        RF24_PA_MAX
    }

    public enum RF24_DataRate
    {
        RF24_1MBPS = 0,
        RF24_2MBPS,
        RF24_250KBPS
    }

    public enum SPI_Port : byte
    {
        SPI_0 = 0,
        SPI_1
    }

    public enum CE_Pin : byte
    {
        RPI_P1_11 = 17,
        RPI_P1_15 = 22,
        RPI_P1_16 = 23,
        RPI_P1_18 = 24,
        RPI_P1_22 = 25
    }

    public enum SPI_Speed : ushort
    {
        SPI_Speed_32MHZ = 8,
        SPI_Speed_16MHZ = 16,
        SPI_Speed_8MHZ = 32,
        SPI_Speed_4MHZ = 64,
        SPI_Speed_2MHZ = 128,
        SPI_Speed_1MHZ = 256,
        SPI_Speed_512KHZ = 512,
        SPI_Speed_256KHZ = 1024,
        SPI_Speed_128KHZ = 2048,
    } 
    #endregion

    public enum MessageType : byte
    {
        PRESENTATION = 0,
        SET = 1,
        REQ = 2,
        INTERNAL = 3,
        STREAM = 4
    }

    public enum InternalSubType : byte
    {
        I_BATTERY_LEVEL = 0,
        I_TIME,
        I_VERSION,
        I_ID_REQUEST,
        I_ID_RESPONSE,
        I_INCLUSION_MODE,
        I_CONFIG,
        I_FIND_PARENT,
        I_FIND_PARENT_RESPONSE,
        I_LOG_MESSAGE,
        I_CHILDREN,
        I_SKETCH_NAME,
        I_SKETCH_VERSION,
        I_REBOOT,
        I_GATEWAY_READY
    }

    public enum SensorPresentationType : byte
    {
        S_DOOR,
        S_MOTION,
        S_SMOKE,
        S_LIGHT,
        S_DIMMER,
        S_COVER,
        S_TEMP,
        S_HUM,
        S_BARO,
        S_WIND,
        S_RAIN,
        S_UV,
        S_WEIGHT,
        S_POWER,
        S_HEATER,
        S_DISTANCE,
        S_LIGHT_LEVEL,
        S_ARDUINO_NODE,
        S_ARDUINO_REPEATER_NODE,
        S_LOCK,
        S_IR,
        S_WATER,
        S_AIR_QUALITY,
        S_CUSTOM,
        S_DUST,
        S_SCENE_CONTROLLER
    }

    public enum SensorDataType : byte
    {
        V_TEMP,
        V_HUM,
        V_LIGHT,
        V_DIMMER,
        V_PRESSURE,
        V_FORECAST,
        V_RAIN,
        V_RAINRATE,
        V_WIND,
        V_GUST,
        V_DIRECTION,
        V_UV,
        V_WEIGHT,
        V_DISTANCE,
        V_IMPEDANCE,
        V_ARMED,
        V_TRIPPED,
        V_WATT,
        V_KWH,
        V_SCENE_ON,
        V_SCENE_OFF,
        V_HEATER,
        V_HEATER_SW,
        V_LIGHT_LEVEL,
        V_VAR1,
        V_VAR2,
        V_VAR3,
        V_VAR4,
        V_VAR5,
        V_UP,
        V_DOWN,
        V_STOP,
        V_IR_SEND,
        V_IR_RECEIVE,
        V_FLOW,
        V_VOLUME,
        V_LOCK_STATUS,
        V_DUST_LEVEL,
        V_VOLTAGE,
        V_CURRENT
    }

    public enum StreamSubType : byte
    {
        ST_FIRMWARE_CONFIG_REQUEST,
        ST_FIRMWARE_CONFIG_RESPONSE,
        ST_FIRMWARE_REQUEST,
        ST_FIRMWARE_RESPONSE,
        ST_SOUND,
        ST_IMAGE
    }
}
