namespace SharpC2.Models
{
    public class DroneMetadata
    {
        public string Guid { get; set; }
        public string Address { get; set; }
        public string Hostname { get; set; }
        public string Username { get; set; }
        public string Process { get; set; }
        public int Pid { get; set; }
        public DroneIntegrity Integrity { get; set; }
        public DroneArch Arch { get; set; }
        
        public enum DroneIntegrity
        {
            Medium,
            High,
            SYSTEM
        }

        public enum DroneArch
        {
            x86,
            x64
        }
    }
}