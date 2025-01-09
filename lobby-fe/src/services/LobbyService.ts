import {GameInLobby} from '../domain/GameInLobby'
import { LobbyState } from '../domain/LobbyState'
import GameService from './GameService'

/**
 * Fetched from the backend state of the entire lobby and holds business logic
 * about the lobby.
 */
export default class LobbyService {

	static back_end = process.env.REACT_APP_LOBBY_BE_HOST

	/**
	 * Query for the latest lobby state. 
	 * @param since how far back in history to go for chats
	 * @returns online players, games and recent chats
	 */
	static async getLobbyState(since: number): Promise<LobbyState> {
		const headers: Headers = new Headers()
		headers.set('Content-Type', 'application/json')
		headers.set('Accept', 'application/json')

		// If 'since' was defined, pass it as an argument.  Otherwise let the server
		// decicde what chats are recent enough.
		const param = (since <= 0 ? "" : `?lastactivity=${since}`)

		const request: RequestInfo = new Request(`${LobbyService.back_end}/lobby${param}`, {
			method: 'GET',
			headers: headers
		})

		return fetch(request)
			.then(res => res.json())
			.then(res => {
			return LobbyService.cleanLobby(res)
		})
	}

	/**
	 * Returns whether changes have happened in a lobby
	 * @param lobby1 a previous lobby state
	 * @param lobby2 the latest lobby state
	 * @returns true if no differences exist between the two lobby states.
	 */
	static isLobbyStateEqual(lobby1: LobbyState, lobby2: LobbyState): boolean {
		// Handle player names differ
		if (
			(lobby1.online_player_names.length !== lobby2.online_player_names.length) ||
			(lobby1.online_player_names.filter(x => !lobby2.online_player_names.includes(x)).length > 0))
		{
			console.log(`Change in players detected.  [${lobby1.online_player_names}] != [${lobby2.online_player_names}]`)
			return false;
		}

		function gameSortFunc(game1: GameInLobby, game2: GameInLobby): number {
			return (game1.session - game2.session);
		}

		// Handle games differ
		const games1 = lobby1.games.sort(gameSortFunc)
		const games2 = lobby2.games.sort(gameSortFunc)
		if (JSON.stringify(games1) !== JSON.stringify(games2)) {
			console.log("Change in game detected")
			return false;
		}

		// Handle chats differ
		if (lobby1.recent_chats.length !== lobby2.recent_chats.length) {
			return false;
		}
		return true;
	}

	/**
	 * Examine the lobby state and find old data and clean it up
	 * by making calls the the lobby back end (and by modifying 
	 * the current state object)
	 * @param lobby the state of the lobby
	 * @returns a new, cleaned up state
	 */
	static cleanLobby(lobby: LobbyState) : LobbyState {
		// The backend already filters out players that have gone offline

		// Go through games removing players who are no longer active
		const empty_games: GameInLobby[] = []
		for (const game of lobby.games) {
			const originalNumPlayers = game.player_names.length
			game.player_names = game.player_names.filter((player_name: string) => lobby.online_player_names.indexOf(player_name)>=0)
			if (game.player_names.length === 0) {
				empty_games.push(game)
			}
			if (game.player_names.length !== originalNumPlayers) {
				// We make this call asynchronously.  Don't need to wait for response.
				GameService.updateGame(game)
			}
		}

		// Remove all games with no players
		for (const game of empty_games) {
			// We make this call asynchronously.  Don't need to wait for response.
			GameService.deleteGame(game)
		}
		lobby.games = lobby.games.filter((game: GameInLobby)=>empty_games.indexOf(game)<0)

		// Chats are always recent and don't need to be cleaned.

		return lobby;
	}
}

