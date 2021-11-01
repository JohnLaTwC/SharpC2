// Author: Ryan Cobb (@cobbr_io), The Wover (@TheRealWover)
// Project: SharpSploit (https://github.com/cobbr/SharpSploit)
// License: BSD 3-Clause

using System;
using System.Runtime.InteropServices;

namespace Drone.Invocation.DynamicInvoke
{
    public static class Win32
    {
        public static class Kernel32
        {
            public static bool IsWow64Process(IntPtr hProcess, ref bool lpSystemInfo)
            {
                object[] funcargs = { hProcess, lpSystemInfo };

                var retVal = (bool)Generic.DynamicAPIInvoke(@"kernel32.dll", @"IsWow64Process",
                    typeof(Delegates.IsWow64Process), ref funcargs);
                lpSystemInfo = (bool)funcargs[1];

                return retVal;
            }

            public static IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize,
                IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, ref IntPtr lpThreadId)
            {
                object[] funcargs =
                {
                    hProcess, lpThreadAttributes, dwStackSize, lpStartAddress, lpParameter, dwCreationFlags, lpThreadId
                };

                var retValue = (IntPtr)Generic.DynamicAPIInvoke(@"kernel32.dll", @"CreateRemoteThread",
                    typeof(Delegates.CreateRemoteThread), ref funcargs);

                lpThreadId = (IntPtr)funcargs[6];

                return retValue;
            }

            public static bool CloseHandle(IntPtr handle)
            {
                object[] funcargs = { handle };

                var retVal = (bool)Generic.DynamicAPIInvoke(@"kernel32.dll", @"CloseHandle",
                    typeof(Delegates.CloseHandle), ref funcargs);

                return retVal;
            }

            public static bool PeekNamedPipe(IntPtr pipeHandle, ref uint bytesToRead)
            {
                var parameters = new object[]
                {
                    pipeHandle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, bytesToRead, IntPtr.Zero
                };
                
                var result = (bool)Generic.DynamicAPIInvoke(@"kernel32.dll", @"PeekNamedPipe",
                    typeof(Delegates.PeekNamedPipe), ref parameters);

                bytesToRead = (uint)parameters[4];
                return result;
            }
        }

        public static class Advapi32
        {
            public static bool LogonUserA(string lpszUsername, string lpszDomain, string lpszPassword,
                Data.Win32.Advapi32.LogonUserType dwLogonType, Data.Win32.Advapi32.LogonUserProvider dwLogonProvider,
                ref IntPtr phToken)
            {
                object[] funcargs =
                {
                    lpszUsername, lpszDomain, lpszPassword, dwLogonType, dwLogonProvider, phToken
                };

                var retVal = (bool)Generic.DynamicAPIInvoke(@"advapi32.dll", @"LogonUserA",
                    typeof(Delegates.LogonUserA), ref funcargs);

                phToken = (IntPtr)funcargs[5];
                return retVal;
            }

            public static bool ImpersonateLoggedOnUser(IntPtr hToken)
            {
                object[] funcargs = { hToken };

                var retVal = (bool)Generic.DynamicAPIInvoke(@"advapi32.dll", @"ImpersonateLoggedOnUser",
                    typeof(Delegates.ImpersonateLoggedOnUser), ref funcargs);

                return retVal;
            }

            public static bool RevertToSelf()
            {
                object[] funcargs = { };

                var retVal = (bool)Generic.DynamicAPIInvoke(@"advapi32.dll", @"RevertToSelf",
                    typeof(Delegates.RevertToSelf), ref funcargs);

                return retVal;
            }

            public static bool OpenProcessToken(IntPtr hProcess, Data.Win32.Advapi32.TokenAccess tokenAccess,
                ref IntPtr hToken)
            {
                object[] funcargs = { hProcess, tokenAccess, hToken };

                var retVal = (bool)Generic.DynamicAPIInvoke(@"advapi32.dll", @"OpenProcessToken",
                    typeof(Delegates.OpenProcessToken), ref funcargs);

                hToken = (IntPtr)funcargs[2];

                return retVal;
            }

            public static bool DuplicateTokenEx(IntPtr hExistingToken, Data.Win32.Advapi32.TokenAccess dwDesiredAccess,
                Data.Win32.WinNT.SECURITY_IMPERSONATION_LEVEL ImpersonationLevel, Data.Win32.WinNT.TOKEN_TYPE TokenType,
                ref IntPtr phNewToken)
            {
                var lpTokenAttributes = new Data.Win32.WinNT.SECURITY_ATTRIBUTES();
                lpTokenAttributes.nLength = Marshal.SizeOf(lpTokenAttributes);

                object[] funcargs =
                {
                    hExistingToken, dwDesiredAccess, lpTokenAttributes, ImpersonationLevel,
                    TokenType, phNewToken
                };

                var retVal = (bool)Generic.DynamicAPIInvoke(@"advapi32.dll", @"DuplicateTokenEx",
                    typeof(Delegates.DuplicateTokenEx), ref funcargs);

                phNewToken = (IntPtr)funcargs[5];

                return retVal;
            }

            public static IntPtr OpenSCManager(string machineName, string databaseName,
                Data.Win32.Advapi32.SCMAccess dwAccess)
            {
                object[] funcargs = { machineName, databaseName, dwAccess };
                return (IntPtr)Generic.DynamicAPIInvoke(@"advapi32.dll", @"OpenSCManager",
                    typeof(Delegates.OpenSCManager), ref funcargs);
            }

