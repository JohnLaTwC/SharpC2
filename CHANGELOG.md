# Changelog
All notable changes to this project will be documented in this file.

## [0.5.0] - 2021-11-24
### Added
- SMB Drone.
### Changed
- Improved autocompletion in Client.

## [0.4.2] - 2021-09-01
### Added
- New HostedFiles API endpoint to host arbitrary files on the default HTTP Handler.
- New Client screen to call said API.
- PowerShell Drone Module.

## [0.4.0] - 2021-08-30
### Added
- Team Server now uses a C2 Profile to control Drone behaviours.
- New C2Lint tool to validate C2 Profiles.
- A few extra `stdapi` commands: `cat`, `upload`, `rm`, `rmdir` and `mkdir`.
### Changed
- Simplified Payload API by moving options to the C2 Profile.

## [0.3.4] - 2021-08-21
### Changed
- Drone metadata now contains the integrity of its host process.
- Drone arch is now an enum, rather than a string.

## [0.3.3] - 2021-08-20
### Changed
- Fix bug when handling message from first-time-seen Drone.

## [0.3.2] - 2021-08-19
### Changed
- Payload API can now generate donut shellcode using [DonutCS](https://github.com/n1xbyte/donutCS).
- Payload API can now generate service binary payloads.
- Payload API can now generate PowerShell payloads.

## [0.2.6] - 2021-08-18
### Added
- `ps` command to `stdapi`.

## [0.2.5] - 2021-08-18
### Changed
- Moved most Drone functionality out into external DLLs.
- The `stdapi` is pushed on initial check-in.
- AMSI and ETW bypasses now use API hooking.
### Added
- [Ceri Coburn's](https://twitter.com/_EthicalChaos_) [MinHook.NET](https://github.com/CCob/MinHook.NET) engine.
- D/Invoke's Injection API.
- `shinject` command to `stdapi`.

## [0.2.4] - 2021-08-14
### Added
- Load external Drone modules at runtime.
- DroneModuleLoaded SignalR call.

## [0.2.3] - 2021-08-13
### Added
- Reverse port forwards.

## [0.2.2] - 2021-08-13
### Added
- Token commands. Credit to [@MDSecLabs](https://twitter.com/MDSecLabs) for their idea of a "token store".

## [0.2.1] - 2021-08-13
### Added
- Null-reference check to the Handler string when generating payloads.

## [0.2.0] - 2021-08-09
### Changed
- Moved payload generation to its own API endpoint.
- Exposed additional payload option for DllExport name.
### Added
- New Client screen for payload generation.

## [0.1.0] - 2021-08-07
### Added
- New Handler API endpoint to load a Handler on the Team Server during runtime.
- New Client command on the `handlers` screen to call said API.
### Changed
- Put authentication on the SignalR hub.

## [0.0.2] - 2021-08-06
### Changed
- Fix bug when starting the default HTTP Handler.

## [0.0.1] - 2021-08-06
### Added
- First release.