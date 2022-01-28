using Microsoft.VisualStudio.TestTools.UnitTesting;
using CommunicationWithS7OverTCPIP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

namespace CommunicationWithS7OverTCPIP.Tests
{
    [TestClass()]
    public class SocketCommunicationWithS71200Tests
    {
        static SocketCommunicationWithS71200 _plc;

        [TestMethod()]
        public void Test00Init()
        {
            _plc = new SocketCommunicationWithS71200("192.168.250.100", assembly: typeof(SocketCommunicationWithS71200).Assembly);

        }

        [TestMethod()]
        public void Test01GetMsgsFromInputString()
        {
            IS71200Msg s71200Msg;
            string msgInput = "";
            string msgRemains = "";
            Queue<IS71200Msg> queueMsgInput = new Queue<IS71200Msg>();

            msgInput = "ppp^ST^1^ID^1^DT^1^EN^ppp";
            msgRemains = _plc.GetMsgsFromInputString(msgInput, out queueMsgInput);
            Assert.IsTrue(queueMsgInput.Count==1);
            Assert.IsTrue(msgRemains == "ppp");
            s71200Msg = queueMsgInput.Dequeue();
            Assert.IsTrue(s71200Msg.MsgType==1);
            Assert.IsTrue(s71200Msg.DataId == 1);
            Assert.IsTrue(s71200Msg.Data == "1");

            msgInput = "ppp^ST^5^ID^3^DT^4^EN^ppp";
            msgRemains = _plc.GetMsgsFromInputString(msgInput, out queueMsgInput);
            Assert.IsTrue(queueMsgInput.Count == 1);
            Assert.IsTrue(msgRemains == "ppp");
            s71200Msg = queueMsgInput.Dequeue();
            Assert.IsTrue(s71200Msg.MsgType == 5);
            Assert.IsTrue(s71200Msg.DataId == 3);
            Assert.IsTrue(s71200Msg.Data == "4");

            msgInput = "^ST^99^ID^2005^DT^Krg^EN^";
            msgRemains = _plc.GetMsgsFromInputString(msgInput, out queueMsgInput);
            Assert.IsTrue(queueMsgInput.Count == 1);
            Assert.IsTrue(msgRemains == "");
            s71200Msg = queueMsgInput.Dequeue();
            Assert.IsTrue(s71200Msg.MsgType == 99);
            Assert.IsTrue(s71200Msg.DataId == 2005);
            Assert.IsTrue(s71200Msg.Data == "Krg");

            msgInput = "^ST^10^ID^2^DT^1^EN^^ST^8^ID^2^DT^1^EN^";
            msgRemains = _plc.GetMsgsFromInputString(msgInput, out queueMsgInput);
            Assert.IsTrue(queueMsgInput.Count == 2);
            Assert.IsTrue(msgRemains == "");
            s71200Msg = queueMsgInput.Dequeue();
            Assert.IsTrue(s71200Msg.MsgType == 10);
            Assert.IsTrue(s71200Msg.DataId == 2);
            Assert.IsTrue(s71200Msg.Data == "1");
            s71200Msg = queueMsgInput.Dequeue();
            Assert.IsTrue(s71200Msg.MsgType == 8);
            Assert.IsTrue(s71200Msg.DataId == 2);
            Assert.IsTrue(s71200Msg.Data == "1");

            msgInput = "sdfsdf^ST^10^ID^2^DT^1^EN^sfsdf^ST^8^ID^2^DT^1^EN^sdfsdf";
            msgRemains = _plc.GetMsgsFromInputString(msgInput, out queueMsgInput);
            Assert.IsTrue(queueMsgInput.Count == 2);
            Assert.IsTrue(msgRemains == "sdfsdf");
            s71200Msg = queueMsgInput.Dequeue();
            Assert.IsTrue(s71200Msg.MsgType == 10);
            Assert.IsTrue(s71200Msg.DataId == 2);
            Assert.IsTrue(s71200Msg.Data == "1");
            s71200Msg = queueMsgInput.Dequeue();
            Assert.IsTrue(s71200Msg.MsgType == 8);
            Assert.IsTrue(s71200Msg.DataId == 2);
            Assert.IsTrue(s71200Msg.Data == "1");

            msgInput = "ppp^ST^1^ID^1^DT^1^EN^pppppp^ST^1^ID^2^DT^1^EN^pppppp^ST^1^ID^3^DT^1^EN^pppppp^ST^1^ID^4^DT^1^EN^ppppppppp^ST^1^ID^5^DT^1^EN^ppp";
            msgRemains = _plc.GetMsgsFromInputString(msgInput, out queueMsgInput);
            Assert.IsTrue(queueMsgInput.Count == 5);
            Assert.IsTrue(msgRemains == "ppp");
            s71200Msg = queueMsgInput.Dequeue();
            Assert.IsTrue(s71200Msg.MsgType == 1);
            Assert.IsTrue(s71200Msg.DataId == 1);
            Assert.IsTrue(s71200Msg.Data == "1");
            s71200Msg = queueMsgInput.Dequeue();
            Assert.IsTrue(s71200Msg.MsgType == 1);
            Assert.IsTrue(s71200Msg.DataId == 2);
            Assert.IsTrue(s71200Msg.Data == "1");
            s71200Msg = queueMsgInput.Dequeue();
            Assert.IsTrue(s71200Msg.MsgType == 1);
            Assert.IsTrue(s71200Msg.DataId == 3);
            Assert.IsTrue(s71200Msg.Data == "1");
            s71200Msg = queueMsgInput.Dequeue();
            Assert.IsTrue(s71200Msg.MsgType == 1);
            Assert.IsTrue(s71200Msg.DataId == 4);
            Assert.IsTrue(s71200Msg.Data == "1");
            s71200Msg = queueMsgInput.Dequeue();
            Assert.IsTrue(s71200Msg.MsgType == 1);
            Assert.IsTrue(s71200Msg.DataId == 5);
            Assert.IsTrue(s71200Msg.Data == "1");

            msgInput = "ppp^ST^1^ID^1ppp^ST^7^ID^8^DT^9^EN^pppp";
            msgRemains = _plc.GetMsgsFromInputString(msgInput, out queueMsgInput);
            Assert.IsTrue(queueMsgInput.Count == 1);
            Assert.IsTrue(msgRemains == "pppp");
            s71200Msg = queueMsgInput.Dequeue();
            Assert.IsTrue(s71200Msg.MsgType == 7);
            Assert.IsTrue(s71200Msg.DataId == 8);
            Assert.IsTrue(s71200Msg.Data == "9");
        }

