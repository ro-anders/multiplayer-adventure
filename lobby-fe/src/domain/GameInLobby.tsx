export interface GameDetails {
    session: number;
    game_number: number;
    number_players: number; /* Should be 2, 2.5 or 3 */
    fast_dragons?: boolean;
    fearful_dragons?: boolean;
    player_names: string[];
}

export interface GameInLobby extends GameDetails {
    state: number; /* 0 = proposed, 1 = started, 2 = finished */
    display_names: string[]; /* player_names in the order they were added */
    order: number; /* which ordering used to reorder display_names into player_names */
}

/**
 * Take a list of players in the order they joined and an ordering
 * and return a list of players in the order they will appear in the game.
 */
export function reorder(display_names: string[], order: number) : string[] {
    if (display_names.length === 3) {
        switch (order) {
            case 0:
                return [display_names[0], display_names[1], display_names[2]];
            case 1:
                return [display_names[0], display_names[2], display_names[1]];
            case 2:
                return [display_names[1], display_names[0], display_names[2]];
            case 3:
                return [display_names[1], display_names[2], display_names[0]];
            case 4:
                return [display_names[2], display_names[0], display_names[1]];
            case 5: default:
                return [display_names[2], display_names[1], display_names[0]];
        }
    } else if (display_names.length === 2) {
        if (order % 2 === 0) {
           return [display_names[0], display_names[1]];
        } else {
            return [display_names[1], display_names[0]];
        }
    } else {
        return display_names.slice();
    }
}

export const GAMESTATE__PROPOSED = 0;
export const GAMESTATE_RUNNING = 1;
export const GAMESTATE_ENDED = 2;

