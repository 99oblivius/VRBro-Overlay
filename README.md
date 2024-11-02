# VRBro Overlay for SteamVR
![VRBro Banner](assets/VRBro_banner.png)

## Introduction
VRBro Overlay is a SteamVR companion application that provides seamless control of OBS Studio directly from within VR. Working in conjunction with the [VRBro Plugin for OBS](https://github.com/99oblivius/VRBro-plugin), it allows content creators to manage their streams and recordings without leaving their VR experience.

## Features
- Intuitive and adjustable VR controller bindings for OBS control
- Wrist-mounted menu for scene access
- Support for OBS functionalities:
  - Start/Stop Streaming
  - Start/Stop Recording
  - Start/Stop Replay Buffer
  - Save Replay Buffer
  - Split Recording File

## Prerequisites
- SteamVR
- OBS Studio with [VRBro Plugin](https://github.com/99oblivius/VRBro-plugin) installed
- Compatible VR controllers (Tested with Valve Index Controllers)

## Installation
1. A future release is planned on Steam

## Configuration
1. Start the OBS VRBro Plugin server first and then launch the Overlay
2. For custom connections outside localhost, use the tray icon (default: 127.0.0.1:33390)
3. All controller bindings can be customized through SteamVR settings

## Default Controls
- Control OBS through the Dahsboard GUI
- Long press Left A: Save Buffer
- Left B + Left A + Left Grab: Toggle scene selection menu
- Menu Interface: Point and click with your controller
- Additional bindings configurable through SteamVR

## Troubleshooting
1. Ensure OBS Studio is running with the VRBro Plugin installed before you launch SteamVR
2. Check the VRBro Plugin server settings in OBS (Tools -> VRBro Server Settings) (edit only with basic networking knowledge)
3. Verify your client settings via the VRBro system tray icon (edit only with basic networking knowledge)

## Contributing
Contributions are welcome! Feel free to submit issues or pull requests.

## Acknowledgements
- Valve for OpenVR SDK
