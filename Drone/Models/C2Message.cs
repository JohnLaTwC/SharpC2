using System.Runtime.Serialization;

namespace Drone.Models
{
    [DataContract]
    public class C2Message
    {
        [DataMember (Name = "Direction")]
        public MessageDirection Direction { get; set; }
        
        [DataMember (Name = "Type")]
        public MessageType Type { get; set; }
        
        [DataMember (Name = "Metadata")]
        public Metadata Metadata { get; set; }
        
        [DataMember (Name = "Data")]
        public string Data { get; set; }

        public C2Message(MessageDirection direction, MessageType type, Metadata metadata)
        {
            Direction = direction;
            Type = type;
            Metadata = metadata;
        }
        
        public enum MessageDirection : int
        {
            Upstream = 0,
            Downstream = 1
        }
        
        public enum MessageType : int
        {
            DroneModule = 0,
            DroneTask = 1,
            DroneTaskUpdate = 2,
            NewLink = 3,
        }
    }
}