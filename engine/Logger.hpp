#pragma once

/**
 * A bare bones logger class for sending messages either to the console or a log file.
 */
class Logger {
public:

	/** A class solely to indicate the end of a log message. */
	class EndOfLogMessage {};

	/* What to send to the logger with the << operator to indicate the log message is completely
	 * built and should be output to the file or console.
	 */
	static const EndOfLogMessage EOM;

	/** The three possible places log messages can go. */
	static const int OFF;
	static const int CONSOLE;
	static const int FILE;

	/* The two possible levels of log messages. */
	static const int ERROR;
	static const int INFO;

	~Logger();

	/**
	 * This sets up the global logger which can be reached with
	 * Logger::log().
	 */
	static void setup(int destination, int level);

	/**
	 * This gives you a reference to the global logger which you can send
	 * INFO level messages to with the << operator.
	 */
	static Logger& log();

	/**
	* This gives you a reference to the global logger which you can send
	* ERROR level messages to with the << operator.
	*/
	static Logger& logError();

	/**
	 * A convenience method to send a simple INFO level message to the logger. 
	 */
	static void log(const char* message);

	/**
	* A convenience method to send a simple ERROR level message to the logger.
	*/
	static void logError(const char* message);

	/**
	 * Build on the current log message by appending the given string.
	 */
	Logger& operator<<(const char* message);

	/**
	* Build on the current log message by appending the given integer.
	*/
	Logger& operator<<(long number);

	/**
	* Add a newline to the end of the log message and send it to the log.
	*/
	Logger& operator<<(EndOfLogMessage end);

private:
	/** The global instance for logging INFO level messages. */
	static Logger* infoLogger;

	/** The global instance for logging ERROR level messages. */
	static Logger* errorLogger;

	/** Where to send the message. */
	int destination;

	/** String buffer for building log messages. */
	char* buffer;

	/** Current size of buffer */
	int bufferSize;

	/** How many characters are in the buffer. */
	int charsInBuffer;

	/**
	 * Constructor.  Don't call directly.  Use setup().
	 */
	Logger(int destination);

	/** 
	 * Actually send the log message to the destination. 
	 */
	void sendMessage(const char* message);

};