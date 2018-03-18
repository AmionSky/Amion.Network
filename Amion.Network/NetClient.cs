using System;
using System.Net;
using System.Net.Sockets;

namespace Amion.Network
{
    /// <summary>
    /// Class for handling a single connection.
    /// </summary>
    public class NetClient : NetShared, IDisposable
    {
        /// <summary>
        /// Current connection.
        /// </summary>
        public NetConnection Connection { get; private set; } = null;
        
        private object connectLock = new object();

        /// <summary></summary>
        public NetClient()
        {
            ConnectionStatusChanged += NetClient_ConnectionStatusChanged;
        }

        /// <summary>
        /// Connect to a specified IP. Disconnects from the current connection on successful connect.
        /// </summary>
        /// <param name="ipEndPoint">IP to connect to</param>
        public void Connect(IPEndPoint ipEndPoint)
        {
            if (ipEndPoint == null) { Log("IPEndPoint is null"); return; }

            lock (connectLock)
            {
                Socket clientSocket = null;

                try { clientSocket = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp); }
                catch (SocketException ex)
                {
                    Log(NetUtility.Error(ECode.Client_FailedToCreateConnectionSocket));
                    Log(ex.Message);
                    clientSocket?.Dispose();
                    return;
                }

                clientSocket.NoDelay = UseNoDelay;

                try { clientSocket.Connect(ipEndPoint); }
                catch (Exception ex)
                {
                    Log(NetUtility.Error(ECode.Client_FailedToConnect));
                    Log(ex.Message);
                    clientSocket?.Dispose();
                    return;
                }

                Connection?.Dispose();
                Connection = new NetConnection(clientSocket, OnConnectionStatusChanged);
                OnConnectionAdded(Connection);

                if (AutoStartReceiver) Connection?.StartReceiverTask();
            }
        }

        /// <summary>
        /// Disconnects from the current connection.
        /// </summary>
        public void Disconnect()
        {
            if (Connection != null)
            {
                Connection.Dispose();
                Connection = null;
            }
        }

        private void NetClient_ConnectionStatusChanged(object sender, ConnectionStatusChangedEventArgs e)
        {
            Log($"Connection status changed: {e.Status}");

            if (e.Status == NetConnectionStatus.Disconnected)
            {
                if (Connection != null && e.RemoteId == Connection.RemoteId)
                {
                    OnConnectionRemoved(Connection.RemoteId);
                    Connection.StatusChanged -= OnConnectionStatusChanged;
                    Connection = null;
                }
            }
        }

        /// <summary>
        /// Disconnects and releases resources.
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
            if (disposing)
            {
                Disconnect();
            }
        }
    }
}