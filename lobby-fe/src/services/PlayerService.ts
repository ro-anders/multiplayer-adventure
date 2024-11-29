

export default class PlayerService {

	 static back_end = process.env.REACT_APP_LOBBY_BE_HOST

	 static async getOnlinePlayers(): Promise<string[]> {
		// We can use the `Headers` constructor to create headers
		// and assign it as the type of the `headers` variable
		const headers: Headers = new Headers()
		// Add a few headers
		headers.set('Content-Type', 'application/json')
		headers.set('Accept', 'application/json')

		// Create the request object, which will be a RequestInfo type. 
		// Here, we will pass in the URL as well as the options object as parameters.
		const request: RequestInfo = new Request(`${PlayerService.back_end}/player/`, {
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
				const player_names: string[] = res.map((player: any) => player['playername'])
				return player_names
			})
	 }	  

	 static async registerPlayer(username: string): Promise<void> {
		// We can use the `Headers` constructor to create headers
		// and assign it as the type of the `headers` variable
		const headers: Headers = new Headers()
		// Add a few headers
		headers.set('Content-Type', 'application/json')
		headers.set('Accept', 'application/json')

		// A player is only a name and a timestamp (which is always now) so no body needs
		// to be sent.  Just a URL with the playername.
		const request: RequestInfo = new Request(`${PlayerService.back_end}/player/${username}`, {
			method: 'PUT',
			headers: headers
		})

		fetch(request)
			.then(res => {
				// Don't need to do anything
			})
	 }	  


}

