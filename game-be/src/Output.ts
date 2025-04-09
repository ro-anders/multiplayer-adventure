/**
 * This is simply a collector of constants that are used across the Game Backend and the entire system
 */
export default class output {

    static LOG_SILENT = "SILENT"
    static LOG_ERROR = "ERROR"
    static LOG_WARN = "WARN"
    static LOG_INFO = "INFO"
    static LOG_DEBUG = "DEBUG"
    static LOG_LEVELS: {[name: string]: number} = {
        "SILENT": 0,
        "ERROR": 1,
        "WARN": 2,
        "INFO": 3,
        "DEBUG": 4
    }
    static DEFAULT_LEVEL = output.LOG_LEVELS[output.LOG_INFO];

    /**
     * Whether a message output at a given log level should actually appear
     * @param level the level a message was output at
     * @returns true if the message should appear, false if not
     */
    static should_output(level: string): boolean {
        const env_log_level = process.env.LOG_LEVEL || ""
        const maximum_to_output = 
            (env_log_level in output.LOG_LEVELS ? output.LOG_LEVELS[env_log_level] : output.DEFAULT_LEVEL)
        return output.LOG_LEVELS[level] <= maximum_to_output    
    }

	/** Output a message if the logging level is set to ERROR or higher */
	static error(message) {
        if (output.should_output(output.LOG_ERROR)) {
            console.error(message)
        }
    }

	/** Output a message if the logging level is set to WARN or higher */
	static warn(message) {
        if (output.should_output(output.LOG_WARN)) {
            console.warn(message)
        }
    }

	/** Output a message if the logging level is set to INFO or higher */
	static log(message) {
        if (output.should_output(output.LOG_INFO)) {
            console.log(message)
        }
    }

    /** Output a message if the logging level is set to DEBUG or higher */
	static debug(message) {
        if (output.should_output(output.LOG_DEBUG)) {
            console.debug(message)
        }
    }

}