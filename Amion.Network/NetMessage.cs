using System;
using System.Collections.Generic;
using System.Text;

namespace Amion.Network
{
    public enum MessageType : byte
    {
        Unknown = 0,
        Data = 1,
        IsAlive = 2,
        Verification = 3,
        Ping = 4,
    }

    /// <summary>
    /// Class for creating outgoing network message
    /// </summary>
    public class NetOutMessage
    {
        internal const int HeaderSize = 5;
        protected List<byte> message;

        /// <summary></summary>
        /// <param name="msgType">The type of the message. Defaults to 'MessageType.Data'</param>
        public NetOutMessage(MessageType msgType = MessageType.Data)
        {
            message = new List<byte>();
            message.Add((byte)msgType);
            message.AddRange(BitConverter.GetBytes((int)0));
        }

        protected void FinalizeMessage()
        {
            byte[] msgLength = BitConverter.GetBytes(message.Count - 5);

            for (int i = 0; i < 4; i++)
            {
                message[i + 1] = msgLength[i];
            }
        }

        //---------------------------------------------------------------------
        // Internal methods
        //---------------------------------------------------------------------

        internal byte[] ToArray()
        {
            FinalizeMessage();
            return message.ToArray();
        }

        internal static void DecodeHeader(byte[] header, out MessageType messageType, out int messageLength)
        {
            messageType = (MessageType)header[0];
            messageLength = BitConverter.ToInt32(header, 1);
        }

        //---------------------------------------------------------------------
        // Writers
        //---------------------------------------------------------------------

        /// <summary>
        /// Writes a string at the end of the message using an int for the length and Unicode
        /// </summary>
        public void Write(String data)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(data);
            message.AddRange(BitConverter.GetBytes(bytes.Length));
            message.AddRange(bytes);
        }

        /// <summary>
        /// Writes a short at the end of the message using 2 bytes
        /// </summary>
        public void Write(Int16 data)
        {
            message.AddRange(BitConverter.GetBytes(data));
        }

        /// <summary>
        /// Writes an int at the end of the message using 4 bytes
        /// </summary>
        public void Write(Int32 data)
        {
            message.AddRange(BitConverter.GetBytes(data));
        }

        /// <summary>
        /// Writes a long at the end of the message using 8 bytes
        /// </summary>
        public void Write(Int64 data)
        {
            message.AddRange(BitConverter.GetBytes(data));
        }

        /// <summary>
        /// Writes an unsigned short at the end of the message using 2 bytes
        /// </summary>
        public void Write(UInt16 data)
        {
            message.AddRange(BitConverter.GetBytes(data));
        }

        /// <summary>
        /// Writes an unsigned int at the end of the message using 4 bytes
        /// </summary>
        public void Write(UInt32 data)
        {
            message.AddRange(BitConverter.GetBytes(data));
        }

        /// <summary>
        /// Writes an unsigned long at the end of the message using 8 bytes
        /// </summary>
        public void Write(UInt64 data)
        {
            message.AddRange(BitConverter.GetBytes(data));
        }

        /// <summary>
        /// Writes a bool at the end of the message using a byte
        /// </summary>
        public void Write(bool data)
        {
            message.AddRange(BitConverter.GetBytes(data));
        }

        /// <summary>
        /// Writes a byte at the end of the message using a byte
        /// </summary>
        public void Write(byte data)
        {
            message.Add(data);
        }

        /// <summary>
        /// Writes a series of bytes at the end of the message
        /// </summary>
        public void Write(IEnumerable<byte> data)
        {
            message.AddRange(data);
        }

        /// <summary>
        /// Writes a Guid at the end of the message using 16 bytes
        /// </summary>
        public void Write(Guid data)
        {
            message.AddRange(data.ToByteArray());
        }
    }

    public class NetInMessage
    {
        protected MessageType messageType;
        protected byte[] messageData;
        protected int currentPosition = 0;

        public NetInMessage(MessageType messageType, byte[] messageData)
        {
            this.messageType = messageType;
            this.messageData = messageData;
        }

        public string ReadString()
        {
            int length = BitConverter.ToInt32(messageData, currentPosition);
            string data = Encoding.Unicode.GetString(messageData, currentPosition + sizeof(int), length);

            currentPosition += sizeof(int) + length;

            return data;
        }

        public Int32 ReadInt32()
        {
            Int32 data = BitConverter.ToInt32(messageData, currentPosition);

            currentPosition += sizeof(Int32);

            return data;
        }

        public Int64 ReadInt64()
        {
            Int64 data = BitConverter.ToInt64(messageData, currentPosition);

            currentPosition += sizeof(Int64);

            return data;
        }

        public bool ReadBoolean()
        {
            bool data = BitConverter.ToBoolean(messageData, currentPosition);

            currentPosition++;

            return data;
        }

        public byte ReadByte()
        {
            byte data = messageData[currentPosition];

            currentPosition++;

            return data;
        }

        public byte[] ReadBytes(int amount)
        {
            byte[] data = new byte[amount];

            Buffer.BlockCopy(messageData, currentPosition, data, 0, amount);

            currentPosition += amount;

            return data;
        }

        public ArraySegment<byte> ReadBytesAsSegment(int amount)
        {
            var arraySegment =  new ArraySegment<byte>(messageData, currentPosition, amount);

            currentPosition += amount;

            return arraySegment;
        }

        public Guid ReadGuid()
        {
            const int guidSize = 16;
            byte[] data = new byte[guidSize];
            
            Buffer.BlockCopy(messageData, currentPosition, data, 0, guidSize);

            currentPosition += guidSize;

            return new Guid(data);
        }

        // Other types...

        public MessageType GetMessageType()
        {
            return messageType;
        }

        public byte[] GetMessageData()
        {
            return messageData;
        }
    }
}