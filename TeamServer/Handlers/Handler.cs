using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;

using TeamServer.Hubs;
using TeamServer.Interfaces;
using TeamServer.Models;
using TeamServer.Services;

namespace TeamServer.Handlers
{
    public abstract class Handler
    {
        public abstract string Name { get; }
        public abstract List<HandlerParameter> Parameters { get; }
        
        protected ITaskService TaskService;
        protected ICredentialService CredentialService;
        protected IHubContext<MessageHub, IMessageHub> MessageHub;
        protected CancellationTokenSource TokenSource;

        public void Init(ITaskService taskService, ICredentialService credentialService, IHubContext<MessageHub, IMessageHub> messageHub)
        {
            TaskService = taskService;
            CredentialService = credentialService;
            MessageHub = messageHub;
        }

        public bool Running
            => TokenSource is not null && !TokenSource.IsCancellationRequested;

        public void SetParameters(Dictionary<string, string> parameters)
        {
            foreach (var (key, value) in parameters)
                SetParameter(key, value);
        }

        public void SetParameter(string name, string value)
        {
            var parameter = Parameters.FirstOrDefault(p =>
                p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            parameter?.SetValue(value);
        }

        protected string GetParameter(string key)
            => Parameters.Single(p => p.Name.Equals(key, StringComparison.OrdinalIgnoreCase)).Value;

        public virtual Task Start()
        {
            var nonOptional = Parameters.Where(p => !p.Optional).ToArray();

            if (nonOptional.Length <= 0) return Task.CompletedTask;
            
            foreach (var parameter in nonOptional)
                if (string.IsNullOrEmpty(parameter.Value))
                    throw new Exception($"Parameter \"{parameter.Name}\" cannot be null");

            TokenSource = new CancellationTokenSource();
            return Task.CompletedTask;
        }
        
        public abstract void Stop();
    }
}