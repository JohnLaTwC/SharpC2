using System;
using System.Collections.Generic;

namespace SharpC2.Models
{
    public class Drone : Result
    {
        public string Guid { get; set; }
        public string Parent { get; set; }
        public string Address { get; set; }
        public string Hostname { get; set; }
        public string Username { get; set; }
        public string Process { get; set; }
        public int Pid { get; set; }
        public DroneIntegrity Integrity { get; set; }
        public DroneArch Arch { get; set; }
        public DateTime LastSeen { get; set; }

        public bool Hidden { get; private set; }

        public void Hide()
        {
            Hidden = true;
        }

        public void Show()
        {
            Hidden = false;
        }
        
        public List<DroneModule> Modules { get; set; }

        // empty ctor for automapper
        public Drone() { }

        public Drone(DroneMetadata metadata)
        {
            Guid = metadata.Guid;
            Address = metadata.Address;
            Hostname = metadata.Hostname;
            Username = metadata.Username;
            Process = metadata.Process;
            Pid = metadata.Pid;
            Integrity = (DroneIntegrity)metadata.Integrity;
            Arch = (DroneArch)metadata.Arch;
            LastSeen = DateTime.UtcNow;
        }
        
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

        protected internal override IList<ResultProperty> ResultProperties => new List<ResultProperty>
        {
            new() { Name = "Guid", Value = Guid },
            new() { Name = "Parent", Value = Parent },
            new() { Name = "Address", Value = Address },
            new() { Name = "Hostname", Value = Hostname },
            new() { Name = "Username", Value = Username },
            new() { Name = "Process", Value = Process },
            new() { Name = "Pid", Value = Pid },
            new() { Name = "Integrity", Value = Integrity },
            new() { Name = "Arch", Value = Arch },
            new() { Name = "LastSeen", Value = LastSeen }
        };
    }
}