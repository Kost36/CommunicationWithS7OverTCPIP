using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;

namespace CommunicationWithS7OverTCPIP
{
    /// <summary>
    /// Класс взаимодействия с ПЛК S7 Поверх сокетов TCPIP
    /// </summary>
    public class SocketCommunicationWithS71200
    {
        #region Конструкторы
        /// <summary>
        /// Инициализация класса взаимодействия с ПЛК
        /// Примечания:
        /// Для активации обработки пользовательских сообщений (Реализации интерфейса IS71200Msg) необходимо передать ссылку на сборку, в которой реализованны классы
        /// </summary>
        /// <param name="ipAddressPC"> Ip адрес ПЛК </param>
        /// <param name="timeOutRead"> Таймаут чтения </param>
        /// <param name="portIn"> Порт входящего соединения </param>
        /// <param name="portOut"> Порт исходящего соединения </param>
        /// <param name="assembly"> Сборка, в которой реализованы классы взаимодействия с ПЛК (Наследники IS71200Msg) </param>
        public SocketCommunicationWithS71200(string ipAddressPC, int timeOutRead = 20, int portIn = 3000, int portOut = 3001, Assembly assembly = null)
        {
            _assembly = assembly;
            _timeOutRead = timeOutRead;
            _portIn = portIn;
            _portOut = portOut;
            _ipAddressPC = ipAddressPC;
            if (IPAddress.TryParse(_ipAddressPC, out _iPAddress) == false)
            {
                //Вызвать событие, на которое будет подписываться клиентский код
                Debug.WriteLine("Приведение Ip адреса невозможно");
            }
            _threadIn = new Thread(StartIn);
            _threadIn.Priority = ThreadPriority.Highest;
            _threadIn.Start();
            _threadOut = new Thread(StartOut);
            _threadOut.Priority = ThreadPriority.Highest;
            _threadOut.Start();
            _threadWork = new Thread(Work);
            _threadWork.Priority = ThreadPriority.Highest;
            _threadWork.Start();
        }
        #endregion

        #region Константы
        /// <summary>
        /// Строка начала сообщения
        /// </summary>
        private const string _startMsg = "^ST^";
        /// <summary>
        /// Строка начала идентификатора данных в сообщении
        /// </summary>
        private const string _dataIdMsg = "^ID^";
        /// <summary>
        /// Строка начала данных в сообщении
        /// </summary>
        private const string _dataMsg = "^DT^";
        /// <summary>
        /// Строка завершения сообщения
        /// </summary>
        private const string _endMsg = "^EN^";
        /// <summary>
        /// Прификс, при отправке в ПЛК
        /// </summary>
        private string _prifixToPLC = "##";
        #endregion

        #region Поля
        private string _ipAddressPC;
        private IPAddress _iPAddress;
        private bool _cmdDispose;
        private Thread _threadWork;
        private Assembly _assembly;
        /// <summary>
        /// Типы реализаций IS71200Msg;
        /// Где:
        /// Key => MsgType
        /// Value => TypeImplementationClass
        /// </summary>
        private Dictionary<int, Type> _dictionaryMsgClass = new Dictionary<int, Type>();
        private bool _isConnected;

        #region Входящее соединение
        private int _portIn;
        private bool _cmdDisposeIn;
        private Thread _threadIn;
        private Socket _socketIn = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private Socket _clientIn = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private bool _clientInOk;
        List<string> _msgsTest = new List<string>();
        string _strFromSockedOld = "";
        #endregion

        #region Исходящее соединение
        private int _portOut;
        private bool _cmdDisposeOut;
        private Thread _threadOut;
        private Socket _socketOut = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private Socket _clientOut = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private bool _clientOutOk;
        private Queue<IS71200Msg> _queueMsgOut = new Queue<IS71200Msg>();
        #endregion

        private StepComm _stepComm = StepComm.None;
        private int _timeOutRead;
        private int _timeMaxRead;

        private string[] _msgSeparetors = new string[] { _dataIdMsg, _dataMsg, _endMsg };
        #endregion

        #region Свойства
        /// <summary>
        /// Связь с ПЛК S71200
        /// </summary>
        public bool IsConnected { get; set; }
        /// <summary>
        /// Ошибка конфигурации
        /// </summary>
        public bool ErrorConfiguration { get; set; }
        //{
        //    get => _isConnected;
        //    set => Set(ref _isConnected, value);
        //}
        #endregion