        [TestMethod()]
        public void Test02Communication()
        {
            while (true)
            {
                if (_plc.IsConnected == true)
                {
                    Assert.IsTrue(true);
                    return;
                }
            }
        }

        [TestMethod()]
        public void Test03ControlLight()
        {
            new Thread(() => {
                while (true)
                {
                    _plc.SendMsg(new S71200MsgLightControl(1, 7, true), out int _);
                    Thread.Sleep(500);
                    _plc.SendMsg(new S71200MsgLightControl(1, 6, true), out int _);
                    Thread.Sleep(500);

                    while (true)
                    {
                        if (PLCTags.LightColumn.Red)
                        {
                            _plc.SendMsg(new S71200MsgRstAlarms(), out int _);
                            break;
                        }
                    }

                    while (true)
                    {
                        if (PLCTags.LightColumn.Red == false)
                        {
                            break;
                        }
                    }

                    _plc.SendMsg(new S71200MsgLightControl(1, 7, false), out int _);
                    Thread.Sleep(500);
                    _plc.SendMsg(new S71200MsgLightControl(1, 6, false), out int _);
                    Thread.Sleep(500);
                }
            }).Start();
            Thread.Sleep(30000);
            while (true)
            {
                _plc.SendMsg(new S71200MsgLightControl(1, 1, true), out int _);
                //Debug.WriteLine($"Управление светом 1 {DateTime.Now}");
                while (true)
                {
                    if (PLCTags.Light.Light1 == true)
                    {
                        break;
                    }
                    Thread.Sleep(5);
                }

                _plc.SendMsg(new S71200MsgLightControl(1, 2, true), out int _);
                //Debug.WriteLine($"Управление светом 2 {DateTime.Now}");
                while (true)
                {
                    if (PLCTags.Light.Light2 == true)
                    {
                        break;
                    }
                    Thread.Sleep(5);
                }

                _plc.SendMsg(new S71200MsgLightControl(1, 3, true), out int _);
                //Debug.WriteLine($"Управление светом 3 {DateTime.Now}");
                while (true)
                {
                    if (PLCTags.Light.Light3 == true)
                    {
                        break;
                    }
                    Thread.Sleep(5);
                }

                _plc.SendMsg(new S71200MsgLightControl(1, 4, true), out int _);
                //Debug.WriteLine($"Управление светом 4 {DateTime.Now}");
                while (true)
                {
                    if (PLCTags.Light.Light4 == true)
                    {
                        break;
                    }
                    Thread.Sleep(5);
                }

                _plc.SendMsg(new S71200MsgLightControl(1, 5, true), out int _);
                //Debug.WriteLine($"Управление светом 5 {DateTime.Now}");
                while (true)
                {
                    if (PLCTags.Light.Light5 == true)
                    {
                        break;
                    }
                    Thread.Sleep(5);
                }

                Thread.Sleep(500);
                _plc.SendMsg(new S71200MsgLightControl(1, 1, false), out int _);
                while (true)
                {
                    if (PLCTags.Light.Light1 == false)
                    {
                        break;
                    }
                    Thread.Sleep(5);
                }
                _plc.SendMsg(new S71200MsgLightControl(1, 2, false), out int _);
                while (true)
                {
                    if (PLCTags.Light.Light2 == false)
                    {
                        break;
                    }
                    Thread.Sleep(5);
                }
                _plc.SendMsg(new S71200MsgLightControl(1, 3, false), out int _);
                while (true)
                {
                    if (PLCTags.Light.Light3 == false)
                    {
                        break;
                    }
                    Thread.Sleep(5);
                }
                _plc.SendMsg(new S71200MsgLightControl(1, 4, false), out int _);
                while (true)
                {
                    if (PLCTags.Light.Light4 == false)
                    {
                        break;
                    }
                    Thread.Sleep(5);
                }
                _plc.SendMsg(new S71200MsgLightControl(1, 5, false), out int _);
                while (true)
                {
                    if (PLCTags.Light.Light5 == false)
                    {
                        break;
                    }
                    Thread.Sleep(5);
                }
                Thread.Sleep(500);
            }
        }
    }
}