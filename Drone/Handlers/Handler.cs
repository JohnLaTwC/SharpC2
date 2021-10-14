using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

using Drone.Models;

namespace Drone.Handlers
{
    public abstract class Handler
    {
        public abstract string Name { get; }
        
        protected readonly ConcurrentQueue<MessageEnvelope> InboundQueue = new();
        protected readonly ConcurrentQueue<MessageEnvelope> OutboundQueue = new();

        protected DroneConfig Config;
        protected Metadata Metadata;

        public virtual void Init(DroneConfig config, Metadata metadata)
        {
            Config = config;
            Metadata = metadata;
        }

        public void QueueOutbound(MessageEnvelope message)
        {
            OutboundQueue.Enqueue(message);
        }
        
        public bool GetInbound(out IEnumerable<MessageEnvelope> messages)
        {
            if (InboundQueue.IsEmpty)
            {
                messages = null;
                return false;
            }

            List<MessageEnvelope> temp = new();

            while (InboundQueue.TryDequeue(out var message))
                temp.Add(message);

            messages = temp.ToArray();
            return true;
        }

        protected IEnumerable<MessageEnvelope> GetOutboundQueue()
        {
            List<MessageEnvelope> temp = new();

            while (OutboundQueue.TryDequeue(out var message))
                temp.Add(message);

            return temp.ToArray();
        }

        public abstract Task Start();
        public abstract void Stop();
    }
}