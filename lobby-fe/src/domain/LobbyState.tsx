import { Game } from "./Game";

/**
 * Represents the state of the lobby (e.g. what players
 * are on, what games are running, and what messages have been chatted)
 */
export interface LobbyState {
    online_player_names: string[];
    games: Game[];
}
