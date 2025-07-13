# The Last Haven - Game Design Document

## Game Overview
"The Last Haven" is a narrative-driven zombie survival game that combines intense action, resource management, and meaningful character relationships. Set during the outbreak of a zombie apocalypse, players navigate through personal stories of survival, loss, and hope.

## Core Game Loop
- Survive in a hostile environment
- Gather resources and manage supplies
- Complete story missions and side objectives
- Build and maintain a safe haven
- Develop relationships with survivors

## Camera System
- **Perspective Switching**:
  - Dynamic switching between first-person and third-person views
  - Hotkey for quick perspective changes
  - Automatic switching for specific actions (e.g., driving, construction)
  - Persistent camera preference settings

- **First-Person View**:
  - Enhanced immersion for close combat
  - Precise aiming for gunplay
  - Detailed interaction with objects and resources
  - Better spatial awareness for construction

- **Third-Person View**:
  - Better situational awareness in combat
  - Improved visibility during stealth
  - Enhanced movement control in parkour/climbing
  - Easier character positioning during construction

- **Context-Specific Features**:
  - Custom FOV settings for each perspective
  - Perspective-specific control schemes
  - Adjustable camera sensitivity per view
  - Optional motion smoothing
  - Debug camera mode for development

## Story Structure

### Act 1: The Outbreak (Interactive Narrative Introduction)
- **Narrative Style**: Story sequences are presented through interactive gameplay segments similar to narrative adventure games (like The Walking Dead, Detroit: Become Human)
- **Player Agency**: Players participate in story moments through:
  - Quick-time events during intense sequences
  - Dialogue interactions
  - Environmental exploration
  - Simple but meaningful actions
- **Linear Progression**: While players have interaction options, the story follows a set path to maintain narrative consistency

#### Parallel Stories (Tutorial Integration)
1. **Cent & Isaac's Story**
   - **Basic Movement & Combat Tutorial**:
     - Core movement controls while carrying Kriztel
     - Environmental interaction (pushing/pulling objects to block paths)
     - Basic melee combat with improvised weapons (planks, pipes)
     - Quick-time events for cooperative actions
   - **Vehicle Tutorial**:
     - Basic driving mechanics
     - Navigation through chaos
     - Obstacle avoidance
     - Quick decision-making at roadblocks
   - **Resource Management Introduction**:
     - Gathering basic supplies
     - Managing inventory while carrying an injured person
     - Using environmental objects as barriers

2. **Bryan & Johmel's Story**
   - **Advanced Movement & Exploration**:
     - Motorcycle navigation through tight spaces
     - Character switching mechanics
     - Advanced melee combat
   - **Vehicle System Introduction**:
     - Finding abandoned vehicles
     - Basic vehicle inspection mechanics
     - Vehicle condition assessment
     - Introduction to repair concepts
   - **Resource Gathering Enhancement**:
     - Scavenging from vehicles
     - Tool collection for future vehicle repairs
     - Risk/reward decisions while exploring

3. **Clark's Airport Story**
   - **Combat & Stealth Systems**:
     - Introduction to gunplay mechanics
     - Ammo management
     - Stealth movement and detection systems
     - Cover system usage
   - **NPC Interaction System**:
     - Meeting and cooperating with airport security
     - Basic follower mechanics
     - Coordinated combat with AI companion
     - NPC survival state (health, stamina)
   - **Environmental Modification**:
     - Basic construction mechanics using airport materials
     - Reinforcing existing walls and doors
     - Breaking down walls to create new pathways
     - Combining smaller rooms into larger spaces
     - Barricading windows and entrances
     - Repurposing airport furniture and equipment
   - **Airport Survival Mechanics**:
     - Using airport layout for tactical advantage
     - Managing noise levels to avoid hordes
     - Strategic resource gathering in high-security areas
     - Utilizing airport equipment and facilities
     - Finding and collecting construction materials
     - Learning basic structural integrity rules

### Core Gameplay Systems Integration
- **Progressive Skill Introduction**:
  - Each parallel story naturally introduces new mechanics
  - Skills learned in early segments become crucial for later gameplay
  - Systems build upon each other (melee → vehicles → firearms → construction)

- **Future Haven Foundation**:
  - Vehicle mechanics from Bryan/Johmel's story prepares for transportation needs
  - Clark's NPC interaction system lays groundwork for future recruitment
  - Environmental modification skills become basis for haven construction
  - Combat and resource management skills become essential later

- **Resource System Evolution**:
  - Starting with basic supplies (Cent/Isaac)
  - Expanding to vehicle parts (Bryan/Johmel)
  - Advanced resource management and construction materials (Clark)

