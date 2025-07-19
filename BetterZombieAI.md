# Prompt for Modular Enemy Detection System in Unity

## 🎯 Objective

Create a **modular** and **performance-optimized** enemy detection system in Unity consisting of:

- 🔊 Audio Detection Module
- 👁️ Vision Detection Module

The system should be **extensible**, **configurable per enemy type** (e.g., zombie vs human), and scalable to many enemies.

---

## 🔊 Audio Detection Script

Create a script `AudioDetector.cs` with the following features:

### 📌 Functionalities

- Accepts two float parameters:
  - `innerDetectionRadius`: radius for *instant detection* of the player.
  - `outerDetectionRadius`: radius for *alert-only* (zombie investigates sound origin).

- Uses **Unity Physics.OverlapSphere** or **trigger colliders** for spatial checks.
- When a sound is "played" (e.g., footsteps, gunfire), call a public method `HearSound(Vector3 sourcePosition, float soundStrength)`.
  - Sound strength should determine if player is heard in outer or inner zone.
  - Use **squared distance checks** for performance.

- For outer radius:
  - Enemy becomes **alerted**, faces sound direction and **walks to investigate** the sound source.
- For inner radius:
  - Player is instantly detected and **chased**.

### ⚙️ Optimization

- Use **layer masks** to filter relevant sound sources.
- Use **Physics.OverlapSphereNonAlloc** to avoid garbage collection.
- Avoid continuous checks unless a sound is triggered.

---

## 👁️ Vision Detection Script

Create a script `VisionDetector.cs` with the following features:

### 📌 Functionalities

- Accepts:
  - `nearVisionRange`: close detection range
  - `farVisionRange`: extended detection range
  - `visionAngle`: field of view in degrees
  - `detectionTimeThreshold`: time in seconds required before enemy acts on far-vision detection

- Vision cone/fan check:
  - Use **Vector3.Angle** for angle detection
  - Use **Raycast** or **RaycastAll** toward the player to check LOS (line-of-sight)
  - Distance-based logic:
    - **Near Range**:
      - Player is instantly seen and chased
    - **Far Range**:
      - Enemy **watches** player direction
      - Starts a timer
      - If player remains visible beyond `detectionTimeThreshold`, enemy walks to last seen location
      - If nothing is found after arrival + `waitTime`, return to patrolling or idle or wander (depends on what I want to that enemy)

- Configurable behaviors (via enum or bool flags):
  - `returnToPreviousPosition`
  - `stayAndGuardLastSeenPosition`

### ⚙️ Optimization

- Only activate raycasting if player is within a **trigger zone** (e.g., invisible fan-shaped area based on `farVisionRange`)
- Use **distance checks** to avoid constant raycasting
- Use **sparse updates** (e.g., run logic every 0.2–0.5s instead of every frame)
- Use **dot product** checks instead of `Vector3.Angle` where possible for better performance

---

## 🧩 Modular Design & Extensibility

- Both detection scripts should be **independent MonoBehaviours**
- Each enemy prefab should have both components attached and configured per type
- Add optional **StealthEvaluator.cs** in future to affect visibility via:
  - Shadow/light check
  - Bush/cover check
  - Player stance (crouched/walking/running)

---

## 🔄 Player Integration

- Player character should have a `SoundEmitter.cs` script that calls `HearSound(...)` on nearby enemies.
- Can simulate stealth by reducing sound emission or using tags (e.g., "Hidden" when in bush).

---

## ✅ Deliverables

1. `AudioDetector.cs`
2. `VisionDetector.cs`
3. `DetectionData.cs` (optional data container for values)
4. Usage example on an enemy prefab with both detection components attached

---

## 💡 Optional Enhancements

- Use **ScriptableObjects** to define detection profiles per enemy type
- Pool raycasts if managing large enemy numbers
- Implement **gizmos** to visualize detection zones in editor for debugging
