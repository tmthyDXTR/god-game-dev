# Changelog

All notable changes to Hex Grid Overlay will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-11-08

### Added
- Initial release of Hex Grid Overlay system
- HexMapSettings ScriptableObject for configuration
- HexGridOverlay component for grid generation and management
- HexRenderer custom UI.Graphic component for efficient hexagon rendering
- HexGridEditor runtime editor with H key toggle
- MouseWheelSlider component for precise value adjustment via scroll
- Support for both pointy-top and flat-top hex orientations
- Isometric 3D rotation on X, Y, Z axes
- Real-time grid regeneration
- Configurable grid dimensions (1-20 width/height)
- Configurable hex size (10-200 units)
- Configurable first tile position with negative value support
- Customizable border color and width
- Show/hide grid toggle
- Mesh-based rendering for performance
- Dedicated hex container prevents map background rotation
- Mouse wheel support using Unity Input System
- Whole number mode for integer-only values
- 0.1 increment mode for hex size precision
- Compact editor UI with small fonts and tight spacing
- Comprehensive documentation and examples

### Technical Details
- Uses Unity UI Canvas and RectTransform
- Requires TextMesh Pro for UI text
- Requires Input System package for mouse wheel
- Namespace: HexGridOverlay.Runtime
- Efficient VertexHelper mesh generation
- DestroyImmediate for immediate cleanup
- Reflection-based field wiring support

### Package Structure
- Runtime/ - Core components and runtime scripts
- Editor/ - Editor-only scripts (currently unused)
- Documentation/ - Additional documentation
- package.json - Unity package manifest
- README.md - Complete usage documentation
- CHANGELOG.md - Version history

[1.0.0]: https://github.com/yourusername/hex-grid-overlay/releases/tag/v1.0.0
