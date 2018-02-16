using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Amion.Network
{
    public class NetClient : NetShared, IDisposable
    {
        public NetConnection Connection => connection;

        private NetConnection connection = null;
        private object connectLock = new object();

        public NetClient()
        {
            ConnectionStatusChanged += NetClient_ConnectionStatusChanged;
        }

        public void Connect(IPEndPoint ipEndPoint)
        {
            lock (connectLock)
            {
                Socket clientSocket = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
                {
                    NoDelay = UseNoDelay
                };

                try
                {
                    clientSocket.Connect(ipEndPoint);
                }
                catch (Exception ex)
                {
                    Log("Connect: " + ex.Message);
                    return;
                }

                connection?.Dispose();
                connection = new NetConnection(clientSocket);
                OnConnectionAdded(connection);
                connection.StatusChanged += OnConnectionStatusChanged;
                OnConnectionStatusChanged(connection, connection.Status, connection.RemoteId);

                if (connection.Status != NetConnectionStatus.Disconnected && AutoStartReceiver)
                {
                    connection.StartReceiverTask();
                }
            }
        }

        private void NetClient_ConnectionStatusChanged(object sender, ConnectionStatusChangedEventArgs e)
        {
            Log($"Connection status changed: {e.Status}");

            if (e.Status == NetConnectionStatus.Disconnected)
            {
                if (e.RemoteId == connection?.RemoteId)
                {
                    OnConnectionRemoved(connection.RemoteId);
                    connection.StatusChanged -= OnConnectionStatusChanged;
                    connection = null;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (connection != null)
                {
                    connection.Dispose();
                    connection = null;
                }
            }
        }
    }
}