using System;
using System.Text.Json.Serialization;

namespace TeamServer.Models
{
    [Serializable]
    public class C2Message
    {
        public MessageDirection Direction { get; }
        public MessageType Type { get; }
        public DroneMetadata Metadata { get; set; }
        public byte[] Data { get; set; }

        public C2Message(MessageDirection direction, MessageType type)
        {
            Direction = direction;
            Type = type;
        }

        public enum MessageDirection
        {
            Upstream,
            Downstream
        }
        
        public enum MessageType : int
        {
            DroneModule = 0,
            DroneTask = 1,
            DroneTaskUpdate = 2,
            NewLink = 3
        }
    }
}