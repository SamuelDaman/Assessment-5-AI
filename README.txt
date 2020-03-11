This is a project designed to demonstrate simple AI behaviors using the Unity navmesh and a Finite State Machine.
It is meant to be run in the Unity editor.

Behavior Evaluation

The project consists of two AI controlled tanks that are spawned in at random points and set along random paths.
Each tank's objective is to shoot* the other while avoiding being shot.
The tanks can be in one of three states.
	-Patrol: the tank moves along a random path while looking for the opponent.
	-Seek: the tank takes aim at the opponent, attempting to get them in the center of its line-of-sight.
	-Flee: the tank runs away from the opponent's center of vision to avoid being shot.
	Also, seeking behavior is mixed in with the fleeing behavior in a last-ditch effort to shoot the enemy before being shot.

*For simplicity's sake, "shooting" is defined as one tank aiming directly at the other tank, when there are no obstructions between the two.

Changes from the Original Proposal

The project features two AI controlled characters instead of an AI and a player.
The starting positions and patrol paths are randomized.
When one tank shoots the other, that tank scores, and a new round is started by re-randomizing the positions and paths.
The fleeing behavior also calls on the seeking behavior.
