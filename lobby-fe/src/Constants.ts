/**
 * This is simply a collector of constants that are used across the Game Backend and the entire system
 */
export default class Constants {
    /** How often the game backend pings the lobby backend to let it know it's still up */
	static GAMEBACKEND_PING_PERIOD = 1 * 60 * 1000 // milliseconds
    // DON'T CHANGE THIS WITHOUT CHANGING GAME BACKEND CONSTANT!!!

    /** How long to wait for the game backend to start up before giving up and
     * rerequesting a new one */
    static GAMEBACKEND_WORSTCASE_STARTUP_TIME = 5 * 60 * 1000 // milliseconds
}