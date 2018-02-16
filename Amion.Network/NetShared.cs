using System;
using System.Net.Sockets;

namespace Amion.Network
{
    public abstract class NetShared : NetUtility
    {
        public AddressFamily PreferredAddressFamily = AddressFamily.InterNetwork;

        public bool UseNoDelay = false;
        public bool AutoStartReceiver = true;

        public event EventHandler<ConnectionAddedEventArgs> ConnectionAdded;
        public event EventHandler<ConnectionRemovedEventArgs> ConnectionRemoved;
        public event EventHandler<ConnectionStatusChangedEventArgs> ConnectionStatusChanged;

        // Events
        protected virtual void OnConnectionAdded(ConnectionAddedEventArgs e)
        {
            ConnectionAdded?.Invoke(this, e);
        }
        protected virtual void OnConnectionAdded(NetConnection netConnection)
        {
            ConnectionAdded?.Invoke(this, new ConnectionAddedEventArgs(netConnection));
        }

        protected virtual void OnConnectionRemoved(ConnectionRemovedEventArgs e)
        {
            ConnectionRemoved?.Invoke(this, e);
        }
        protected virtual void OnConnectionRemoved(Guid remoteId)
        {
            ConnectionRemoved?.Invoke(this, new ConnectionRemovedEventArgs(remoteId));
        }

        protected virtual void OnConnectionStatusChanged(object sender, ConnectionStatusChangedEventArgs e)
        {
            ConnectionStatusChanged?.Invoke(sender, e);
        }
        protected virtual void OnConnectionStatusChanged(object sender, NetConnectionStatus status, Guid remoteId)
        {
            ConnectionStatusChanged?.Invoke(sender, new ConnectionStatusChangedEventArgs(status, remoteId));
        }
    }
}