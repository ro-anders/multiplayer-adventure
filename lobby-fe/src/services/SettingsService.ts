import {GameInLobby} from '../domain/GameInLobby'

export default class SettingsService {

	static back_end = process.env.REACT_APP_LOBBY_BE_HOST

	static GAMESERVER_START_POLL_TIME = 5000; // Milliseconds

	/**
	 * Query for the GameServer's IP.  If the GameServer is not running, it will
	 * request the GameServer start up and poll until it gets an IP, but it will
	 * not return until it has a valid IP or hits an error.
	 * @returns the IP of the GameServer
	 */
	static async getGameServerIP(): Promise<String> {
		var ip: string = ''
		var firstRequest = true
		while (ip == '') {
			// We can use the `Headers` constructor to create headers
			// and assign it as the type of the `headers` variable
			const headers: Headers = new Headers()
			// Add a few headers
			headers.set('Content-Type', 'application/json')
			headers.set('Accept', 'application/json')

			const request: RequestInfo = new Request(`${SettingsService.back_end}/setting/game_server_ip`, {
				method: 'GET',
				headers: headers
			})

			const response = await fetch(request)
			const jsonResp = await response.json()
			if (jsonResp && jsonResp['value']) {
				ip = jsonResp['value']
			}
			else {
				if (firstRequest) {
					SettingsService.spawnGameServer()
				}
				await new Promise((resolve) => setTimeout(resolve, SettingsService.GAMESERVER_START_POLL_TIME));
			}
			firstRequest = false
		}
		return ip;
	}

	/**
	 * Request the backend spawn a new game server.
	 */
	static async spawnGameServer() {
		const headers: Headers = new Headers()
		headers.set('Content-Type', 'application/json')
		headers.set('Accept', 'application/json')

		const request: RequestInfo = new Request(`${SettingsService.back_end}/newgameserver`, {
			method: 'POST',
			headers: headers
		})

		await fetch(request)
	}

}

