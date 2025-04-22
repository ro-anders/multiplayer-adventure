import {GameInLobby} from '../domain/GameInLobby'

/**
 * A class for performing CRUD operations on the Game database table.
 */
export default class GameService {

	static back_end = process.env.REACT_APP_LOBBY_BE_HOST

	/**
	 * Get a list of all recent games
	 * @returns list of games both proposed and started
	 */
	static async getGames(): Promise<GameInLobby[]> {
		// We can use the `Headers` constructor to create headers
		// and assign it as the type of the `headers` variable
		const headers: Headers = new Headers()
		// Add a few headers
		headers.set('Content-Type', 'application/json')
		headers.set('Accept', 'application/json')

		const request: RequestInfo = new Request(`${GameService.back_end}/game`, {
			method: 'GET',
			headers: headers
		})

		// For our example, the data is stored on a static `users.json` file
		return fetch(request)
			// the JSON body is taken from the response
			.then(res => res.json())
			.then(res => {
			// The response has an `any` type, so we need to cast
			// it to the `User` type, and return it from the promise
			return res
		})
	}

	/**
	 * Get details of a specific game
	 * @param session the session number of the desired game
	 * @returns details of game or null if not found
	 */
	static async getGame(session: number): Promise<GameInLobby | null> {
		const headers: Headers = new Headers()
		headers.set('Content-Type', 'application/json')
		headers.set('Accept', 'application/json')

		const request: RequestInfo = new Request(`${GameService.back_end}/game/${session}`, {
			method: 'GET',
			headers: headers
		})
		const response = await fetch(request)
		if (response.status == 400) {
			return null
		}
		return await response.json()
	}

	/**
	 * Create a new proposed game that is visible to everyone.
	 * @param game_setup the details of the game
	 * @returns supposed to return false if a concurrent action by another player
	 *   blocks this, but that can never happen with a new game, so always returns true
	 */
	static async proposeNewGame(game_setup: GameInLobby): Promise<boolean> {
		const headers: Headers = new Headers()
		headers.set('Content-Type', 'application/json')
		headers.set('Accept', 'application/json')

		// Create the request object, which will be a RequestInfo type. 
		// Here, we will pass in the URL as well as the options object as parameters.
		const request: RequestInfo = new Request(`${GameService.back_end}/newgame`, {
			method: 'POST',
			headers: headers,
			body: JSON.stringify(game_setup)
		})

		await fetch(request);
		return true
	}

	/**
	 * Update an existing game with new information.  If another user has modified the game
	 * since our last sync, don't update the game and return failure.
	 * @param game the details of the game
	 * @param oldGame the details of the game before we modified it
	 * @returns false if the server's latest version of the game is different from oldGame,
	 *   otherwise true
	 */
	static async updateGame(game: GameInLobby, oldGame: GameInLobby | null): Promise<boolean> {
		// Before sending an update to the server, make sure the game has not changed underneath us
		if (oldGame != null) {
			const server_version = await this.getGame(game.session)
			// The only thing that changes once a game is created is the player list and the number of players
			// so check that those haven't changed.
			if (server_version != null) {
				if ((server_version.number_players != oldGame.number_players) ||
					(server_version.display_names.length != oldGame.player_names.length) ||
					(!server_version.display_names.every((val, index) => val === oldGame.display_names[index]))) {
					
					console.warn(`Detected concurrent change in game ${game.session}.  Aborting update.`)
					return false
				}
			}
		}

		const headers: Headers = new Headers()
		headers.set('Content-Type', 'application/json')
		headers.set('Accept', 'application/json')

		// Create the request object, which will be a RequestInfo type. 
		// Here, we will pass in the URL as well as the options object as parameters.
		const request: RequestInfo = new Request(`${GameService.back_end}/game/${game.session}`, {
			method: 'PUT',
			headers: headers,
			body: JSON.stringify(game)
		})

		await fetch(request);
		return true
	}

	/**
	 * Update an existing game with new information.
	 * @param game the details of the game
	 * @returns a list of all currently proposed games, including this one.
	 */
	static async deleteGame(game: GameInLobby) {
		const headers: Headers = new Headers()
		headers.set('Content-Type', 'application/json')
		headers.set('Accept', 'application/json')

		// Create the request object, which will be a RequestInfo type. 
		// Here, we will pass in the URL as well as the options object as parameters.
		const request: RequestInfo = new Request(`${GameService.back_end}/game/${game.session}`, {
			method: 'DELETE',
			headers: headers
		})

		await fetch(request);
	}

}

