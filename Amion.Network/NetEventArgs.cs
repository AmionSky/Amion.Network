using System;
using System.Collections.Generic;
using System.Text;

namespace Amion.Network
{
    /// <summary>
    /// EventArgs for ConnectionAdded event.
    /// </summary>
    public class ConnectionAddedEventArgs : EventArgs
    {
        /// <summary>
        /// The added connection.
        /// </summary>
        public NetConnection Connection;

        /// <summary></summary>
        public ConnectionAddedEventArgs() { }

        /// <summary></summary>
        public ConnectionAddedEventArgs(NetConnection connection)
        {
            Connection = connection;
        }
    }

    /// <summary>
    /// EventArgs for ConnectionRemoved event.
    /// </summary>
    public class ConnectionRemovedEventArgs : EventArgs
    {
        /// <summary>
        /// The remote ID of the removed connection.
        /// </summary>
        public Guid RemoteId;

        /// <summary></summary>
        public ConnectionRemovedEventArgs() { }

        /// <summary></summary>
        public ConnectionRemovedEventArgs(Guid remoteId)
        {
            RemoteId = remoteId;
        }
    }

    /// <summary>
    /// EventArgs for ListenerStatusChanged event.
    /// </summary>
    public class ListenerStatusEventArgs : EventArgs
    {
        /// <summary>
        /// Listener IsActive.
        /// </summary>
        public bool IsActive;

        /// <summary></summary>
        public ListenerStatusEventArgs() { }

        /// <summary></summary>
        public ListenerStatusEventArgs(bool isActive)
        {
            IsActive = isActive;
        }
    }

    /// <summary>
    /// EventArgs for ConnectionStatusChanged event.
    /// </summary>
    public class ConnectionStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// New status of the connection.
        /// </summary>
        public NetConnectionStatus Status;

        /// <summary>
        /// Remote ID of the connection.
        /// </summary>
        public Guid RemoteId;

        /// <summary></summary>
        public ConnectionStatusChangedEventArgs() { }

        /// <summary></summary>
        public ConnectionStatusChangedEventArgs(NetConnectionStatus status, Guid remoteId)
        {
            Status = status;
            RemoteId = remoteId;
        }
    }

    /// <summary>
    /// EventArgs for MessageReceived event.
    /// </summary>
    public class MessageReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Received message.
        /// </summary>
        public NetInMessage Message;

        /// <summary>
        /// Remote ID of the connection.
        /// </summary>
        public Guid RemoteId;

        /// <summary></summary>
        public MessageReceivedEventArgs() { }

        /// <summary></summary>
        public MessageReceivedEventArgs(NetInMessage message, Guid remoteId)
        {
            Message = message;
            RemoteId = remoteId;
        }
    }
}
