using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Amion.Network
{
    public enum NetConnectionStatus : byte
    {
        Unknown = 0,
        Disconnected = 4,
        Connected = 8,
    }

    public class NetConnection : NetUtility, IDisposable
    {
        public event EventHandler<MessageReceivedEventArgs> RawMessageReceived;
        public event EventHandler<ConnectionStatusChangedEventArgs> StatusChanged;

        private NetConnectionStatus status = NetConnectionStatus.Unknown;
        private Guid remoteId;

        private Socket connection;
        private Task receiverTask;
        private Task senderTask;

        private bool disposed;

        private object senderLock;
        private bool senderLoop;
        private ConcurrentQueue<byte[]> messageQueue;
        private AutoResetEvent messageSentEvent;

        public NetConnectionStatus Status => status;
        public Guid RemoteId => remoteId;
        public EndPoint RemoteEndPoint => connection.RemoteEndPoint;

        public NetConnection(Socket socket)
        {
            connection = socket;
            remoteId = Guid.NewGuid();
            status = NetConnectionStatus.Connected;
            receiverTask = null;

            //Dispose
            disposed = false;

            //Sender
            senderLock = new object();
            senderLoop = true;
            messageQueue = new ConcurrentQueue<byte[]>();
            messageSentEvent = new AutoResetEvent(false);
            senderTask = Task.Factory.StartNew(SenderWorker, TaskCreationOptions.LongRunning);
        }

        public void StartReceiverTask()
        {
            if (receiverTask == null) receiverTask = Task.Factory.StartNew(ReceiverWorker, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Queues a message for send through the connection.
        /// </summary>
        /// <param name="message">Message to send.</param>
        public void Send(NetOutMessage message)
        {
            if (disposed) return;

            byte[] msg = message.ToArray();
            
            messageQueue.Enqueue(msg);
            messageSentEvent.Set();
        }

        /// <summary>
        /// Sends a message for through the connection as soon as able to.
        /// </summary>
        /// <param name="message">Message to send.</param>
        public void SendSynchronously(NetOutMessage message)
        {
            if (disposed) return;

            byte[] msg = message.ToArray();

            SocketSend(msg);
        }

        private void SocketSend(byte[] msg)
        {
            try
            {
                lock (senderLock)
                {
                    connection.Send(msg);
                }
            }
            catch (Exception)
            {
                Log("Send: Exception");
                if (!disposed) Dispose();
            }
        }

        private void SenderWorker()
        {
            while (senderLoop)
            {
                while (!messageQueue.IsEmpty)
                {
                    if (!senderLoop) return;

                    if (messageQueue.TryDequeue(out byte[] msg))
                    {
                        SocketSend(msg);
                    }
                }

                messageSentEvent.WaitOne();
            }
        }

        private void ReceiverWorker()
        {
            MessageType messageType = MessageType.Unknown;
            int messageLength = -1;
            int messageHeaderCursor = 0;
            int messageDataCursor = 0;
            byte[] messageHeader = new byte[NetOutMessage.HeaderSize];
            byte[] messageData = null;
            bool messageStarted = false;

            byte[] buffer = new byte[2048];
            int bytesRead = -1;
            int bufferCursor = 0;
            int amountToCopy = 0;

            //while (connection.Connected)
            while (bytesRead != 0)
            {
                //Reading data from connection with disconnect detector
                try { bytesRead = connection.Receive(buffer); }
                catch { Log("Connection lost."); bytesRead = 0; }
                bufferCursor = 0;

                while (bufferCursor < bytesRead)
                {
                    if (messageHeaderCursor != NetOutMessage.HeaderSize)
                    {
                        //Getting data for the message header
                        amountToCopy = Math.Min(bytesRead - bufferCursor, NetOutMessage.HeaderSize - messageHeaderCursor);
                        if (amountToCopy > 0)
                        {
                            Buffer.BlockCopy(buffer, bufferCursor, messageHeader, messageHeaderCursor, amountToCopy);

                            messageHeaderCursor += amountToCopy;
                            bufferCursor += amountToCopy;
                        }
                    }

                    //If header is completed continue
                    if (messageHeaderCursor == NetOutMessage.HeaderSize)
                    {
                        //Convert header to info and prepair for message data
                        if (!messageStarted)
                        {
                            NetOutMessage.DecodeHeader(messageHeader, out messageType, out messageLength);
                            messageData = new byte[messageLength];

                            messageStarted = true;
                        }

                        //Copy data in buffer to messageData
                        amountToCopy = Math.Min(bytesRead - bufferCursor, messageLength - messageDataCursor);
                        if (amountToCopy > 0)
                        {
                            Buffer.BlockCopy(buffer, bufferCursor, messageData, messageDataCursor, amountToCopy);

                            messageDataCursor += amountToCopy;
                            bufferCursor += amountToCopy;
                        }

                        //Finish message and reset
                        if (messageDataCursor == messageLength)
                        {
                            OnRawMessageReceived(new NetInMessage(messageType, messageData));

                            messageStarted = false;
                            messageHeaderCursor = 0;
                            messageDataCursor = 0;
                            messageData = null;
                        }
                    }
                }
            }

            Dispose();
        }

        // Events
        protected void OnRawMessageReceived(NetInMessage message)
        {
            RawMessageReceived?.Invoke(this, new MessageReceivedEventArgs(message, RemoteId));
        }

        protected void OnStatusChanged(NetConnectionStatus newStatus)
        {
            if (status != newStatus)
            {
                status = newStatus;
                StatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs(newStatus, RemoteId));
            }
        }

        // Dispose
        [Obsolete("Use Dispose() instead")]
        public void Disconnect() => Dispose();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !disposed)
            {
                disposed = true;
                OnStatusChanged(NetConnectionStatus.Disconnected);

                try
                {
                    connection.Shutdown(SocketShutdown.Both);
                    connection.Disconnect(false);
                }
                catch (Exception) { }

                if (senderLoop)
                {
                    senderLoop = false;
                    messageSentEvent?.Set();
                    senderTask?.Wait();
                }

                if (connection != null)
                {
                    connection.Dispose();
                    connection = null;
                }

                if (messageSentEvent != null)
                {
                    messageSentEvent.Dispose();
                    messageSentEvent = null;
                }
            }
        }
    }
}