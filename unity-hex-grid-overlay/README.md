# Hex Grid Overlay for Unity

A flexible and customizable hexagonal grid overlay system for Unity UI Canvas. Features real-time configuration, support for both pointy-top and flat-top hex orientations, and isometric rotation capabilities.

## Features

- Hexagonal grid overlay on Unity UI Canvas
- Runtime editor with H key toggle for live adjustment
- Dual orientation support: pointy-top and flat-top hexagons
- Isometric 3D rotation (X, Y, Z axes)
- Mouse wheel support for precise value adjustments
- ScriptableObject-based configuration
- Efficient mesh-based rendering using UI.Graphic system
- Fully customizable: position, size, grid dimensions, colors, border width

## Requirements

- Unity 2021.3 or later
- TextMesh Pro
- Input System package

## Installation

### Option 1: Unity Package Manager (Git URL)

1. Open Unity Package Manager (Window > Package Manager)
2. Click the + button and select "Add package from git URL"
3. Enter the repository URL
4. Click Add

### Option 2: Manual Installation

1. Copy the `HexGridOverlay` folder into your project's `Assets` directory
2. Unity will automatically import the package

## Quick Start

### 1. Create Hex Map Settings

Right-click in Project window:
- Create > HexGridOverlay > Hex Map Settings
- Configure your default settings

### 2. Add to Scene

Add to a UI Canvas container:

```csharp
// In your UI setup code
GameObject mapContainer = // your map container RectTransform

// Create hex grid overlay
GameObject hexOverlayObj = new GameObject("HexGridOverlay");
hexOverlayObj.transform.SetParent(mapContainer.transform, false);
HexGridOverlay hexOverlay = hexOverlayObj.AddComponent<HexGridOverlay>();

// Assign settings via reflection or Inspector
hexOverlay.hexSettings = yourHexMapSettings;
hexOverlay.mapContainer = mapContainer.GetComponent<RectTransform>();

// Create runtime editor
GameObject editorObj = new GameObject("HexGridEditor");
editorObj.transform.SetParent(canvas.transform, false);
HexGridEditor editor = editorObj.AddComponent<HexGridEditor>();
editor.hexSettings = yourHexMapSettings;
editor.hexGridOverlay = hexOverlay;
```

### 3. Use Runtime Editor

- Press **H** in Play mode to toggle the editor panel
- Adjust settings in real-time:
  - First Tile Position (X, Y)
  - Hex Size
  - Grid dimensions (Width, Height)
  - Border Width
  - Pointy Top toggle
  - Isometric Rotation (Tilt Back, Tilt Side, Spin)
  - Show/Hide grid
- Use mouse wheel over sliders for precise adjustments

## Configuration

### HexMapSettings (ScriptableObject)

**Grid Layout:**
- `firstTilePosition` (Vector2): Starting position for hex (0,0)
- `hexSize` (float, 10-200): Distance from center to vertex
- `gridWidth` (int, 1-20): Number of hexagons horizontally
- `gridHeight` (int, 1-20): Number of hexagons vertically
- `pointyTop` (bool): Hex orientation (true = pointy top, false = flat top)

**Isometric Rotation:**
- `rotationX` (float, -180 to 180): Tilt back/forward
- `rotationY` (float, -180 to 180): Tilt left/right
- `rotationZ` (float, -180 to 180): Spin clockwise/counter-clockwise

**Visual Settings:**
- `hexBorderColor` (Color): Border line color with alpha
- `borderWidth` (float, 1-5): Line thickness
- `showHexGrid` (bool): Toggle visibility

## Architecture

### Core Components

**HexMapSettings.cs**
- ScriptableObject for persistent configuration
- Exposes all grid parameters
- Can be created via Create menu

**HexGridOverlay.cs**
- Main grid generation and management
- Creates dedicated hex container for rotation
- Handles grid regeneration
- Public `GenerateHexGrid()` method for runtime updates

**HexRenderer.cs**
- Custom UI.Graphic component
- Renders individual hexagons using VertexHelper
- Efficient mesh-based rendering
- Supports both hex orientations

**HexGridEditor.cs**
- Runtime editor UI with H key toggle
- Creates sliders, toggles, and labels programmatically
- Includes MouseWheelSlider component for scroll support
- Right-side panel with semi-transparent background

**MouseWheelSlider.cs**
- IPointerEnterHandler/IPointerExitHandler for hover detection
- Uses Input System for mouse scroll
- Configurable increment per scroll

## API Reference

### HexGridOverlay

```csharp
public class HexGridOverlay : MonoBehaviour
{
    // Public method to regenerate grid with current settings
    public void GenerateHexGrid()
}
```

### HexMapSettings

All fields are public and can be modified at runtime. Changes take effect when `GenerateHexGrid()` is called.

## Usage Examples

### Basic Setup

```csharp
// Create settings
HexMapSettings settings = ScriptableObject.CreateInstance<HexMapSettings>();
settings.hexSize = 50f;
settings.gridWidth = 10;
settings.gridHeight = 8;
settings.pointyTop = true;

// Apply to overlay
hexOverlay.hexSettings = settings;
hexOverlay.GenerateHexGrid();
```

### Isometric View

```csharp
// Typical isometric configuration
settings.rotationX = 30f;  // Tilt back
settings.rotationY = 0f;   // No side tilt
settings.rotationZ = 45f;  // Rotate 45 degrees
hexOverlay.GenerateHexGrid();
```

### Runtime Color Change

```csharp
// Change border color
settings.hexBorderColor = new Color(1f, 0f, 0f, 0.5f); // Red, 50% alpha
hexOverlay.GenerateHexGrid();
```

## Hex Grid Math

**Pointy-Top Hexagons:**
- Width: `hexSize * sqrt(3)`
- Height: `hexSize * 2`
- Horizontal spacing: `width`
- Vertical spacing: `height * 0.75`
- Odd rows offset right by `spacing * 0.5`

**Flat-Top Hexagons:**
- Width: `hexSize * 2`
- Height: `hexSize * sqrt(3)`
- Horizontal spacing: `width * 0.75`
- Vertical spacing: `height`
- Odd columns offset down by `spacing * 0.5`

## Performance Considerations

- Grid regeneration destroys and recreates all hex objects
- Use `DestroyImmediate` for immediate cleanup in editor
- Each hex is a separate GameObject with HexRenderer component
- Mesh generation is efficient but large grids (>400 hexes) may impact performance
- Consider object pooling for dynamic grids

## Troubleshooting

**Hexagons not appearing:**
- Ensure `showHexGrid` is true
- Check that `mapContainer` is assigned
- Verify Canvas has proper render mode

**Rotation not working:**
- Rotation is applied to hex container, not map container
- Map background remains unrotated by design

**Mouse wheel not responding:**
- Hover over slider before scrolling
- Check Input System package is installed
- Verify Mouse.current is not null

## Namespace

All components use namespace: `HexGridOverlay.Runtime`

Add to your scripts:
```csharp
using HexGridOverlay.Runtime;
```

## License

Free to use and modify for personal and commercial projects.

## Support

For issues, questions, or contributions, please refer to the repository.

## Version

1.0.0 - Initial release
