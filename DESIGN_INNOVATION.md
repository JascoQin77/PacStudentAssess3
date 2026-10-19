# Design Innovation

## InnovationScene: Pulse Shift

`InnovationScene` is a second playable scene based on the PacStudent maze. It adds a deliberate defensive ability instead of changing the maze into a cosmetic variation.

Press `Space` during play to activate **Pulse Shift**. The pulse freezes every active ghost for 2.2 seconds, while a cyan ring expands from PacStudent to make the effect readable in the maze. The ability then recharges for 7.5 seconds. Dead ghosts are not affected, and the normal countdown, death reset, scoring, pellet collection, and win conditions remain intact.

The mechanic changes the player experience in three ways:

- **Route planning:** the player can hold the pulse for a dangerous intersection instead of spending it immediately.
- **Risk and timing:** using the ability creates a clear cooldown window, so the player must decide whether to take a shortcut or wait for protection.
- **Readable feedback:** the top-left Pulse Shift panel shows `READY`, the live freeze duration, or the remaining recharge time; the expanding ring communicates the active range and duration in the play space.

The main menu `Level 2` button loads `InnovationScene`. The scene is enabled in `ProjectSettings/EditorBuildSettings.asset`, and the scene can also be rebuilt with **Tools > PacStudent > Build Innovation Scene**.