        #region События
        /// <summary>
        /// Делегат входящего сообщения
        /// </summary>
        /// <param name="s71200Msg"> Класс сообщения </param>
        public delegate void MsgFromPlcHandler(IS71200Msg s71200Msg);
        /// <summary>
        /// Событие о новом сообщении
        /// </summary>
        public event MsgFromPlcHandler NotifyNewMsg;
        #endregion

        #region Публичные методы
        /// <summary>
        /// Отправить сообщение в ПЛК
        /// </summary>
        /// <param name="s71200Msg"> Экземпляр реализации сообщения </param>
        /// <param name="queueSendMsgCount"> Очередь готовых к отправке сообщений </param>
        /// <returns> true(OK) / false(NotConnect)</returns>
        public bool SendMsg(IS71200Msg s71200Msg, out int queueSendMsgCount)
        {
            //Входные проверки
            if (s71200Msg.MsgType == 0)
            {
                queueSendMsgCount = _queueMsgOut.Count;
                return false;
            }

            //Выполнение действия
            lock (_queueMsgOut)
            {
                if (_clientOutOk)
                {
                    _queueMsgOut.Enqueue(s71200Msg);
                    //Debug.WriteLine($"В очередь на отправку добавлено сообщение: Тип: {s71200Msg.MsgType}; Id данных: {s71200Msg.DataId}; Данные: {s71200Msg.Data}");
                } //Если связь есть
                queueSendMsgCount = _queueMsgOut.Count;
                return _clientOutOk;
            }
        }
        /// <summary>
        /// Отключиться от ПЛК
        /// </summary>
        public void Disconnect()
        {
            try
            {
                _socketOut.Close();
                _socketIn.Close();
                _clientIn.Close();
                _clientOut.Close();
                _cmdDispose = true;
                _cmdDisposeIn = true;
                _cmdDisposeOut = true;
                _threadIn = null;
                _threadOut = null;
                _threadWork = null;
            }
            catch (Exception ex)
            {
                //Вызвать событие, на которое будет подписываться клиентский код
                //Debug.WriteLine(ex.Message);
            }
        }
        /// <summary>
        /// Подключиться к ПЛК
        /// </summary>
        public bool Connect()
        {
            if (ErrorConfiguration)
            {
                return false;
            }
            _threadIn = new Thread(StartIn);
            _threadIn.Priority = ThreadPriority.Highest;
            _threadIn.Start();
            _threadOut = new Thread(StartOut);
            _threadOut.Priority = ThreadPriority.Highest;
            _threadOut.Start();
            _threadWork = new Thread(Work);
            _threadWork.Priority = ThreadPriority.Highest;
            _threadWork.Start();
            return true;
        }
        #endregion

        #region Приватные методы
        /// <summary>
        /// Запуск входящего соединения
        /// </summary>
        private void StartIn()
        {
            while (true)
            {
                try
                {
                    IPEndPoint ipPoint = new IPEndPoint(_iPAddress, _portIn); //Получаем конечную точку для запуска сокета
                    _socketIn.Bind(ipPoint); //Связываем сокет с локальной конечной точкой сервера, по которой будем принимать входные соединения

                    // Начинаем прослушивание
                    _socketIn.Listen(50); //Очередь из 50 подключений
                    while (true)
                    {
                        Socket socket = _socketIn.Accept(); //При наличии подключения поднимаем сокет
                        lock (_clientIn)
                        {
                            _clientIn = socket;
                            _clientInOk = true;
                        }
                        Thread.Sleep(5);
                    } //Постоянно
                }
                catch (Exception ex)
                {
                    //Вызвать событие, на которое будет подписываться клиентский код
                    //Debug.WriteLine($"Исключение при подключении StartIn SocketCommunicationWithS71200 { System.DateTime.Now.Ticks / 10000} Ms");
                }
            }
        }
        /// <summary>
        /// Запуск исходящего соединения
        /// </summary>
        private void StartOut()
        {
            while (true)
            {
                try
                {
                    IPEndPoint ipPoint = new IPEndPoint(_iPAddress, _portOut); //Получаем конечную точку для запуска сокета
                    _socketOut.Bind(ipPoint); //Связываем сокет с локальной конечной точкой сервера, по которой будем принимать входные соединения

                    // Начинаем прослушивание
                    _socketOut.Listen(50); //Очередь из 50 подключений
                    while (true)
                    {
                        Socket socket = _socketOut.Accept(); //При наличии подключения поднимаем сокет
                        lock (_clientOut)
                        {
                            _clientOut = socket;
                            _clientOutOk = true;
                        }
                        Thread.Sleep(5);
                    } //Постоянно
                }
                catch (Exception ex)
                {
                    //Вызвать событие, на которое будет подписываться клиентский код
                    //Debug.WriteLine($"Исключение при подключении StartOut SocketCommunicationWithS71200 { System.DateTime.Now.Ticks / 10000} Ms");
                }
            }
        }

