import Constants from '../Constants'

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
		var just_spawned = false
		var first_time = true // We log some things on the first try, but not subsequent tries
		while (ip === '') {
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
			const response_ip = (jsonResp ? jsonResp['setting_value'] : '')
			const response_timestamp = (jsonResp ? jsonResp['time_set']: 0)
			// Figure out from the response if we have an ip and, if not, if we need to spawn the server
			const too_old_period = (response_ip === "starting" ? Constants.GAMEBACKEND_WORSTCASE_STARTUP_TIME : 2*Constants.GAMEBACKEND_PING_PERIOD)
			const too_old = Date.now()-response_timestamp >= too_old_period
			// We have an ip if the ip is not "starting" and isn't too old 
			ip = (response_ip && (response_ip !== "starting") && !too_old ? response_ip : '')

			// We spawn the server if we have no setting or if the setting is too old, but don't
			// spawn if we just recently tried to spawn.
			if ((!response_ip || too_old)) {
				if (!just_spawned) {
					// TEMP SettingsService.spawnGameServer()
					just_spawned = true
				}
			} else {
				just_spawned = false
			}

			// The first time through, log what's happening.
			if (first_time) {
				if (too_old) {
					console.log(`Found game server ip, but server appears to have aborted abnormally.  Respawning.`)
				} else if (just_spawned) {
					console.log(`No game server running.  Spawning new game server.`)
				} else if (response_ip === "starting") {
					console.log(`Game server is in the process of starting up.`)
				} else {
					console.log("Game server in unknown state.")
				}
				first_time = false;
			}

			// If we don't yet have an ip, wait.
			if (!ip) {
				await new Promise((resolve) => setTimeout(resolve, SettingsService.GAMESERVER_START_POLL_TIME));
			}

		}
		console.log(`Game Server running at ${ip}.  Proceeding.`)
		return ip;
	}

	/**
	 * Request the backend spawn a new game server.
	 */
	private static async spawnGameServer() {
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

