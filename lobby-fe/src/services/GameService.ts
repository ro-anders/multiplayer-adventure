import {GameInLobby} from '../domain/GameInLobby'

/**
 * A class for performing CRUD operations on the Game database table.
 */
export default class GameService {

	static back_end = process.env.REACT_APP_LOBBY_BE_HOST

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
	 * Create a new proposed game that is visible to everyone.
	 * @param game_setup the details of the game
	 * @returns a list of all currently proposed games, including this one.
	 */
	static async proposeNewGame(game_setup: GameInLobby): Promise<GameInLobby[]> {
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
		return await this.getGames()
	}

	/**
	 * Update an existing game with new information.
	 * @param game the details of the game
	 * @returns a list of all currently proposed games, including this one.
	 */
	static async updateGame(game: GameInLobby) {
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
	}
}