            public static bool QueryServiceConfig(IntPtr hService,
                out Data.Win32.Advapi32.ServiceConfig serviceConfig)
            {
                object[] funcargs = { hService, IntPtr.Zero, (uint)0, (uint)0 };

                Generic.DynamicAPIInvoke(@"advapi32.dll", @"QueryServiceConfigA", typeof(Delegates.QueryServiceConfig),
                    ref funcargs);

                var bytesRequired = (uint)funcargs[3];
                var ptr = Marshal.AllocHGlobal((int)bytesRequired);

                funcargs = new object[] { hService, ptr, bytesRequired, (uint)0 };

                var success = (bool)Generic.DynamicAPIInvoke(@"advapi32.dll", @"QueryServiceConfigA",
                    typeof(Delegates.QueryServiceConfig), ref funcargs);

                if (!success)
                {
                    Marshal.FreeHGlobal(ptr);
                    serviceConfig = null;
                    return false;
                }

                serviceConfig =
                    (Data.Win32.Advapi32.ServiceConfig)Marshal.PtrToStructure(ptr,
                        typeof(Data.Win32.Advapi32.ServiceConfig));

                return true;
            }

            public static bool CloseServiceHandle(IntPtr hSCObject)
            {
                object[] funcargs = { hSCObject };

                return (bool)Generic.DynamicAPIInvoke(@"advapi32.dll", @"CloseServiceHandle",
                    typeof(Delegates.CloseServiceHandle), ref funcargs);
            }
        }

        public static class Delegates
        {
            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate bool CloseHandle(IntPtr handle);

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate IntPtr CreateRemoteThread(IntPtr hProcess,
                IntPtr lpThreadAttributes,
                uint dwStackSize,
                IntPtr lpStartAddress,
                IntPtr lpParameter,
                uint dwCreationFlags,
                out IntPtr lpThreadId);

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate bool LogonUserA(
                string lpszUsername,
                string lpszDomain,
                string lpszPassword,
                Data.Win32.Advapi32.LogonUserType dwLogonType,
                Data.Win32.Advapi32.LogonUserProvider dwLogonProvider,
                ref IntPtr phToken);

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate bool ImpersonateLoggedOnUser(IntPtr hToken);

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate bool OpenProcessToken(
                IntPtr ProcessHandle,
                Data.Win32.Advapi32.TokenAccess DesiredAccess,
                ref IntPtr TokenHandle);

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate bool DuplicateTokenEx(
                IntPtr hExistingToken,
                Data.Win32.Advapi32.TokenAccess dwDesiredAccess,
                ref Data.Win32.WinNT.SECURITY_ATTRIBUTES lpTokenAttributes,
                Data.Win32.WinNT.SECURITY_IMPERSONATION_LEVEL ImpersonationLevel,
                Data.Win32.WinNT.TOKEN_TYPE TokenType,
                ref IntPtr phNewToken);

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate bool RevertToSelf();

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate IntPtr CreateFileW(
                [MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
                uint dwDesiredAccess,
                uint dwShareMode,
                IntPtr lpSecurityAttributes,
                uint dwCreationDisposition,
                uint dwFlagsAndAttributes,
                IntPtr hTemplateFile);

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate uint GetFileAttributesW(IntPtr lpFileName);

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate bool GetFileAttributesExW(
                IntPtr lpFileName,
                uint fInfoLevelId,
                IntPtr lpFileInformation);

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate bool GetFileInformationByHandle(
                IntPtr hFile,
                IntPtr lpFileInformation);

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate IntPtr CreateTransaction(
                IntPtr lpTransactionAttributes,
                IntPtr uow,
                int createOptions,
                int isolationLevel,
                int isolationFlags,
                int timeout,
                [MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder description);

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate IntPtr CreateFileTransactedW(
                [MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
                uint dwDesiredAccess,
                uint dwShareMode,
                IntPtr lpSecurityAttributes,
                uint dwCreationDisposition,
                uint dwFlagsAndAttributes,
                IntPtr hTemplateFile,
                IntPtr hTransaction,
                ref ushort pusMiniVersion,
                IntPtr nullValue);

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate bool WriteFile(
                IntPtr hFile,
                byte[] lpBuffer,
                uint nNumberOfBytesToWrite,
                ref uint lpNumberOfBytesWritten,
                IntPtr lpOverlapped);

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate bool IsWow64Process(
                IntPtr hProcess,
                ref bool lpSystemInfo);

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate IntPtr OpenSCManager(
                string machineName,
                string databaseName,
                Data.Win32.Advapi32.SCMAccess dwAccess);

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate bool QueryServiceConfig(
                IntPtr hService,
                IntPtr lpServiceConfig,
                uint cbBufSize,
                out uint pcbBytesNeeded);

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate bool CloseServiceHandle(IntPtr hSCObject);
            
            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate bool PeekNamedPipe(
                IntPtr handle,
                IntPtr buffer,
                IntPtr nBufferSize,
                IntPtr bytesRead,
                ref uint bytesAvail,
                IntPtr bytesLeftThisMessage);
        }
    }
}