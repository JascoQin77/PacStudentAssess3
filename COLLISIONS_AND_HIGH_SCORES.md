# Assessment 4 – Section 24.5

Open `StartScene`, press Play, then choose Level 1. WASD controls PacStudent.

## Implemented

- Kinematic Rigidbody2D components register triggers only. Movement and collision rollback use Transform/Lerp; no forces or Rigidbody collision response.
- Walls block travel and emit a single bump particle/audio response per blocked attempt.
- Two side tunnel triggers teleport to the opposite entrance and continue moving inward.
- Pellets: +10; power pills: +50; bonus cherries: +100; edible ghosts: +300. Collection updates the HUD and removes the item once.
- Power pills last ten seconds, with Recovering during the final three seconds. Dead ghosts retain their own three-second respawn timer and return to the state matching the remaining power time.
- Normal ghost contact removes one life, freezes ghosts, synchronizes death animation/audio, emits particles, and resets PacStudent/ghosts. PacStudent waits for new input after respawning.
- The round shows 3, 2, 1, GO! for one second each. A blocking overlay covers the level, HUD and ghost labels. Movement, collisions and timer remain frozen until GO! disappears. Intro/normal/scared/dead music follows game state.
- Game time starts at 00:00:00, formatted as minutes:seconds:hundredths.
- Eating every pellet (including power pills), or losing all three lives, shows Game Over, freezes movement/time, disables buttons, saves eligible records and returns to StartScene after three seconds.
- PlayerPrefs keys: `Level1HighScore`, `Level1BestTime`. A higher score replaces the record; an equal score replaces it only when its time is lower. StartScene displays the stored score and matching time.

Ghost pathfinding/movement belongs to the later ghost-AI assessment section. The four existing ghosts remain in their initial pen, with working collision and animation states.

## Scene generation and verification

- `Tools > PacStudent > Build Assessment 4 Collisions and Game Flow` rebuilds the collision/game-flow scene bindings. Run this after rebuilding the earlier HUD or movement setup.
- `BuildAssessment4Collisions.Validate` checks saved scene references, colliders, kinematic bodies, audio, particles, overlays and teleporters.
- `Assessment4CollisionsVerification.Run` is a batch editor integration check. It loads StartScene and the real gameplay scene, exercises physics triggers, timers, state transitions, wall feedback, death/reset, both game-over conditions and PlayerPrefs tie rules. It restores existing score/time values after testing.
- Verification output: `Logs/assessment4-collisions-verification.txt`. Graphic runs additionally save countdown, gameplay and Game Over previews to `Logs`.

The Unity 6000.4.11f1 scene validation and Play Mode integration checks passed on 17 October 2026.
