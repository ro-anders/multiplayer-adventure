import { RunningGame } from "../domain/RunningGame";

/**
 * This class encapsulates communication with the Lobby Backend.
 * The Lobby never communicates directly with the Game Backend.  Instead, it
 * relies on the Game Backend reporting data to the Lobby Backend.
 */

export default class LobbyBackend {
	constructor(private lobby_url: string) 
    {
        if (!lobby_url) {
            throw new Error("Undefined LOBBY_URL.  Cannot run Game Backend without lobby.")
        }
        this.lobby_url = lobby_url;
    }	 

	/**
	 * Reports the IP of the game server to the lobby backend, or clears
     * the IP if ip is null.
	 */	
	async set_gamesever_ip(ip: string) {
		const headers: Headers = new Headers()
		headers.set('Content-Type', 'application/json')
		headers.set('Accept', 'application/json')
        const method = (!ip ? 'DELETE': 'PUT')
        const body = (!ip ? null : JSON.stringify({
            setting_name: "game_server_ip",
            setting_value: ip
        }));
		const request: RequestInfo = new Request(`${this.lobby_url}/setting/game_server_ip`, {
			method: method,
			headers: headers,
			body: body
		})

		try {
			console.log(`${method} ${request.url}`)
			const response = await fetch(request)
			console.log(`${method} ${response.status}`)
			if (response.status != 200) {
				console.log(`Update lobby's Server IP received ${response.status} response: ${JSON.stringify(await response.json())}`)		
			}
		}
		catch (e) {
			console.log(`Error encountered: ${e}`)
		}
	}

	/**
	 * Requests the details of a game.  Returns the raw data from the lobby without parsing
	 */	
	async get_game_info(session: string): Promise<any> {
		const headers: Headers = new Headers()
		headers.set('Content-Type', 'application/json')
		headers.set('Accept', 'application/json')
		const request: RequestInfo = new Request(`${this.lobby_url}/game/${session}`, {
			method: 'GET',
			headers: headers
		})

		try {
			console.log(`GET ${request.url}`)
			const response = await fetch(request)
			console.log(`GET ${response.status}`)
			if (response.status != 200) {
				console.log(`Get game info received ${response.status} response: ${JSON.stringify(await response.json())}`)		
			}
			const game_info = await response.json()
			return game_info
		}
		catch (e) {
			console.log(`Error encountered: ${e}`)
		}
	}

	/**
	 * Updates the state of the game in the Lobby.  Usually this
	 * is just updating last active time, but also updates the game state
	 * or running and ended.
	 */	
	async update_game(game_info: RunningGame) {
		const headers: Headers = new Headers()
		headers.set('Content-Type', 'application/json')
		headers.set('Accept', 'application/json')
		const request: RequestInfo = new Request(`${this.lobby_url}/game/${game_info.session}`, {
			method: 'PUT',
			headers: headers,
			body: JSON.stringify(game_info)
		})

		try {
			const response = await fetch(request)
			if (response.status != 200) {
				console.log(`Update game ${game_info.session} received ${response.status} response: ${JSON.stringify(await response.json())}`)		
			}
		}
		catch (e) {
			console.log(`Error encountered: ${e}`)
		}
	}

	/**
	 * Reports the IP of the game server to the lobby backend, or clears
     * the IP if ip is null.
	 */	
	async update_player(player_name: string) {
		const headers: Headers = new Headers()
		headers.set('Content-Type', 'application/json')
		headers.set('Accept', 'application/json')
		// A player is only a name and a timestamp (which is always now) so no body needs
		// to be sent.  Just a URL with the playername.
		const request: RequestInfo = new Request(`${this.lobby_url}/player/${player_name}`, {
			method: 'PUT',
			headers: headers
		})

		try {
			console.log(`PUT ${request.url}`)
			const response = await fetch(request)
			console.log(`PUT ${response.status}`)
			if (response.status != 200) {
				console.log(`Update player ${player_name} received ${response.status} response: ${JSON.stringify(await response.json())}`)		
			}
		}
		catch (e) {
			console.log(`Error encountered: ${e}`)
		}
	}


}
