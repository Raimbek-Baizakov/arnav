# ARNAV - AR Navigation App

ARNAV is an augmented reality navigation application for Android devices that provides real-time GPS-based navigation with visual AR guidance.

## Features

- **Interactive Route Planning**: Select start and end points on an interactive web-based map
- **AR Navigation**: Real-time 3D arrow guidance overlaid on camera feed
- **HUD Display**: Speed, distance to next turn, and remaining distance information
- **Device Integration**: GPS location, compass heading, and gyroscope support
- **Cross-Platform**: Built with Unity for Android deployment
- **Route Persistence**: Save and restore routes between sessions

## Requirements

- Unity 2021.3.16f1 or later
- Android SDK (API level 24+)
- Android device with GPS and compass sensors
- Internet connection for map services

## Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/arnav.git
   cd arnav
   ```

2. Open the project in Unity Hub with Unity 2021.3.16f1

3. Ensure Android build support is installed in Unity

4. Build and deploy to Android device:
   - File > Build Settings > Android
   - Select your device or create APK

## Usage

1. **Launch the app** and grant necessary permissions (camera, location)

2. **Route Planning**:
   - Navigate to the map scene
   - Select start and end points on the interactive map
   - Confirm route to proceed

3. **AR Navigation**:
   - Switch to camera scene
   - Follow the 3D arrow overlay
   - Monitor HUD for speed and distance information

4. **Navigation Controls**:
   - Use back button to return to route planning
   - Routes are automatically saved and can be restored

## Project Structure

```
Assets/
├── Scripts/                 # Core game logic
│   ├── HudArrowController.cs      # HUD display management
│   ├── _3dHudArrow.cs             # 3D arrow rendering
│   ├── SensorsService.cs          # GPS/compass sensor handling
│   ├── RouteSession.cs            # Route data management
│   ├── GeoUtils.cs                # Geographic calculations
│   ├── RouteMapWebViewController.cs # WebView map controller
│   ├── CameraFeed.cs              # Camera device management
│   ├── CompassController.cs       # Compass UI controller
│   ├── PermissionManager.cs       # Android permissions
│   └── ...
├── Scenes/                 # Unity scenes
├── StreamingAssets/        # Web assets (map.html)
├── Plugins/               # Third-party plugins
└── Resources/             # Game assets
```

## Technologies Used

- **Unity Engine**: Game development framework
- **C#**: Primary programming language
- **WebView**: Embedded web content for maps
- **TextMeshPro**: Advanced text rendering
- **Android JNI**: Native Android integration
- **GPS/Compass APIs**: Device sensor integration

## Architecture

The application follows a modular architecture with clear separation of concerns:

- **Sensor Layer**: Handles device sensors and permissions
- **Data Layer**: Manages route and session data
- **UI Layer**: Controls user interface and AR overlays
- **Integration Layer**: Manages WebView and external services

## Permissions Required

- **Camera**: For AR overlay on camera feed
- **Fine Location**: For precise GPS positioning
- **Coarse Location**: For approximate location services

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test on Android device
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.