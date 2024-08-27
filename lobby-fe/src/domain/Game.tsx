export interface Game {
    session?: number,
    game_number: number;
    number_players: number; /* Should be 2, 2.5 or 3 */
    fast_dragons?: boolean;
    fearful_dragons?: boolean;
    player1_name?: string;
    player2_name?: string;
    player3_name?: string;
}
