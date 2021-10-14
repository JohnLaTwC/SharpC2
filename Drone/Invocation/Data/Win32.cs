// Author: Ryan Cobb (@cobbr_io)
// Project: SharpSploit (https://github.com/cobbr/SharpSploit)
// License: BSD 3-Clause

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Drone.Invocation.Data
{
    public static class Win32
    {
        public static class Kernel32
        {
            public static uint MEM_COMMIT = 0x1000;
            public static uint MEM_RESERVE = 0x2000;
            public static uint MEM_RESET = 0x80000;
            public static uint MEM_RESET_UNDO = 0x1000000;
            public static uint MEM_LARGE_PAGES = 0x20000000;
            public static uint MEM_PHYSICAL = 0x400000;
            public static uint MEM_TOP_DOWN = 0x100000;
            public static uint MEM_WRITE_WATCH = 0x200000;
            public static uint MEM_COALESCE_PLACEHOLDERS = 0x1;
            public static uint MEM_PRESERVE_PLACEHOLDER = 0x2;
            public static uint MEM_DECOMMIT = 0x4000;
            public static uint MEM_RELEASE = 0x8000;
            
            [StructLayout(LayoutKind.Sequential)]
            public struct IMAGE_IMPORT_DESCRIPTOR
            {
                public uint OriginalFirstThunk;
                public uint TimeDateStamp;
                public uint ForwarderChain;
                public uint Name;
                public uint FirstThunk;
            }

            [Flags]
            public enum FileAccessFlags : uint
            {
                DELETE = 0x10000,
                FILE_READ_DATA = 0x1,
                FILE_READ_ATTRIBUTES = 0x80,
                FILE_READ_EA = 0x8,
                READ_CONTROL = 0x20000,
                FILE_WRITE_DATA = 0x2,
                FILE_WRITE_ATTRIBUTES = 0x100,
                FILE_WRITE_EA = 0x10,
                FILE_APPEND_DATA = 0x4,
                WRITE_DAC = 0x40000,
                WRITE_OWNER = 0x80000,
                SYNCHRONIZE = 0x100000,
                FILE_EXECUTE = 0x20
            }

            [Flags]
            public enum FileShareFlags : uint
            {
                FILE_SHARE_NONE = 0x0,
                FILE_SHARE_READ = 0x1,
                FILE_SHARE_WRITE = 0x2,
                FILE_SHARE_DELETE = 0x4
            }

            [Flags]
            public enum FileOpenFlags : uint
            {
                FILE_DIRECTORY_FILE = 0x1,
                FILE_WRITE_THROUGH = 0x2,
                FILE_SEQUENTIAL_ONLY = 0x4,
                FILE_NO_INTERMEDIATE_BUFFERING = 0x8,
                FILE_SYNCHRONOUS_IO_ALERT = 0x10,
                FILE_SYNCHRONOUS_IO_NONALERT = 0x20,
                FILE_NON_DIRECTORY_FILE = 0x40,
                FILE_CREATE_TREE_CONNECTION = 0x80,
                FILE_COMPLETE_IF_OPLOCKED = 0x100,
                FILE_NO_EA_KNOWLEDGE = 0x200,
                FILE_OPEN_FOR_RECOVERY = 0x400,
                FILE_RANDOM_ACCESS = 0x800,
                FILE_DELETE_ON_CLOSE = 0x1000,
                FILE_OPEN_BY_FILE_ID = 0x2000,
                FILE_OPEN_FOR_BACKUP_INTENT = 0x4000,
                FILE_NO_COMPRESSION = 0x8000
            }
            
            [Flags]
            public enum ProcessAccessFlags : uint
            {
                // https://msdn.microsoft.com/en-us/library/windows/desktop/ms684880%28v=vs.85%29.aspx?f=255&MSPPError=-2147217396
                PROCESS_ALL_ACCESS = 0x001F0FFF,
                PROCESS_CREATE_PROCESS = 0x0080,
                PROCESS_CREATE_THREAD = 0x0002,
                PROCESS_DUP_HANDLE = 0x0040,
                PROCESS_QUERY_INFORMATION = 0x0400,
                PROCESS_QUERY_LIMITED_INFORMATION = 0x1000,
                PROCESS_SET_INFORMATION = 0x0200,
                PROCESS_SET_QUOTA = 0x0100,
                PROCESS_SUSPEND_RESUME = 0x0800,
                PROCESS_TERMINATE = 0x0001,
                PROCESS_VM_OPERATION = 0x0008,
                PROCESS_VM_READ = 0x0010,
                PROCESS_VM_WRITE = 0x0020,
                SYNCHRONIZE = 0x00100000
            }
        }

        public static class Advapi32
        {
            public enum LogonUserProvider
            {
                LOGON32_PROVIDER_DEFAULT = 0,
                LOGON32_PROVIDER_WINNT35 = 1,
                LOGON32_PROVIDER_WINNT40 = 2,
                LOGON32_PROVIDER_WINNT50 = 3,
                LOGON32_PROVIDER_VIRTUAL = 4
            }

            public enum LogonUserType
            {
                LOGON32_LOGON_INTERACTIVE = 2,
                LOGON32_LOGON_NETWORK = 3,
                LOGON32_LOGON_BATCH = 4,
                LOGON32_LOGON_SERVICE = 5,
                LOGON32_LOGON_UNLOCK = 7,
                LOGON32_LOGON_NETWORK_CLEARTEXT = 8,
                LOGON32_LOGON_NEW_CREDENTIALS = 9
            }

            [Flags]
            public enum TokenAccess : uint
            {
                TOKEN_ASSIGN_PRIMARY = 0x0001,
                TOKEN_DUPLICATE = 0x0002,
                TOKEN_IMPERSONATE = 0x0004,
                TOKEN_QUERY = 0x0008,
                TOKEN_QUERY_SOURCE = 0x0010,
                TOKEN_ADJUST_PRIVILEGES = 0x0020,
                TOKEN_ADJUST_GROUPS = 0x0040,
                TOKEN_ADJUST_DEFAULT = 0x0080,
                TOKEN_ADJUST_SESSIONID = 0x0100,
                TOKEN_ALL_ACCESS_P = 0x000F00FF,
                TOKEN_ALL_ACCESS = 0x000F01FF,
                TOKEN_READ = 0x00020008,
                TOKEN_WRITE = 0x000200E0,
                TOKEN_EXECUTE = 0x00020000
            }

            [Flags]
            public enum SCMAccess : uint
            {
                SC_MANAGER_CONNECT = 0x00001,
                SC_MANAGER_CREATE_SERVICE = 0x00002,
                SC_MANAGER_ENUMERATE_SERVICE = 0x00004,
                SC_MANAGER_LOCK = 0x00008,
                SC_MANAGER_QUERY_LOCK_STATUS = 0x00010,
                SC_MANAGER_MODIFY_BOOT_CONFIG = 0x00020,

                SC_MANAGER_ALL_ACCESS = WinNT.ACCESS_MASK.STANDARD_RIGHTS_REQUIRED |
                                        SC_MANAGER_CONNECT |
                                        SC_MANAGER_CREATE_SERVICE |
                                        SC_MANAGER_ENUMERATE_SERVICE |
                                        SC_MANAGER_LOCK |
                                        SC_MANAGER_QUERY_LOCK_STATUS |
                                        SC_MANAGER_MODIFY_BOOT_CONFIG,

                GENERIC_READ = WinNT.ACCESS_MASK.STANDARD_RIGHTS_READ |
                               SC_MANAGER_ENUMERATE_SERVICE |
                               SC_MANAGER_QUERY_LOCK_STATUS,

                GENERIC_WRITE = WinNT.ACCESS_MASK.STANDARD_RIGHTS_WRITE |
                                SC_MANAGER_CREATE_SERVICE |
                                SC_MANAGER_MODIFY_BOOT_CONFIG,

                GENERIC_EXECUTE = WinNT.ACCESS_MASK.STANDARD_RIGHTS_EXECUTE |
                                  SC_MANAGER_CONNECT | SC_MANAGER_LOCK,

                GENERIC_ALL = SC_MANAGER_ALL_ACCESS,
            }

            [Flags]
            public enum ServiceAccess : uint
            {
                STANDARD_RIGHTS_REQUIRED = 0xF0000,
                SERVICE_QUERY_CONFIG = 0x00001,
                SERVICE_CHANGE_CONFIG = 0x00002,
                SERVICE_QUERY_STATUS = 0x00004,
                SERVICE_ENUMERATE_DEPENDENTS = 0x00008,
                SERVICE_START = 0x00010,
                SERVICE_STOP = 0x00020,
                SERVICE_PAUSE_CONTINUE = 0x00040,
                SERVICE_INTERROGATE = 0x00080,
                SERVICE_USER_DEFINED_CONTROL = 0x00100,

                SERVICE_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED |
                                     SERVICE_QUERY_CONFIG |
                                     SERVICE_CHANGE_CONFIG |
                                     SERVICE_QUERY_STATUS |
                                     SERVICE_ENUMERATE_DEPENDENTS |
                                     SERVICE_START |
                                     SERVICE_STOP |
                                     SERVICE_PAUSE_CONTINUE |
                                     SERVICE_INTERROGATE |
                                     SERVICE_USER_DEFINED_CONTROL
            }
            
            [StructLayout(LayoutKind.Sequential)]
            public class ServiceConfig
            {
                public ServiceType dwServiceType;
                public ServiceStartType dwStartType;
                public ServiceErrorControl dwErrorControl;
                public string lpBinaryPathName;
                public string lpLoadOrderGroup;
                public uint dwTagId;
                public string lpDependencies;
                public string lpServiceStartName;
                public string lpDisplayName;
            }

            public enum ServiceType : uint
            {
                SERVICE_FILE_SYSTEM_DRIVER = 0x00000002,
                SERVICE_KERNEL_DRIVER = 0x00000001,
                SERVICE_WIN32_OWN_PROCESS = 0x00000010,
                SERVICE_WIN32_SHARE_PROCESS = 0x00000020,
                SERVICE_INTERACTIVE_PROCESS = 0x00000100
            }
            
            public enum ServiceStartType : uint
            {
                SERVICE_AUTO_START = 0x00000002,
                SERVICE_BOOT_START = 0x00000000,
                SERVICE_DEMAND_START = 0x00000003,
                SERVICE_DISABLED = 0x00000004,
                SERVICE_SYSTEM_START = 0x00000001
            }
            
            public enum ServiceErrorControl : uint
            {
                SERVICE_ERROR_CRITICAL = 0x00000003,
                SERVICE_ERROR_IGNORE = 0x00000000,
                SERVICE_ERROR_NORMAL = 0x00000001,
                SERVICE_ERROR_SEVERE = 0x00000002
            }
        }

        public static class WinNT
        {
            public const uint PAGE_NOACCESS = 0x01;
            public const uint PAGE_READONLY = 0x02;
            public const uint PAGE_READWRITE = 0x04;
            public const uint PAGE_WRITECOPY = 0x08;
            public const uint PAGE_EXECUTE = 0x10;
            public const uint PAGE_EXECUTE_READ = 0x20;
            public const uint PAGE_EXECUTE_READWRITE = 0x40;
            public const uint PAGE_EXECUTE_WRITECOPY = 0x80;
            public const uint PAGE_GUARD = 0x100;
            public const uint PAGE_NOCACHE = 0x200;
            public const uint PAGE_WRITECOMBINE = 0x400;
            public const uint PAGE_TARGETS_INVALID = 0x40000000;
            public const uint PAGE_TARGETS_NO_UPDATE = 0x40000000;

            public const uint SEC_COMMIT = 0x08000000;
            public const uint SEC_IMAGE = 0x1000000;
            public const uint SEC_IMAGE_NO_EXECUTE = 0x11000000;
            public const uint SEC_LARGE_PAGES = 0x80000000;
            public const uint SEC_NOCACHE = 0x10000000;
            public const uint SEC_RESERVE = 0x4000000;
            public const uint SEC_WRITECOMBINE = 0x40000000;

            [Flags]
            public enum ACCESS_MASK : uint
            {
                DELETE = 0x00010000,
                READ_CONTROL = 0x00020000,
                WRITE_DAC = 0x00040000,
                WRITE_OWNER = 0x00080000,
                SYNCHRONIZE = 0x00100000,
                STANDARD_RIGHTS_REQUIRED = 0x000F0000,
                STANDARD_RIGHTS_READ = 0x00020000,
                STANDARD_RIGHTS_WRITE = 0x00020000,
                STANDARD_RIGHTS_EXECUTE = 0x00020000,
                STANDARD_RIGHTS_ALL = 0x001F0000,
                SPECIFIC_RIGHTS_ALL = 0x0000FFF,
                ACCESS_SYSTEM_SECURITY = 0x01000000,
                MAXIMUM_ALLOWED = 0x02000000,
                GENERIC_READ = 0x80000000,
                GENERIC_WRITE = 0x40000000,
                GENERIC_EXECUTE = 0x20000000,
                GENERIC_ALL = 0x10000000,
                DESKTOP_READOBJECTS = 0x00000001,
                DESKTOP_CREATEWINDOW = 0x00000002,
                DESKTOP_CREATEMENU = 0x00000004,
                DESKTOP_HOOKCONTROL = 0x00000008,
                DESKTOP_JOURNALRECORD = 0x00000010,
                DESKTOP_JOURNALPLAYBACK = 0x00000020,
                DESKTOP_ENUMERATE = 0x00000040,
                DESKTOP_WRITEOBJECTS = 0x00000080,
                DESKTOP_SWITCHDESKTOP = 0x00000100,
                WINSTA_ENUMDESKTOPS = 0x00000001,
                WINSTA_READATTRIBUTES = 0x00000002,
                WINSTA_ACCESSCLIPBOARD = 0x00000004,
                WINSTA_CREATEDESKTOP = 0x00000008,
                WINSTA_WRITEATTRIBUTES = 0x00000010,
                WINSTA_ACCESSGLOBALATOMS = 0x00000020,
                WINSTA_EXITWINDOWS = 0x00000040,
                WINSTA_ENUMERATE = 0x00000100,
                WINSTA_READSCREEN = 0x00000200,
                WINSTA_ALL_ACCESS = 0x0000037F,

                SECTION_ALL_ACCESS = 0x10000000,
                SECTION_QUERY = 0x0001,
                SECTION_MAP_WRITE = 0x0002,
                SECTION_MAP_READ = 0x0004,
                SECTION_MAP_EXECUTE = 0x0008,
                SECTION_EXTEND_SIZE = 0x0010
        };
            
            [StructLayout(LayoutKind.Sequential)]
            public struct SECURITY_ATTRIBUTES
            {
                public int nLength;
                public IntPtr lpSecurityDescriptor;
                public int bInheritHandle;
            }
            
            public enum SECURITY_IMPERSONATION_LEVEL
            {
                SecurityAnonymous,
                SecurityIdentification,
                SecurityImpersonation,
                SecurityDelegation
            }
            
            public enum TOKEN_TYPE
            {
                TokenPrimary = 1,
                TokenImpersonation = 2
            }
        }
    }
}