### Narrative Integration
- **Story Transitions**: Smooth flow between interactive story sequences and core gameplay
- **Tutorial Integration**: Core mechanics naturally introduced through story moments
- **Interactive Elements**:
  - Context-sensitive actions
  - Simple dialogue choices (for character expression, not story branching)
  - Environmental storytelling through player-driven exploration
  - Quick-time events during action sequences

### Act 2: The Haven
- **Main Hub**: The cabin becomes the central safe zone after the convergence of survivors
- **Branching Narratives**:
  - Rescue Mission for Clark (Airport)
  - Search for Cent
- **Base Management**: Players begin developing the haven

### Act 3: Expanding Stories
- Multiple mission paths
- Team composition choices
- Character relationship development
- Haven expansion and community building

## Gameplay Systems

### Combat
- Melee combat system
- Ranged weapons with limited ammunition
- Stealth mechanics for avoiding zombies
- Dynamic difficulty based on noise and attention
- **View-Specific Combat Features**:
  - First-person precision aiming
  - Third-person tactical positioning
  - Seamless combat flow between perspectives

### Resource Management
- Inventory system
- Scavenging mechanics
- Resource allocation for haven development
- Vehicle maintenance and fuel management

### Character System
- **Playable Characters**:
  - Isaac (Initial protagonist)
  - Bryan
  - Johmel
  - Clark (After rescue)
- **Key NPCs**:
  - Kriztel (Isaac's wife)
  - Cent
  - Other survivors

### Haven Management
- Base fortification
- Resource distribution
- Survivor management
- Facility upgrades

## Mission Structure

### Tutorial Missions
1. Escape from the city
2. Vehicle navigation and combat
3. Basic survival mechanics
4. Introduction to resource gathering

### Core Story Missions
1. **Airport Rescue**
   - Objective: Save Clark
   - Team: Johmel/Bryan focused
   - Special mechanics: Airport environment challenges

2. **Search for Cent**
   - Objective: Locate Cent in the urban zone
   - Team: Isaac focused
   - Special mechanics: Urban exploration

### Side Missions
- Resource gathering runs
- Survivor rescue operations
- Territory expansion
- Haven improvement tasks

## Technical Implementation

### Core Systems Priority
1. Basic movement and combat
2. Inventory and resource management
3. Vehicle mechanics
4. Mission system
5. Haven management
6. Character relationship system

### Development Phases

#### Phase 1: Core Mechanics
- Player movement and combat
- Basic zombie AI
- Inventory system
- Vehicle mechanics

#### Phase 2: Story Implementation
- Cutscene system
- Dialog system
- Mission framework
- Character switching

#### Phase 3: Haven Systems
- Base building mechanics
- Resource management
- NPC AI and routines
- Survival systems

#### Phase 4: Content Creation
- Story missions
- Side missions
- Environment design
- Character assets

## Visual Style
- Realistic but stylized graphics
- Dynamic day/night cycle
- Weather system affecting gameplay
- Distinct character designs

## Audio Design
- Dynamic music system
- Environmental audio
- Zombie detection through sound
- Character voice acting

## UI/UX
- Minimal HUD
- Context-sensitive prompts
- Inventory management interface
- Mission tracking system
- Relationship status indicators
- **Interactive Story Elements**:
  - Subtle interaction prompts during story sequences
  - Quick-time event indicators
  - Contextual action highlights
  - Dialogue interface similar to narrative adventure games

## Future Expansion Possibilities
- New playable characters
- Additional story chapters
- More haven facilities
- New zombie types
- Multiplayer cooperation

## Technical Requirements
- Unity Engine
- Save/Load system
- Performance optimization for large environments
- Dynamic loading for open world

## Target Platform
- Initial release: PC
- Potential ports: Consoles (PS5, Xbox Series X|S)

## Development Timeline
1. **Pre-production** (2 months)
   - Story finalization
   - Core mechanics prototyping
   - Art style development

2. **Alpha Phase** (4 months)
   - Core gameplay systems
   - Basic mission structure
   - Initial haven mechanics

3. **Beta Phase** (3 months)
   - Story implementation
   - Character systems
   - Haven management
   - Mission content

4. **Polish Phase** (3 months)
   - Bug fixing
   - Performance optimization
   - Content completion
   - Quality assurance

## Risk Mitigation
- Modular development approach
- Regular playtest sessions
- Scalable content structure
- Clear milestone definitions

## Success Metrics
- Player engagement with story
- Mission completion rates
- Haven development progression
- Character relationship exploration
- Player retention metrics 