        /// <summary>
        /// Работа класса
        /// </summary>
        private void Work()
        {
            try
            {
                _dictionaryMsgClass.Clear();
                CheckAssembly(_assembly, ref _dictionaryMsgClass); //Анализ сборки
            }
            catch(Exception ex)
            {

            } //Анализ сборки

            while (true)
            {
                if (_clientInOk & _clientOutOk)
                {
                    IsConnected = true;

                    if(_stepComm == StepComm.None)
                    {
                        _stepComm = StepComm.Read; //Читаем
                        lock (_queueMsgOut)
                        {
                            if (_queueMsgOut.Count > 0)
                            {
                                _stepComm = StepComm.Write;
                            }
                        } //Если есть данные на запись. Пишем. вместо чтения
                    }

                    switch (_stepComm)
                    {
                        case StepComm.Read:
                            CmdRead();
                            break;
                        case StepComm.Write:
                            CmdWrite();
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    IsConnected = false;
                    _stepComm = StepComm.None;
                }
                Thread.Sleep(5);
                continue;
            }
        }

        /// <summary>
        /// Чтение из сокета
        /// </summary>
        private void CmdRead()
        {
            DateTime dateTimeTimeStartRead = DateTime.Now;
            DateTime dateTimeTimeOutRead = DateTime.Now.AddMilliseconds(_timeOutRead);
            int timeRead = 0;
            while (true)
            {
                try
                {
                    lock (_clientIn)
                    {
                        if (_clientIn == null)
                        {
                            //Debug.WriteLine($"SocketCommunicationWithS71200 CmdRead _clientIn == null { DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond} Ms");
                            _stepComm = StepComm.None;
                            return;
                        }
                        if (_clientIn.Available > 0) //Если в сокете есть данные
                        {
                            byte[] bytes = new byte[_clientIn.Available]; //Подготовка массива под доступное в соките кол-во байт
                            int readBytesCount = _clientIn.Receive(bytes); //Читаем данные
                            string strFromSocked = (Encoding.ASCII.GetString(bytes)); //Декодируем прочитанные байты, соединяя с остатком с прошлого раза
                            strFromSocked = strFromSocked.Substring(2, strFromSocked.Length-2);//Обрежем первые 2 символа из ПЛК
                            strFromSocked = strFromSocked.Replace("\0","");
                            //_strFromSockedOld += strFromSocked; //ПЛК при отправке след сообщения перезаписывает определенную длинну сообщения, но данные по остальной части остаются такими же
                            _strFromSockedOld += strFromSocked; 
                            _msgsTest.Add(strFromSocked);
                            //Debug.WriteLine($"Строка перед обработкой {_strFromSockedOld}");
                            _strFromSockedOld = GetMsgsFromInputString(_strFromSockedOld, out Queue<IS71200Msg> _); //Вытягиваем из строки сообщения
                            //Debug.WriteLine($"Строка после обработки {_strFromSockedOld}");
                            if (timeRead > _timeMaxRead)
                            {
                                _timeMaxRead = timeRead;
                            }
                            //Debug.WriteLine($"SocketCommunicationWithS71200 CmdRead { DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond} Ms");
                        } //Читаем данные
                    }
                    //Thread.Sleep(5);
                    timeRead = (int)((DateTime.Now.Ticks - dateTimeTimeStartRead.Ticks) / TimeSpan.TicksPerMillisecond);
                    if (dateTimeTimeOutRead < DateTime.Now)
                    {
                        //Console.WriteLine($"SocketCommunicationWithS71200 Таймаут Чтения: { System.DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond} Ms");
                        _stepComm = StepComm.None;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    //Debug.WriteLine($"Исключение при чтении SocketCommunicationWithS71200 { DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond} Ms");
                    _stepComm = StepComm.None;
                    return;
                }
            }
        }
        /// <summary>
        /// Запись в сокет
        /// </summary>
        private void CmdWrite()
        {
            try
            {
                lock (_clientOut)
                {
                    if (_clientOut == null)
                    {
                        //Debug.WriteLine($"SocketCommunicationWithS71200 CmdWrite _clientOut == null { DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond} Ms");
                        _stepComm = StepComm.None;
                        return;
                    }
                    if (_clientOut.Connected == false) //Если соеденен
                    {
                        //Debug.WriteLine($"При записи SocketCommunicationWithS71200 _clientOut.Connected { DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond} Ms");
                        _stepComm = StepComm.None;
                        return;
                    } //Читаем данные

                    List<string> msgs = new List<string>(); //Список строк
                    string strOnSend = "";
                    lock (_queueMsgOut)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            if (_queueMsgOut.Count > 0)
                            {
                                IS71200Msg s71200Msg = _queueMsgOut.Dequeue(); //Получили сообщение
                                string msg = $"{_startMsg}{s71200Msg.MsgType}{_dataIdMsg}{s71200Msg.DataId}{_dataMsg}{s71200Msg.Data}{_endMsg}"; //Собрали строку сообщения
                                //Debug.WriteLine($"Собрано сообщение: {msg} Время: {DateTime.Now}:{DateTime.Now.Millisecond} ");

                                msgs.Add(msg); //Добавили
                            }
                            else
                            {
                                break;
                            }
                        } //Собираем до 5 сообщений за одно отправление
                    } //Собираем до 5 сообщений
                    if (msgs.Count==0)
                    {
                        _stepComm = StepComm.None;
                        return;
                    } //Если отправлять нечего
                    else
                    {
                        strOnSend = _prifixToPLC; //Прификс
                        foreach (string msg in msgs)
                        {
                            strOnSend += msg;
                        } //Соеденияем сообщения
                    } //Если есть отправления
                    byte[] bytesOnSend = Encoding.ASCII.GetBytes(strOnSend); //подготовка данных
                    _clientOut.Send(bytesOnSend); //Отправка данных в сокет
                    //Debug.WriteLine($"SocketCommunicationWithS71200 Отправлено: {strOnSend} { System.DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond} Ms.Максимальное время чтения после записи: {_timeMaxRead} \r\n  \r\n");
                    _stepComm = StepComm.None;
                }
            }
            catch (Exception ex)
            {
                //Debug.WriteLine($"Исключение при записи SocketCommunicationWithS71200 { System.DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond} Ms");
                return;
            }
        }

