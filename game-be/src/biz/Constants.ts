/**
 * This is simply a collector of constants that are used across the Game Backend and the entire system
 */
export default class Constants {

	/** The time period to wait before deciding no one is using the service
	 * and shutting down. */
	static SHUTDOWN_SERVICE_TIMEOUT = 10 * 60 * 1000 // milliseconds

	/** How often the game backend pings the lobby backend to let it know it's still up */
	static GAMEBACKEND_PING_PERIOD = 1 * 60 * 1000 // milliseconds
    // DON'T CHANGE THIS WITHOUT CHANGING LOBBY FRONTEND AND BACKEND CONSTANTS!!!

    /** How often the lobby and game backend ping the lobby backend to let it know a player
     * is still active. */
    static PLAYER_PING_PERIOD = 1 * 60 * 1000 // milliseconds
    // DON'T CHANGE THIS WITHOUT CHANGING LOBBY FRONTEND CONSTANT!!!	
}