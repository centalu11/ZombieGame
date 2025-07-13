# Debug Features & Temporary Implementations

This document tracks debug features, temporary implementations, and planned changes that need to be addressed before release.

## 🔧 Debug Features (To Be Enhanced/Moved)

### Ghost Mode
**Current Implementation:** Basic collision bypass with G key toggle
**Status:** ⚠️ Temporary - Needs Enhancement

**Current Features:**
- Toggle with G key
- Disables CharacterController collision
- Allows passing through walls/objects
- Basic movement (ground-based)

**Planned Enhancements:**
- [ ] **Free-float movement** - Allow movement in all directions (X, Y, Z)
- [ ] **Speed controls** - Adjustable ghost mode movement speed
- [ ] **Smooth transitions** - Better entry/exit animations
- [ ] **Visual feedback** - Ghost effect (transparency, glow, etc.)
- [ ] **Camera mode** - Optional free-look camera in ghost mode

**Integration Plans:**
- [ ] **Move to Debug Settings Menu** - Remove direct G key binding
- [ ] **Debug-only feature** - Should NOT be accessible in release builds
- [ ] **Permission system** - Only available in development/debug modes

---

## 🎛️ Debug Settings System (Planned)

### Debug Settings Menu
**Status:** 📋 Planned Feature

**Requirements:**
- Separate from main game settings
- Only accessible in development builds
- Hidden in release builds
- Accessible via debug key combination (e.g., Ctrl+Shift+D)

**Planned Debug Options:**
- [ ] **Ghost Mode Toggle** - Enable/disable ghost mode functionality
- [ ] **Ghost Mode Hotkey** - Customize ghost mode toggle key (default: G)
- [ ] **Movement Speed Multipliers** - Debug speed adjustments
- [ ] **Collision Visualization** - Show collision boxes/spheres
- [ ] **AI Debug Info** - Show zombie states, paths, etc.
- [ ] **Performance Metrics** - FPS, memory usage, etc.
- [ ] **Level Tools** - Quick level reload, scene switching
- [ ] **Console Commands** - In-game command line interface

### Implementation Notes
```csharp
// Planned structure
public class DebugSettingsManager : MonoBehaviour
{
    [SerializeField] private bool enableGhostMode = false;
    [SerializeField] private KeyCode ghostModeToggle = KeyCode.G;
    
    // Only compile in development builds
    #if DEVELOPMENT_BUILD || UNITY_EDITOR
    // Debug functionality here
    #endif
}
```

---

## 🚧 Temporary Implementations

### Input System
**Current:** Direct key bindings in input actions
**Planned:** Debug settings-controlled bindings

### Movement System
**Current:** Simple Transform.position manipulation in ghost mode
**Planned:** Proper 3D free-flight system with physics

### Visual Feedback
**Current:** Console debug messages only
**Planned:** UI indicators, visual effects, HUD elements

---

## 📝 TODO: Before Release

### Critical Changes
- [ ] Remove ghost mode from default input actions
- [ ] Implement debug settings menu system
- [ ] Add conditional compilation for debug features
- [ ] Create debug build configuration
- [ ] Add visual feedback systems
- [ ] Implement proper 3D movement for ghost mode

### Code Cleanup
- [ ] Remove debug logs from ghost mode
- [ ] Extract debug features to separate assemblies
- [ ] Add proper documentation for debug systems
- [ ] Create debug feature toggle system

### Testing Requirements
- [ ] Verify ghost mode works in all scenes
- [ ] Test debug menu accessibility
- [ ] Ensure debug features are disabled in release builds
- [ ] Performance testing with debug features enabled/disabled

---

## 🎮 Game vs Debug Settings

### Game Settings (Release)
- Graphics quality
- Audio levels
- Control bindings
- Accessibility options
- Language/localization

### Debug Settings (Development Only)
- Ghost mode options
- Developer tools
- Performance profiling
- AI debugging
- Level design tools
- Console commands

---

## 📋 Implementation Priority

1. **High Priority**
   - Debug settings menu framework
   - Ghost mode enhancements (3D movement)
   - Conditional compilation setup

2. **Medium Priority**
   - Visual feedback systems
   - Debug UI elements
   - Performance monitoring tools

3. **Low Priority**
   - Advanced debug commands
   - Level design tools
   - AI visualization tools

---

## 🔍 Notes for Developers

- All debug features should be wrapped in `#if DEVELOPMENT_BUILD || UNITY_EDITOR`
- Debug settings should persist between sessions (PlayerPrefs/JSON)
- Consider performance impact of debug features
- Maintain separate assembly definitions for debug code
- Document all debug commands and shortcuts

---

*Last Updated: [Current Date]*
*Next Review: Before Alpha Release* 