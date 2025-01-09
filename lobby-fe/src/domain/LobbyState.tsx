import { GameInLobby } from "./GameInLobby";


export interface Chat {
    player_name: string;
    message: string;
}

export interface ReceivedChat extends Chat {
    timestamp: number; // UTC timestamp of when message was sent as recorded by server
}

/**
 * Represents the state of the lobby (e.g. what players
 * are on, what games are running, and what messages have been chatted)
 */
export interface LobbyState {
    online_player_names: string[];
    games: GameInLobby[];
    recent_chats: ReceivedChat[]; // Only chats that have been posted since the last time the lobby state was polled
}
