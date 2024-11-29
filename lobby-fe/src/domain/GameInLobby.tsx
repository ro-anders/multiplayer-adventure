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
}
export const GAMESTATE__PROPOSED = 0;
export const GAMESTATE_RUNNING = 1;
export const GAMESTATE_ENDED = 2;

