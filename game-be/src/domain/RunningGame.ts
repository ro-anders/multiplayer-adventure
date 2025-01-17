
/**
 * A struture containing the characteristcs of the game.
 * These don't change once the game is agreed on.
 */
export interface GameDetails {
	session: number;
	game_number: number;
	number_players: number; /* Should be 2 or 3 in the Game Backend */
	fast_dragons?: boolean;
	fearful_dragons?: boolean;
	player_names: string[];
}/**
 * Add on to the GameDetails structure state needed to start up and run the game.
 */
export interface RunningGame extends GameDetails {
	state: number; /* 0 = proposed, 1 = started, 2 = finished */
	display_names: string[]; /* Same as player_names but in a different order */
	order: number; /* Ordering of display_names. */
	joined_players: number; /* the number of players who have joined the game */
	ready_players: number; /* NOT A COUNT!  A bitmask of which players are ready */
}