        /// <summary>
        /// Парсинг входной строки из сокета
        /// </summary>
        /// <param name="inputString"> Данные прочитанные из сокета </param>
        /// <returns> Остаток (если во входных данных есть наличие части сообщения) </returns>
        public string GetMsgsFromInputString(string inputString, out Queue<IS71200Msg> s71200InputMsgs)
        {
            s71200InputMsgs = new Queue<IS71200Msg>();
            string[] msgs = inputString.Split(_startMsg); //Разделяем данные на сообщения 
            foreach (string str in msgs)
            {
                if (str.Contains(_dataMsg) == false)
                {
                    continue;
                } //Если в сообщении нету идентификатора данных
                if (str.Contains(_dataIdMsg) == false)
                {
                    continue;
                } //Если в сообщении нету данных
                if (str.Contains(_endMsg) == false)
                {
                    continue;
                } //Если в сообщении нету завершения
                string[] msgData = str.Split(_msgSeparetors, options: StringSplitOptions.None); //Разделяем данные на сообщения 
                if (msgData.Length < 3)
                {
                    continue;
                } //Если сообщение извлечено некорректно
                int.TryParse(msgData[0], out int msgType); //Тип сообщения
                int.TryParse(msgData[1], out int idData); //Идентификатор данных
                string data = msgData[2]; //Данные
                ActionMsg(msgType, idData, data, ref s71200InputMsgs);
            } //Проходимся по массиву и парсим сообщения
            if (msgs.Length > 0)
            {
                string endMsg = msgs[msgs.Length - 1];
                int indexEndMsg = endMsg.IndexOf(_endMsg);
                if (indexEndMsg + _endMsg.Length < endMsg.Length)
                {
                    string msg = msgs[msgs.Length - 1];
                    string outPut = msg.Substring(indexEndMsg + _endMsg.Length, msg.Length - (indexEndMsg + _endMsg.Length));
                    return outPut;
                } //Если последнее сообщение не имеет завершения
            } //Вернем остаток
            return "";
        }
        /// <summary>
        /// Анализ сборки (поиск реализаций IS71200Msg)
        /// </summary>
        public void CheckAssembly(Assembly assembly, ref Dictionary<int, Type> dictionaryTypes)
        {
            if (assembly==null)
            {
                return;
            }
            Type typeIS71200Msg = typeof(IS71200Msg); //Получим тип интерфейса
            Type[] types = assembly.GetTypes(); //Получаем все типы сборки 
            List<Type> typesS71200Msgs = types.Where(t => typeIS71200Msg.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract).ToList(); //Получим все системы в сборке
            foreach (Type typeS71200Msg in typesS71200Msgs)
            {
                IS71200Msg s71200Msg = (IS71200Msg)Activator.CreateInstance(typeS71200Msg); //Создаем
                Type type = s71200Msg.GetType(); //Получаем тип
                AttributeMsgType attributeMsgType = type.GetCustomAttribute<AttributeMsgType>();
                if (attributeMsgType == null)
                {
                    continue;
                } //Если аттрибута нету
                if (_dictionaryMsgClass.ContainsKey(attributeMsgType.MsgType))
                {
                    ErrorConfiguration = true; //Ошибка конфигурации
                    Disconnect(); //Остановим общение
                    return;
                } //Если в словаре уже есть класс с таким типом сообщения
                _dictionaryMsgClass.Add(attributeMsgType.MsgType, typeS71200Msg);
            } //Пройдемся по всем системам 
        }
        private void ActionMsg(int msgType, int idData, string data, ref Queue<IS71200Msg> s71200InputMsgs)
        {
            try
            {
                //Debug.WriteLine($"Получено сообщение: {msgType}; ID Данных {idData}; Данные {data}; Время: {DateTime.Now}:{DateTime.Now.Millisecond} ");
                IS71200Msg s71200Msg; //Объект
                if (_dictionaryMsgClass.TryGetValue(msgType, out Type type))
                {
                    s71200Msg = (IS71200Msg)Activator.CreateInstance(type);
                } //Если в словаре есть тип с данным идентификатором сообщения
                else
                {
                    s71200Msg = Activator.CreateInstance<S71200MsgBaseNotify>();
                }
                //Зададим параметры
                s71200Msg.MsgType = msgType;
                s71200Msg.DataId = idData;
                s71200Msg.Data = data;
                s71200InputMsgs.Enqueue(s71200Msg);
                if (s71200Msg.UseAction)
                {
                    s71200Msg.ActionInputMsg(); //Вызов метода
                } //Если есть флаг вызова метода
                else
                {
                    NotifyNewMsg?.Invoke(s71200Msg); //Вызов события
                } //Если нету флага вызова метода
            }
            catch(Exception ex)
            {
                //Debug.WriteLine("Исключение ActionMsg в классе SocketCommunicationWithS71200");
            }
            
        }
        #endregion

        /// <summary>
        /// Состояние
        /// </summary>
        public enum StepComm
        {
            None,
            Read,
            Write
        }
    }
}

//private static string _infoUse = GetInfoUse();
//public static string InfoUse { get => _infoUse; }
//private static string GetInfoUse()
//{
//    string strInfo = "Данный класс после инициализации поднимает два TcpIp сокета (входящий и исходящий), которые подключаются к заданному Ip адресу и заданным портам \r\n";
//    strInfo = +"";
//    strInfo = +"";
//    strInfo = +"";
//    strInfo = +"";
//    return strInfo
//}