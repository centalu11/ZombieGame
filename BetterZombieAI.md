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
- When a sound is "played" (e.g., footsteps, gunfire), call a public method `HearSound(Vector3 sourcePosition, float soundStrength)`:
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

## 🧠 Chase Memory Timer

Implement a **chase memory system** in both detection modules or in a shared `ChaseController.cs`:

### 📌 Features

- When an enemy **starts chasing** a player:
  - If the player becomes hidden (e.g., crouches in cover, enters shadow, etc.), start a **chase memory timer** (`chaseForgetTime`, configurable per enemy).
  - During this time, the enemy still **remembers the last seen location** and will **continue to pursue**.
  - If the timer expires without re-detecting the player, the enemy will **exit chase mode** and re-enter **detection mode**.
  - If the player is re-detected during this time, **reset the timer**.

- Also triggered if player escapes the **chase distance threshold**:
  - Same timer and behavior applies.

- When in chase mode:
  - Enemy gets **enhanced hearing/vision range** (configurable per enemy type).

---

## 🤝 Mob Detection / Enemy Communication

Add a **mob detection system** where enemies can **alert nearby allies** when detecting the player.

### 📌 Features

- Each enemy has:
  - `mobAlertRadius`: how far it can alert others (configurable)
  - `canReceiveAlert`: if the enemy type can be alerted by others
  - `canBroadcastAlert`: if the enemy type can alert others

- When an enemy detects the player:
  - Alert all other enemies within `mobAlertRadius`
  - Those enemies instantly **enter alert or chase** state depending on enemy type

- Different types (examples):
  - **Zombies**: small radius, only nearby zombies get alerted
  - **Humans**: large radius, quick coordination
  - **Screamer zombie**: upon alert, triggers **large-radius scream** that pulls more enemies from further away

- Optional:
  - Visualize alert zones with gizmos
  - Add delay for more realism (e.g., reaction time, scream animation before effect)

---


## ⚙️ Enemy Behavior States

### 1. 🧍 Idle / Wandering State
- Enemy patrols randomly or remains idle within a designated area.
- If a **distance check** or **trigger collider** is activated (due to audio or visual presence of the player), the enemy transitions to **Detection State** (if within the further distance), or instant **Chasing State** (if within the near distance).

---

### 2. 👀 Detection State
- Triggered when the player is detected through:
  - **Far vision cone** (not immediate chase range), or
  - **Outer audio detection radius**.
- While in this state:
  - The enemy focuses attention toward the **last seen or heard location**.
  - If the player is **seen or heard again** during this state — even within the *far range* — the enemy **instantly transitions to Chasing State**.
- A **detection timer** (e.g., 3 seconds — customizable per enemy type) runs in this state.
  - If the timer expires without reacquiring the player, the enemy returns to **Idle/Wandering**.
- This state is also **re-triggered after the Chasing State ends**, if the player was not caught and is no longer detected.

---

### 3. 🏃‍♂️ Chasing State
- Enemy has positively identified the player and actively chases them.
- If the player is lost (e.g., breaks line of sight or leaves audio range), a **chase timeout timer** begins.
  - If the player is not reacquired within this time, transition to **Detection State**.

---

### 4. 🗡️ Attacking State
- Enemy enters this state when close enough to the player.
- Executes attack behavior based on type (e.g., melee swipe, gunfire).
- After attacking:
  - If the player moves out of range but still visible, continue **Chasing**.
  - If the player escapes completely, return to **Detection** or **Idle**, depending on timers and triggers.

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
4. `ChaseController.cs` or integrated chase logic
5. `MobAlertSystem.cs` or integrated mob broadcast feature
6. Usage example on an enemy prefab with both detection components attached

---

## 💡 Optional Enhancements

- Use **ScriptableObjects** to define detection profiles per enemy type
- Pool raycasts if managing large enemy numbers
- Implement **gizmos** to visualize detection zones in editor for debugging
- Add **animation triggers and blend trees** for realistic state transitions (e.g., from idle to alerted to chase)
