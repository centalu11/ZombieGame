
I'm building a Unity game using C# with a **post-apocalyptic, sandbox survival theme** that features zombies, base-building, and town development.

---

🎮 Game Overview

- The game is in **first-person perspective**.
- It starts in a **normal, modern-day residential street** (e.g., inspired by places like Makati or suburban neighborhoods) before chaos breaks out.
- Players explore a **dangerous open world** filled with zombies and limited resources.
- The player can **gather materials**, **fortify structures**, **build multiple bases**, and eventually expand to create an entire **town or community**.

---

🔧 Safe Zones / Base Building Concept

❗️Important: Safe zones are **not magically safe**. Zombies **can** and **should** be able to enter any place **unless** the player actively builds defenses.

This is a **player-driven sandbox** where:
- The player manually places walls, fences, doors, turrets, and traps.
- Fortifications **can be damaged or destroyed** by zombies.
- Players can build **multiple outposts** and grow them into towns.
- The endgame includes building a **functioning community** with **NPCs who have jobs** (e.g., doctor, engineer, guard).

The core gameplay loop includes:
- Exploring
- Gathering
- Building
- Defending
- Expanding

---

✅ What I Want Help With

Please help me build this **step-by-step**, only providing logic/code for the following systems. I will handle scene setup, prefab creation, material assignments, etc., **manually** in Unity.

---

🔨 MVP Phase Breakdown

🔹 Phase 1 — Core Gameplay
1. First-person **player controller**:
   - Smooth walking, jumping, sprinting, crouching
   - Mouse-look with head-bobbing
   - Camera collision detection
2. Basic **resource collection** system
   - Use raycasting or trigger volumes
   - Add items to inventory
3. Simple **inventory system** with UI
   - Show items collected (food, water, materials)
4. **Building placement system**:
   - Place placeholder structures (e.g., cubes or walls)
   - Support grid or surface snapping
   - Structures should have health/durability
5. **Zombie enemy** using **NavMesh**:
   - Wanders and chases player when nearby
   - Attacks structures if player is inside
6. **Save/load system**:
   - Save inventory, structure placements, and player position
   - Use JSON or PlayerPrefs

---

🔮 Future Features to Keep in Mind (Code Should Be Scalable Toward These)

- Recruitable **NPCs** with professions (e.g., doctor, guard)
- **Hard dungeons** with elite enemies and rare loot
- **Random world events** (e.g., ambushes, zombie hordes, trader visits)
- **Town-building** system with assignable zones/buildings
- **Power system** (generators, lights, crafting stations)
- Day/night cycle with **difficulty scaling**

---

📁 Project Structure

Please help me maintain a clean, scalable folder and namespace structure. Example:

Assets/
  ├── Scripts/
  │    ├── Player/
  │    ├── Building/
  │    ├── Enemies/
  │    ├── Inventory/
  │    ├── Systems/
  │    └── UI/
  ├── Prefabs/
  ├── Materials/
  ├── Audio/
  ├── Scenes/
  └── Resources/

---

📌 My Workflow and Expectations

Please remember:
- I will **manually create** all prefabs, colliders, animations, and materials in Unity.
- Your job is to assist with **code**, logic, and system scaffolding.
- Be modular, clean, and extensible.
- Use modern Unity patterns (e.g., ScriptableObjects where useful).
- Provide comments and region markers for clarity.
- Suggest which Unity packages I may need (e.g., Input System, NavMesh, Cinemachine, etc.).

---

🎨 Free Asset Recommendations

Please suggest **free** or beginner-friendly Unity Asset Store packages or external sources for:
- Post-apocalyptic or urban environment props
- Modular buildings (walls, fences, doors, street assets)
- Stylized or basic zombie models
- Free UI icons or packs (for inventory, health, hunger, etc.)
- Mixamo animations (walk, attack, idle)

Make sure they are:
- Lightweight and suitable for prototyping
- Easy to replace later with premium assets
- Unity-compatible (URP preferred)

---

✅ Let’s Begin

Start by helping me create a **first-person player controller**:
- Smooth walking, jumping, crouching, and sprinting
- Mouse look
- Camera collision handling
