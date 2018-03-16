using System;
using System.Collections.Generic;
using System.Text;

namespace Amion.Network
{
    /// <summary>
    /// Type of a NetMessage.
    /// </summary>
    public enum MessageType : byte
    {
        /// <summary>
        /// Should only be unknown when invalid
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Indicates a message containing data defined by the application.
        /// </summary>
        Data = 1,

        /// <summary>
        /// Indicates a message containing no data. Used only for testing if the connection still exist.
        /// </summary>
        IsAlive = 2,

        /// <summary>
        /// Indicates a message containing data for verify the connection.
        /// </summary>
        Verification = 3,

        /// <summary>
        /// Indicates a message containing data for Ping requests. Unimplemented.
        /// </summary>
        Ping = 4,
    }

    /// <summary>
    /// Class for creating an outgoing network message.
    /// </summary>
    public class NetOutMessage
    {
        /// <summary>
        /// The size of the header part of the message (in bytes).
        /// </summary>
        protected const int HeaderSize = 5;

        /// <summary>
        /// A List containing the message.
        /// </summary>
        protected List<byte> message;

        /// <summary></summary>
        /// <param name="msgType">The type of the message. Defaults to 'MessageType.Data'</param>
        public NetOutMessage(MessageType msgType = MessageType.Data)
        {
            message = new List<byte>();
            message.Add((byte)msgType);
            message.AddRange(BitConverter.GetBytes((int)0));
        }

        /// <summary>
        /// Updates the message data length in the message.
        /// </summary>
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

        /// <summary>
        /// Returns the message as a byte array.
        /// </summary>
        public byte[] ToArray()
        {
            FinalizeMessage();
            return message.ToArray();
        }

        /// <summary>
        /// Gets the message header data from a byte array.
        /// </summary>
        /// <param name="header">The array containing the header</param>
        /// <param name="messageType">The type of the message</param>
        /// <param name="messageLength">The length of the message data</param>
        public static void DecodeHeader(byte[] header, out MessageType messageType, out int messageLength)
        {
            messageType = (MessageType)header[0];
            messageLength = BitConverter.ToInt32(header, 1);
        }

        //---------------------------------------------------------------------
        // Writers
        //---------------------------------------------------------------------

        /// <summary>
        /// Writes a string at the end of the message using an int for the length and Unicode.
        /// </summary>
        public void Write(String data)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(data);
            message.AddRange(BitConverter.GetBytes(bytes.Length));
            message.AddRange(bytes);
        }

        /// <summary>
        /// Writes a short at the end of the message using 2 bytes.
        /// </summary>
        public void Write(Int16 data)
        {
            message.AddRange(BitConverter.GetBytes(data));
        }

        /// <summary>
        /// Writes an int at the end of the message using 4 bytes.
        /// </summary>
        public void Write(Int32 data)
        {
            message.AddRange(BitConverter.GetBytes(data));
        }

        /// <summary>
        /// Writes a long at the end of the message using 8 bytes.
        /// </summary>
        public void Write(Int64 data)
        {
            message.AddRange(BitConverter.GetBytes(data));
        }

        /// <summary>
        /// Writes an unsigned short at the end of the message using 2 bytes.
        /// </summary>
        public void Write(UInt16 data)
        {
            message.AddRange(BitConverter.GetBytes(data));
        }

        /// <summary>
        /// Writes an unsigned int at the end of the message using 4 bytes.
        /// </summary>
        public void Write(UInt32 data)
        {
            message.AddRange(BitConverter.GetBytes(data));
        }

        /// <summary>
        /// Writes an unsigned long at the end of the message using 8 bytes.
        /// </summary>
        public void Write(UInt64 data)
        {
            message.AddRange(BitConverter.GetBytes(data));
        }

        /// <summary>
        /// Writes a bool at the end of the message using a byte.
        /// </summary>
        public void Write(bool data)
        {
            message.AddRange(BitConverter.GetBytes(data));
        }

