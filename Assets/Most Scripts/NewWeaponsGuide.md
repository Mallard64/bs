# New Multi-Mode Weapons Guide

This document describes the 7 new multi-mode weapons added to the game, inspired by Brawl Stars and Smash Bros mechanics.

## How to Use

All new weapons have 3 different modes that can be switched using the mode swap button. Each mode has different properties, damage, cooldown, and special effects.

## Weapon List

### 4. Elemental Staff (Fire/Ice/Lightning)
**Inspired by**: Brawl Stars elemental brawlers
- **Fire Mode (0)**: Fast-firing burning projectiles
  - Damage: 15 + burn effect
  - Cooldown: 0.4s
  - Speed: 1.2x normal
- **Ice Mode (1)**: Slowing projectiles with area effect
  - Damage: 20 + slow effect
  - Cooldown: 0.8s
  - Speed: 0.9x normal
- **Lightning Mode (2)**: High-damage chain lightning
  - Damage: 35 + chain effect
  - Cooldown: 1.5s
  - Speed: 1.5x normal

### 5. Morph Cannon (Rocket/Beam/Grenade)
**Inspired by**: Smash Bros item variety
- **Rocket Mode (0)**: Fast seeking projectile
  - Damage: 40
  - Cooldown: 1.0s
  - Speed: 1.3x normal
- **Beam Mode (1)**: Continuous laser beam (8 segments)
  - Damage: 8 per segment (64 total)
  - Cooldown: 2.0s
  - Duration: 0.3s
- **Grenade Mode (2)**: Bouncing explosive projectile
  - Damage: 45 + area effect
  - Cooldown: 1.8s
  - Speed: 0.7x normal

### 6. Spirit Bow (Piercing/Explosive/Homing)
**Inspired by**: Brawl Stars archer mechanics
- **Piercing Mode (0)**: Arrow goes through enemies
  - Damage: 25 per target
  - Cooldown: 0.7s
  - Speed: 1.4x normal
- **Explosive Mode (1)**: Arrow explodes on impact
  - Damage: 35 + area effect
  - Cooldown: 1.2s
  - Speed: Normal
- **Homing Mode (2)**: Arrow seeks nearest enemy
  - Damage: 30
  - Cooldown: 1.5s
  - Speed: 0.9x normal
  - Lifetime: 2x normal

### 7. War Hammer (Slam/Throw/Spin)
**Inspired by**: Smash Bros heavyweight attacks
- **Slam Mode (0)**: Close-range shockwave (5 hits)
  - Damage: 30 per hit
  - Cooldown: 0.8s
  - Range: Close (fan pattern)
  - Ammo: Unlimited
- **Throw Mode (1)**: Boomerang-style throw
  - Damage: 40
  - Cooldown: 2.2s
  - Speed: 1.1x normal
  - Spin: 540°/s
  - Ammo: Limited
- **Spin Mode (2)**: 360-degree attack (12 hitboxes)
  - Damage: 20 per hit
  - Cooldown: 1.5s
  - Range: 0.8 units radius
  - Ammo: Unlimited

### 8. Plasma Rifle (Burst/Charge/Overload)
**Inspired by**: Sci-fi weapon variety
- **Burst Mode (0)**: 3-shot burst with 0.1s delay
  - Damage: 15 per shot (45 total)
  - Cooldown: 0.9s
  - Speed: Normal
- **Charge Mode (1)**: Single powerful shot
  - Damage: 60
  - Cooldown: 2.5s
  - Speed: 0.8x normal
- **Overload Mode (2)**: 9-shot spread (±40°)
  - Damage: 18 per shot
  - Cooldown: 1.8s
  - Speed: 1.2x normal

### 9. Ninja Kunai (Shadow/Poison/Teleport)
**Inspired by**: Stealth and mobility mechanics
- **Shadow Mode (0)**: Triple kunai at different angles
  - Damage: 20 per kunai
  - Cooldown: 0.5s
  - Speed: 1.3x normal
  - Count: 3 (±15° spread)
- **Poison Mode (1)**: Single poisoning kunai
  - Damage: 15 + poison DOT
  - Cooldown: 0.8s
  - Speed: Normal
- **Teleport Mode (2)**: Instant hit at target location
  - Damage: 35
  - Cooldown: 2.0s
  - Range: 4 units (instant)

### 10. Chaos Orb (Random/Portal/Gravity)
**Inspired by**: Unpredictable magical effects
- **Random Mode (0)**: Unpredictable bouncing orb
  - Damage: 20-45 (random)
  - Cooldown: 1.0s
  - Speed: 0.8x-1.4x (random)
  - Behavior: Random direction deviation ±30°
- **Portal Mode (1)**: Creates temporary portal
  - Damage: 25
  - Cooldown: 3.0s
  - Duration: 2 seconds
  - Range: 3 units from player
- **Gravity Mode (2)**: Pulls enemies toward center
  - Damage: 30
  - Cooldown: 2.5s
  - Duration: 3 seconds
  - Effect: Gravity pull

## Technical Implementation

### Weapon IDs
- 0: Sniper
- 1: Shotgun  
- 2: Sword/Knife
- 3: AR
- 4: Elemental Staff
- 5: Morph Cannon
- 6: Spirit Bow
- 7: War Hammer
- 8: Plasma Rifle
- 9: Ninja Kunai
- 10: Chaos Orb

### Mode Switching
All new weapons use `swapModeNum` (0, 1, 2) to determine their current mode. The mode affects:
- Damage output
- Cooldown times
- Projectile behavior
- Ammo consumption
- Visual effects

### Special Mechanics
- **Unlimited Ammo Modes**: War Hammer slam/spin modes, Sword stab/slice modes
- **Multi-Hit Attacks**: Morph Cannon beam, War Hammer slam/spin, Ninja Kunai shadow
- **Delayed Attacks**: Plasma Rifle burst mode
- **Instant Attacks**: Ninja Kunai teleport mode
- **Persistent Effects**: Chaos Orb portal/gravity modes

## Balancing Notes

Each weapon is designed with different playstyles in mind:
- **Elemental Staff**: Versatile magic DPS with elemental effects
- **Morph Cannon**: Heavy weapons specialist with high burst damage
- **Spirit Bow**: Precision archer with utility arrows
- **War Hammer**: Melee powerhouse with close-range dominance
- **Plasma Rifle**: High-tech weapon with burst potential
- **Ninja Kunai**: Agile assassin weapon with mobility
- **Chaos Orb**: Unpredictable support weapon with area control

All weapons maintain the server-authoritative architecture and are fully networked for multiplayer compatibility.