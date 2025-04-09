import output from "../Output"
import { PlayerStats } from "../domain/PlayerStats";
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
			const response = await fetch(request)
			if (response.status != 200) {
				output.error(`Update lobby's Server IP received ${response.status} response: ${JSON.stringify(await response.json())}`)		
			}
		}
		catch (e) {
			output.error(`Error encountered: ${e}`)
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
			const response = await fetch(request)
			if (response.status != 200) {
				output.error(`Get game info received ${response.status} response: ${JSON.stringify(await response.json())}`)		
				throw new Error("GET /game failed.")
			}
			const game_info = await response.json()
			return game_info
		}
		catch (e) {
			output.log(`Error encountered: ${e}`)
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
				output.log(`Update game ${game_info.session} received ${response.status} response: ${JSON.stringify(await response.json())}`)		
			}
		}
		catch (e) {
			output.log(`Error encountered: ${e}`)
		}
	}

	/**
	 * Updates the lobby backend that a player is still active.
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
			const response = await fetch(request)
			if (response.status != 200) {
				output.log(`Update player ${player_name} received ${response.status} response: ${JSON.stringify(await response.json())}`)		
			}
		}
		catch (e) {
			output.log(`Error encountered: ${e}`)
		}
	}

	/**
	 * Requests the stats of a player.  
	 * Note, if the player is not found, that is not an error.  Will just return blank stats.
	 */	
	async get_player_stats(playername: string): Promise<PlayerStats> {
		const headers: Headers = new Headers()
		headers.set('Content-Type', 'application/json')
		headers.set('Accept', 'application/json')
		const request: RequestInfo = new Request(`${this.lobby_url}/playerstats/${playername}`, {
			method: 'GET',
			headers: headers
		})

		try {
			const response = await fetch(request)
			var player_stats;
			if (response.status == 200) {
				player_stats = await response.json()
			} else if (response.status == 404) {
				player_stats = {
					playername: playername,
					games: 0,
					wins: 0,
					achvmts: 0,
					achvmt_time: 0
				}
			} else {
				output.log(`Get player stats ${request.url} received ${response.status} response: ${JSON.stringify(await response.json())}`)		
				throw new Error("GET /playerstats failed")
			}
			return player_stats
		}
		catch (e) {
			output.log(`Error encountered: ${e}`)
		}
	}

	/**
	 * Updates the stats of the player in the Lobby.  
	 */	
	async update_player_stats(player_stats: PlayerStats) {
		const headers: Headers = new Headers()
		headers.set('Content-Type', 'application/json')
		headers.set('Accept', 'application/json')
		const request: RequestInfo = new Request(`${this.lobby_url}/playerstats/${player_stats.playername}`, {
			method: 'PUT',
			headers: headers,
			body: JSON.stringify(player_stats)
		})

		try {
			const response = await fetch(request)
			if (response.status != 200) {
				output.log(`Update player stats ${request.url} received ${response.status} response: ${JSON.stringify(await response.json())}`)		
			}
		}
		catch (e) {
			output.log(`Error encountered: ${e}`)
		}
	}

}
