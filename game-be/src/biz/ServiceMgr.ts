/**
 * The service manager controls the lifecycle of this service.
 * It periodically reports its up status to the lobby backend
 * and if it recognizes it is no longer being used, shuts itself down.
 */

export default class ServiceMgr {

	/** The time period to wait before deciding no one is using the service
	 * and shutting down.
	 */
	//static SHUTDOWN_SERVICE_TIMEOUT = 10 * 60 * 1000 // 10 minutes in milliseconds
	static SHUTDOWN_SERVICE_TIMEOUT = 1 * 60 * 1000 // 10 minutes in milliseconds

	constructor(private lobby_url: string,
				private last_comm_time: number = Date.now(),
				private interval_id: NodeJS.Timeout = null
	) 
	{
		this.report_to_lobby()
		console.log("Creating periodic update")
		interval_id = setInterval(this.periodic_update.bind(this), ServiceMgr.SHUTDOWN_SERVICE_TIMEOUT)
	}	 

	/**
	 * Mark that we just received another websocket message from a running game.
	 * Needs to know this becuase when messages stop coming, this service shuts down.
	 */
	got_game_message() {
		this.last_comm_time = Date.now()
	}

	/**
	 * Posts to the lobby back end that it is still up.
	 */	
	report_to_lobby() {
		// TBD
	}

	/**
	 * This service does two things periodically.
	 * First, it reports that it is up to the lobby.
	 * Second, if it has not received any websocket messages within a given time limit it
	 * decides it is not being used and shuts down.
	 */
	periodic_update() {
		console.log("Running periodic update")
		if (Date.now() - this.last_comm_time > ServiceMgr.SHUTDOWN_SERVICE_TIMEOUT) {
			this.shutdown()
		}
		else {
			this.report_to_lobby()
		}
	}

	/**
	 * No games are currently being played.  Shut down the game backend service.
	 */
	shutdown() {
		console.log("Game Backend shutting down due to inactivity")

		// Report to the lobby that the game service has shutdown
		// TBD

		// Shutdown
		process.exit(0)
	}


  
  
}

