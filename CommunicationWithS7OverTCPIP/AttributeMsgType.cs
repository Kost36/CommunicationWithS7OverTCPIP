using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommunicationWithS7OverTCPIP
{
    /// <summary>
    /// аттрибут к классам S71200Msg (Наследники IS71200Msg)
    /// </summary>
    public class AttributeMsgType : Attribute
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="msgType"> Тип сообщения (идентификатор сообщения/команды/задачи) </param>
        public AttributeMsgType(int msgType)
        {
            MsgType = msgType;
        }
        /// <summary>
        /// Тип сообщения
        /// </summary>
        public int MsgType { get; set; }
    }
}