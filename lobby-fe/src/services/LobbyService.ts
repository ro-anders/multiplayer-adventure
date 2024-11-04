import {GameInLobby} from '../domain/GameInLobby'
import { LobbyState } from '../domain/LobbyState'

/**
 * Fetched from the backend state of the entire lobby and holds business logic
 * about the lobby.
 */
export default class LobbyService {

	static back_end = process.env.REACT_APP_LOBBY_BE_HOST

	static async getLobbyState(): Promise<LobbyState> {
		const headers: Headers = new Headers()
		headers.set('Content-Type', 'application/json')
		headers.set('Accept', 'application/json')

		const request: RequestInfo = new Request(`${LobbyService.back_end}/lobby`, {
			method: 'GET',
			headers: headers
		})

		return fetch(request)
			.then(res => res.json())
			.then(res => {
			return res
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
		const fx = lobby1.online_player_names.filter(x => !lobby2.online_player_names.includes(x))
		if (
			(lobby1.online_player_names.length != lobby2.online_player_names.length) ||
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
		if (JSON.stringify(games1) != JSON.stringify(games2)) {
			console.log("Change in game detected")
			return false;
		}
		return true;
	}
}

