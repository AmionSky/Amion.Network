using System;
using System.Collections.Generic;
using System.Text;

namespace Amion.Network
{
    public class ConnectionAddedEventArgs : EventArgs
    {
        public NetConnection Connection;

        public ConnectionAddedEventArgs() { }

        public ConnectionAddedEventArgs(NetConnection connection)
        {
            Connection = connection;
        }
    }

    public class ConnectionRemovedEventArgs : EventArgs
    {
        public Guid RemoteId;

        public ConnectionRemovedEventArgs() { }

        public ConnectionRemovedEventArgs(Guid remoteId)
        {
            RemoteId = remoteId;
        }
    }

    public class ListenerStatusEventArgs : EventArgs
    {
        public bool IsActive;

        public ListenerStatusEventArgs() { }

        public ListenerStatusEventArgs(bool isActive)
        {
            IsActive = isActive;
        }
    }

    public class ConnectionStatusChangedEventArgs : EventArgs
    {
        public NetConnectionStatus Status;
        public Guid RemoteId;

        public ConnectionStatusChangedEventArgs() { }

        public ConnectionStatusChangedEventArgs(NetConnectionStatus status, Guid remoteId)
        {
            Status = status;
            RemoteId = remoteId;
        }
    }

    public class MessageReceivedEventArgs : EventArgs
    {
        public NetInMessage Message;
        public Guid RemoteId;

        public MessageReceivedEventArgs() { }

        public MessageReceivedEventArgs(NetInMessage message, Guid remoteId)
        {
            Message = message;
            RemoteId = remoteId;
        }
    }
}
