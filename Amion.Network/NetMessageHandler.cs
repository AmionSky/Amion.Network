using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Amion.Network
{
    public class NetMessageHandler : IDisposable
    {
        private ConcurrentQueue<MessageReceivedEventArgs> receivedMessages;
        private AutoResetEvent messageReceivedEvent;
        private Task processorTask;
        private bool processorLoop;

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public NetMessageHandler()
        {
            receivedMessages = new ConcurrentQueue<MessageReceivedEventArgs>();
            messageReceivedEvent = new AutoResetEvent(false);
            processorTask = null;
            processorLoop = false;
        }

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

        public void StopMessageProcessor()
        {
            if (processorTask == null) return;

            processorLoop = false;
            messageReceivedEvent.Set();
            processorTask.Wait();
            processorTask = null;
            messageReceivedEvent.Reset();
        }

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