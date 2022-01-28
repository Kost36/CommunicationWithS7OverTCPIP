using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommunicationWithS7OverTCPIP
{
    /// <summary>
    /// Интерфейс сообщения ПЛК
    /// Для реализации приходящих из ПЛК сообщений необходимо реализовать:
    /// 1) Публичный конструктор без параметров (Для создания класса)
    /// 2) Класс должен иметь аттрибут AttributeMsgType с заданным идентификатором типа сообщения
    /// </summary>
    public interface IS71200Msg
    {
        /// <summary>
        /// Тип сообщения
        /// </summary>
        public int MsgType { get; set; }
        /// <summary>
        /// Идентификатор данных
        /// </summary>
        public int DataId { get; set; }
        /// <summary>
        /// Данные
        /// </summary>
        public string Data { get; set; }
        /// <summary>
        /// Флаг необходимости вызова метода ActionInputMsg();
        /// Если реализована инициализация (UseAction=true), то произойдет вызов метода ActionInputMsg();
        /// Если инициализация не реализована, то произойдет вызов события NotifyNewMsg у класса SocketCommunicationWithS7120;
        /// </summary>
        public bool UseAction { get; }
        /// <summary>
        /// Действие по входящему сообщению из ПЛК (вызывается, при получении сообщения из ПЛК)
        /// </summary>
        public void ActionInputMsg();
    }

    /// <summary>
    /// Базовый объект реализации интерфейса
    /// </summary>
    public class S71200MsgBaseAction : IS71200Msg
    {
        public S71200MsgBaseAction() { }
        public S71200MsgBaseAction(int msgType, int dataId, string data)
        {
            MsgType = msgType;
            DataId = dataId;
            Data = data;
        }
        public int MsgType { get; set; }
        public int DataId { get; set; }
        public string Data { get; set; }
        public bool UseAction { get; } = true;
        public void ActionInputMsg()
        {
            Console.WriteLine("Вызов ActionInputMsg у S71200MsgBaseAction");
        }
    }
    /// <summary>
    /// Базовый объект реализации интерфейса
    /// </summary>
    public class S71200MsgBaseNotify : IS71200Msg
    {
        public S71200MsgBaseNotify() { }
        public S71200MsgBaseNotify(int msgType, int dataId, string data)
        {
            MsgType = msgType;
            DataId = dataId;
            Data = data;
        }
        public int MsgType { get; set; }
        public int DataId { get; set; }
        public string Data { get; set; }
        public bool UseAction { get; }
        public void ActionInputMsg()
        {
            Console.WriteLine("Вызов ActionInputMsg у S71200MsgBaseNotify");
        }
    }

    /// <summary>
    /// Управление подсветкой камер
    /// </summary>
    [AttributeMsgType(1)]
    public class S71200MsgLightControl : IS71200Msg
    {
        public S71200MsgLightControl() { }
        public S71200MsgLightControl(int msgType, int dataId, bool data)
        {
            MsgType = msgType;
            DataId = dataId;
            Data = PLCConvert.ToString(data);
        }
        public int MsgType { get; set; }
        public int DataId { get; set; }
        public string Data { get; set; }
        public bool UseAction { get; } = true;
        public void ActionInputMsg()
        {
            Console.WriteLine("Вызов ActionInputMsg у S71200MsgLightControl");
        }
    }
    /// <summary>
    /// Ответ об управлении подсветкой камер
    /// </summary>
    [AttributeMsgType(2)]
    public class S71200MsgLightControlResp : IS71200Msg
    {
        public S71200MsgLightControlResp() { }
        public S71200MsgLightControlResp(int msgType, int dataId, string data)
        {
            MsgType = msgType;
            DataId = dataId;
            Data = data;
        }
        public int MsgType { get; set; }
        public int DataId { get; set; }
        public string Data { get; set; }
        public bool UseAction { get; } = true;
        public void ActionInputMsg()
        {
            if (MsgType == 2)
            {
                if (DataId == 1)
                {
                    PLCTags.Light.Light1 = PLCConvert.ToBool(Data);
                }
                else if (DataId == 2)
                {
                    PLCTags.Light.Light2 = PLCConvert.ToBool(Data);
                }
                else if(DataId == 3)
                {
                    PLCTags.Light.Light3 = PLCConvert.ToBool(Data);
                }
                else if(DataId == 4)
                {
                    PLCTags.Light.Light4 = PLCConvert.ToBool(Data);
                }
                else if(DataId == 5)
                {
                    PLCTags.Light.Light5 = PLCConvert.ToBool(Data);
                }
            }
        }
    }
    /// <summary>
    /// Значение датчика
    /// </summary>
    [AttributeMsgType(4)]
    public class S71200MsgSensorValue : IS71200Msg
    {
        public S71200MsgSensorValue() { }
        public S71200MsgSensorValue(int msgType, int dataId, string data)
        {
            MsgType = msgType;
            DataId = dataId;
            Data = data;
        }
        public int MsgType { get; set; }
        public int DataId { get; set; }
        public string Data { get; set; }
        public bool UseAction { get; } = true;
        public void ActionInputMsg()
        {
            if (MsgType == 4)
            {
                try
                {
                    float value = ((float)Convert.ToInt32(Data))/10;
                    if (DataId == 1)
                    {
                        PLCTags.Sensor_Lvl_Overflow.Sensor_1 = value;
                    }
                    else if(DataId == 2)
                    {
                        PLCTags.Sensor_Lvl_Overflow.Sensor_2 = value;
                    }
                    else if (DataId == 3)
                    {
                        PLCTags.Sensor_Lvl_Overflow.Sensor_3 = value;
                    }
                    else if (DataId == 4)
                    {
                        PLCTags.Sensor_Lvl_Overflow.Sensor_4 = value;
                    }
                    else if (DataId == 5)
                    {
                        PLCTags.Sensor_Lvl_Overflow.Sensor_5 = value;
                    }
                }
                catch
                {
                    //Игнор
                }
            }
        }
    }
    /// <summary>
    /// Статус ПЛК
    /// </summary>
    [AttributeMsgType(5)]
    public class S71200MsgStatusPLC : IS71200Msg
    {
        public S71200MsgStatusPLC() { }
        public S71200MsgStatusPLC(int msgType, int dataId, string data)
        {
            MsgType = msgType;
            DataId = dataId;
            Data = data;
        }
        public int MsgType { get; set; }
        public int DataId { get; set; }
        public string Data { get; set; }
        public bool UseAction { get; } = true;
        public void ActionInputMsg()
        {
            if (MsgType == 5)
            {
                if (DataId == 0)
                {
                    try
                    {
                        PLCTags.LightColumn.Green = PLCConvert.ToBool(Data[0].ToString());
                        PLCTags.LightColumn.Yellow = PLCConvert.ToBool(Data[1].ToString());
                        PLCTags.LightColumn.Red = PLCConvert.ToBool(Data[2].ToString());
                    }
                    catch
                    {
                        //Игнор
                    }
                }
            }
        }
    }
    /// <summary>
    /// Сброс аварий
    /// </summary>
    [AttributeMsgType(6)]
    public class S71200MsgRstAlarms : IS71200Msg
    {
        public S71200MsgRstAlarms()
        {
            MsgType = 6;
            DataId = 0;
            Data = Convert.ToInt32(true).ToString();
        }
        public int MsgType { get; set; }
        public int DataId { get; set; }
        public string Data { get; set; }
        public bool UseAction { get; } = true;
        public void ActionInputMsg()
        {
        }
    }
    /// <summary>
    /// Возникновение аварии
    /// </summary>
    [AttributeMsgType(7)]
    public class S71200MsgAlarm : IS71200Msg
    {
        public S71200MsgAlarm() { }
        public S71200MsgAlarm(int msgType, int dataId, string data)
        {
            MsgType = msgType;
            DataId = dataId;
            Data = data;
        }
        public int MsgType { get; set; }
        public int DataId { get; set; }
        public string Data { get; set; }
        public bool UseAction { get; } = true;
        public void ActionInputMsg()
        {
            if (MsgType == 7)
            {
                if(PLCTags.AlarmsInPlc.Alarms.TryGetValue(DataId, out string alarmMsg))
                {
                    bool value = PLCConvert.ToBool(Data);
                    if (value)
                    {
                        Debug.WriteLine($"Возникновение аварии. {alarmMsg}");
                    }
                    else
                    {
                        Debug.WriteLine($"Нормализация аварии. {alarmMsg}");
                    }
                }
            }
        }
    }


    /// <summary>
    /// Класс перемнных ПЛК
    /// </summary>
    public static class PLCTags
    {
        /// <summary>
        /// Соединение установлено
        /// </summary>
        public static bool IsConnect { get; set; }
        /// <summary>
        /// Подсветка камер
        /// </summary>
        public static class Light
        {
            public static bool Light1 { get; set; }
            public static bool Light2 { get; set; }
            public static bool Light3 { get; set; }
            public static bool Light4 { get; set; }
            public static bool Light5 { get; set; }
        }
        /// <summary>
        /// Световая колона (Статус)
        /// </summary>
        public static class LightColumn
        {
            public static bool Green { get; set; }
            public static bool Yellow { get; set; }
            public static bool Red { get; set; }
        }
        /// <summary>
        /// Датчики перелива
        /// </summary>
        public static class Sensor_Lvl_Overflow
        {
            public static float Sensor_1 { get; set; }
            public static float Sensor_2 { get; set; }
            public static float Sensor_3 { get; set; }
            public static float Sensor_4 { get; set; }
            public static float Sensor_5 { get; set; }
        }
        /// <summary>
        /// Класс описывающий аварии ПЛК
        /// </summary>
        public static class AlarmsInPlc
        {
            /// <summary>
            /// Словарь аварийных сообщений, соответстующий авариям в ПЛК
            /// </summary>
            public static Dictionary<int, string> Alarms { get; } = new Dictionary<int, string>()
            {
                { 1, "Обрыв датчика перелива пены №1" }, //Обрыв датчика
                { 2, "Обрыв датчика перелива пены №2" }, //Обрыв датчика
                { 3, "Обрыв датчика перелива пены №3" }, //Обрыв датчика
                { 4, "Обрыв датчика перелива пены №4" }, //Обрыв датчика
                { 5, "Обрыв датчика перелива пены №5" }, //Обрыв датчика
                { 6, "Переполнение очереди входных сообщений"},
                { 7, "Переполнение очереди исходящих сообщений"},
                { 8, "Переполнение строки входного сообщения"},
                { 9, "Переполнение очереди исходящего сообщения"},
            };
        }
    }

    public static class PLCConvert
    {
        public static bool ToBool(string str)
        {
            if (str == "1") return true;
            else return false;
        }
        public static string ToString(bool value)
        {
            if (value) { return "1"; }
            else { return "0"; }
        }
    }
}