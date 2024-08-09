

export default class PlayerService {

	 static async getGames(): Promise<string[]> {
		// We can use the `Headers` constructor to create headers
		// and assign it as the type of the `headers` variable
		const headers: Headers = new Headers()
		// Add a few headers
		headers.set('Content-Type', 'application/json')
		headers.set('Accept', 'application/json')
		// Add a custom header, which we can use to check
		headers.set('X-Custom-Header', 'CustomValue')

		// Create the request object, which will be a RequestInfo type. 
		// Here, we will pass in the URL as well as the options object as parameters.
		const request: RequestInfo = new Request('http://localhost:3000/api/game', {
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
			console.log("Got response from server: " + res['games'])
			return res['games'] as string[]
		})
	 }	  
}

