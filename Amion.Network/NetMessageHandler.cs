using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Amion.Network
{
    /// <summary>
    /// Helper class for handling incoming messages from multiple connections without blocking the receiver thread.
    /// </summary>
    public class NetMessageHandler : IDisposable
    {
        private ConcurrentQueue<MessageReceivedEventArgs> receivedMessages;
        private AutoResetEvent messageReceivedEvent;
        private Task processorTask;
        private bool processorLoop;

        /// <summary>
        /// Called when a message is received. Use this event as your main message processing thread.
        /// </summary>
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public NetMessageHandler()
        {
            receivedMessages = new ConcurrentQueue<MessageReceivedEventArgs>();
            messageReceivedEvent = new AutoResetEvent(false);
            processorTask = null;
            processorLoop = false;
        }

        /// <summary>
        /// Starts the message processing task. Subscribe for MessageReceived event before calling this.
        /// </summary>
        public void StartMessageProcessor()
        {
            if (processorTask != null) return;

            processorLoop = true;
            processorTask = Task.Factory.StartNew(MessageProcessor, TaskCreationOptions.LongRunning);
        }

        private void MessageProcessor()
        {
            while (processorLoop)
            {
                while (!receivedMessages.IsEmpty)
                {
                    if (!processorLoop) return;

                    if (receivedMessages.TryDequeue(out MessageReceivedEventArgs e))
                    {
                        OnMessageReceived(e);
                    }
                }

                messageReceivedEvent.WaitOne();
            }
        }

        /// <summary>
        /// Stops the message processing task.
        /// </summary>
        public void StopMessageProcessor()
        {
            if (processorTask == null) return;

            processorLoop = false;
            messageReceivedEvent.Set();
            processorTask.Wait();
            processorTask = null;
            messageReceivedEvent.Reset();
        }

        /// <summary>
        /// Subscribe this method to the NetConnection's RawMessageReceived event.
        /// </summary>
        public void RawMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            receivedMessages.Enqueue(e);
            messageReceivedEvent.Set();
        }

        protected void OnMessageReceived(MessageReceivedEventArgs e)
        {
            MessageReceived?.Invoke(this, e);
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
                if (processorLoop)
                {
                    processorLoop = false;
                    messageReceivedEvent?.Set();
                    processorTask?.Wait();
                }

                if (messageReceivedEvent != null)
                {
                    messageReceivedEvent.Dispose();
                    messageReceivedEvent = null;
                }

                if (processorTask != null)
                {
                    processorTask.Dispose();
                    processorTask = null;
                }
            }
        }
    }
}