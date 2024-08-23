import {Game} from '../domain/Game'

export default class GameServer {

	 static async getGames(filter: string): Promise<Game[]> {
		// We can use the `Headers` constructor to create headers
		// and assign it as the type of the `headers` variable
		const headers: Headers = new Headers()
		// Add a few headers
		headers.set('Content-Type', 'application/json')
		headers.set('Accept', 'application/json')

		const request: RequestInfo = new Request('http://localhost:3000/game', {
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
			console.log("Got response from server: " + res)
			return res
		})
	}
}

