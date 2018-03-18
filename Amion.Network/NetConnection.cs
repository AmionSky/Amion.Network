using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Amion.Network
{
    /// <summary>
    /// Status enum for NetConnection.
    /// </summary>
    public enum NetConnectionStatus : byte
    {
        /// <summary>
        /// Should only be unknown when invalid/uninitialized.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The connection is disconnected.
        /// </summary>
        Disconnected = 4,

        /// <summary>
        /// The connection is connected.
        /// </summary>
        Connected = 8,
    }

    /// <summary>
    /// Class for connection. Handles: socket, status changes, NetMessage send and receive.
    /// </summary>
    public class NetConnection : IDisposable
    {
        /// <summary>
        /// Called when a Message is received from the connection.
        /// Recommended to attach NetMessageHandler to it for handling messages without blocking the receiver thread.
        /// </summary>
        public event EventHandler<MessageReceivedEventArgs> RawMessageReceived;

        /// <summary>
        /// Called when status changed. Use NetServer or NetClient ConnectionStatusChanged event instead of this.
        /// </summary>
        public event EventHandler<ConnectionStatusChangedEventArgs> StatusChanged;

        /// <summary>
        /// Current status of the connection.
        /// </summary>
        public NetConnectionStatus Status => status;

        /// <summary>
        /// Identification of the connection.
        /// </summary>
        public Guid RemoteId => remoteId;

        /// <summary>
        /// Gets the remote endpoint.
        /// </summary>
        public EndPoint RemoteEndPoint => connection.RemoteEndPoint;

        /// <summary>
        /// Log action.
        /// </summary>
        public Action<string> Log = NetUtility.Log;

        private NetConnectionStatus status = NetConnectionStatus.Unknown;
        private Guid remoteId;

        private Socket connection;
        private Task receiverTask;
        private Task senderTask;

        private bool disposed;
        private object disposeLock;

        private int receiverBufferSize;

        private object senderLock;
        private bool senderLoop;
        private ConcurrentQueue<byte[]> messageQueue;
        private AutoResetEvent messageSentEvent;

        /// <summary></summary>
        /// <param name="socket">Socket of the connection</param>
        /// <param name="statusChanged">EventHandler for status changed</param>
        /// <param name="receiverBufferSize">Size of the network message receiver buffer in bytes</param>
        public NetConnection(Socket socket, EventHandler<ConnectionStatusChangedEventArgs> statusChanged, int receiverBufferSize = 1024)
        {
            connection = socket;
            remoteId = Guid.NewGuid();
            receiverTask = null;

            this.receiverBufferSize = receiverBufferSize;

            //status
            StatusChanged += statusChanged;
            OnStatusChanged(NetConnectionStatus.Connected);

            //Dispose
            disposed = false;
            disposeLock = new object();

            //Sender
            senderLock = new object();
            senderLoop = true;
            messageQueue = new ConcurrentQueue<byte[]>();
            messageSentEvent = new AutoResetEvent(false);
            senderTask = Task.Factory.StartNew(SenderWorker, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Starts the message receiver.
        /// </summary>
        public void StartReceiverTask()
        {
            if (status == NetConnectionStatus.Disconnected) return;

            if (receiverTask == null)
            {
                receiverTask = Task.Factory.StartNew(ReceiverWorker, TaskCreationOptions.LongRunning);
            }
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
            SocketSend(message.ToArray());
        }

        private void SocketSend(byte[] msg)
        {
            lock (senderLock)
            {
                if (disposed) return;

                try { connection.Send(msg); }
                catch (Exception)
                {
                    Log("Send: Exception");
                    if (!disposed) Dispose();
                }
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

                messageSentEvent?.WaitOne();
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

            byte[] buffer = new byte[receiverBufferSize];
            int bytesRead = -1;
            int bufferCursor = 0;
            int amountToCopy = 0;

            while (bytesRead != 0)
            {
                //Reading data from connection with disconnect detector
                try { bytesRead = connection.Receive(buffer); }
                catch { Log("Connection lost."); break; }
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

        /// <summary>
        /// Invokes RawMessageReceived event.
        /// </summary>
        protected void OnRawMessageReceived(NetInMessage message)
        {
            RawMessageReceived?.Invoke(this, new MessageReceivedEventArgs(message, RemoteId));
        }

        /// <summary>
        /// Updates status and invokes StatusChanged event.
        /// </summary>
        protected void OnStatusChanged(NetConnectionStatus newStatus)
        {
            if (status != newStatus)
            {
                status = newStatus;
                StatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs(newStatus, RemoteId));
            }
        }

        /// <summary>
        /// Disconnects the socket. Same as Dispose().
        /// </summary>
        [Obsolete("Use Dispose() instead")]
        public void Disconnect() => Dispose();

        /// <summary>
        /// Disconnects. Shuts down worker threads and releases resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Helper for Dispose()
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            lock (disposeLock)
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

                    senderLoop = false;

                    if (messageSentEvent != null)
                    {
                        messageSentEvent.Set();
                        messageSentEvent.Dispose();
                        messageSentEvent = null;
                    }

                    if (connection != null)
                    {
                        connection.Dispose();
                        connection = null;
                    }
                }
            }
        }
    }
}