        /// <summary>
        /// Writes a byte at the end of the message using a byte.
        /// </summary>
        public void Write(byte data)
        {
            message.Add(data);
        }

        /// <summary>
        /// Writes a series of bytes at the end of the message.
        /// </summary>
        public void Write(IEnumerable<byte> data)
        {
            message.AddRange(data);
        }

        /// <summary>
        /// Writes a Guid at the end of the message using 16 bytes.
        /// </summary>
        public void Write(Guid data)
        {
            message.AddRange(data.ToByteArray());
        }
    }

    /// <summary>
    /// Class for an incoming network message.
    /// </summary>
    public class NetInMessage
    {
        /// <summary>
        /// The type of the message.
        /// </summary>
        protected MessageType messageType;

        /// <summary>
        /// The data of the message.
        /// </summary>
        protected byte[] messageData;

        /// <summary>
        /// The current position for reads.
        /// </summary>
        protected int readCursor = 0;

        /// <summary></summary>
        /// <param name="messageType">The type of the message</param>
        /// <param name="messageData">The data of the message</param>
        public NetInMessage(MessageType messageType, byte[] messageData)
        {
            this.messageType = messageType;
            this.messageData = messageData;
        }

        /// <summary>
        /// Reads a string from the message and moves the readCursor.
        /// </summary>
        public string ReadString()
        {
            int length = BitConverter.ToInt32(messageData, readCursor);
            string data = Encoding.Unicode.GetString(messageData, readCursor + sizeof(int), length);

            readCursor += sizeof(int) + length;

            return data;
        }

        /// <summary>
        /// Reads an int from the message and moves the readCursor.
        /// </summary>
        public Int32 ReadInt32()
        {
            Int32 data = BitConverter.ToInt32(messageData, readCursor);

            readCursor += sizeof(Int32);

            return data;
        }

        /// <summary>
        /// Reads a long from the message and moves the readCursor.
        /// </summary>
        public Int64 ReadInt64()
        {
            Int64 data = BitConverter.ToInt64(messageData, readCursor);

            readCursor += sizeof(Int64);

            return data;
        }

        /// <summary>
        /// Reads a bool from the message and moves the readCursor.
        /// </summary>
        public bool ReadBoolean()
        {
            bool data = BitConverter.ToBoolean(messageData, readCursor);

            readCursor++;

            return data;
        }

        /// <summary>
        /// Reads a byte from the message and moves the readCursor.
        /// </summary>
        public byte ReadByte()
        {
            byte data = messageData[readCursor];

            readCursor++;

            return data;
        }

        /// <summary>
        /// Reads bytes from the message and moves the readCursor.
        /// </summary>
        /// <param name="amount">The amount of bytes to read</param>
        public byte[] ReadBytes(int amount)
        {
            byte[] data = new byte[amount];

            Buffer.BlockCopy(messageData, readCursor, data, 0, amount);

            readCursor += amount;

            return data;
        }

        /// <summary>
        /// Returns an ArraySegment referencing bytes from the message data then moves the readCursor.
        /// </summary>
        /// <param name="amount">The amount of bytes to return as ArraySegment</param>
        public ArraySegment<byte> ReadBytesAsSegment(int amount)
        {
            var arraySegment =  new ArraySegment<byte>(messageData, readCursor, amount);

            readCursor += amount;

            return arraySegment;
        }

        /// <summary>
        /// Reads a GUID from the message and moves the readCursor.
        /// </summary>
        public Guid ReadGuid()
        {
            const int guidSize = 16;
            byte[] data = new byte[guidSize];
            
            Buffer.BlockCopy(messageData, readCursor, data, 0, guidSize);

            readCursor += guidSize;

            return new Guid(data);
        }

        /// <summary>
        /// Get the type of this message.
        /// </summary>
        public MessageType GetMessageType()
        {
            return messageType;
        }

        /// <summary>
        /// Get the byte array that contains the data of this message.
        /// </summary>
        public byte[] GetMessageData()
        {
            return messageData;
        }
    }
}