#include "Logger.hpp"

#include <stdio.h>
#include <string.h>

/* Instantiate the singleton instance the end of log message. */
const Logger::EndOfLogMessage Logger::EOM;

/* Set all the static constants */
const int Logger::OFF = 0;
const int Logger::CONSOLE = 1;
const int Logger::FILE =2;
const int Logger::ERROR = 500;
const int Logger::INFO = 400;

Logger::~Logger() {
	delete[] buffer;
}

/**
* This sets up the global logger which can be reached with
* Logger::log().
*/
void Logger::setup(int destination, int level) {
	delete errorLogger;
	errorLogger = new Logger(destination);
	delete infoLogger;
	int infoDestination = (level <= INFO ? destination : OFF);
	infoLogger = new Logger(infoDestination);
}

/**
* This gives you a reference to the global logger which you can send
* INFO level messages to with the << operator.
*/
Logger& Logger::log() {
	return *infoLogger;
}

/**
* This gives you a reference to the global logger which you can send
* ERROR level messages to with the << operator.
*/
Logger& Logger::logError() {
	return *errorLogger;
}

/**
* A convenience method to send a simple INFO level message to the logger.
*/
void Logger::log(const char* message) {
	infoLogger->sendMessage(message);
}

/**
* A convenience method to send a simple ERROR level message to the logger.
*/
void Logger::logError(const char* message) {
	errorLogger->sendMessage(message);
}

/**
* Build on the current log message by appending the given string.
*/
Logger& Logger::operator<<(const char* message) {
	if (destination != OFF) {
		int msgLen = strlen(message);
		// TODO: Don't silently fail.  Expand the buffer.
		if (charsInBuffer + msgLen <= bufferSize) {
			strcpy(buffer + charsInBuffer, message);
			charsInBuffer += msgLen;
		}
	}
	return *this;
}

/**
* Build on the current log message by appending the given integer.
*/
Logger& Logger::operator<<(long number) {
	if (destination != OFF) {
		// TODO: Don't silently fail.  Expand the buffer.
		if (charsInBuffer + 20 /* Max long is 20 digits */ <= bufferSize) {
			int numChars = sprintf(buffer + charsInBuffer, "%ld", number);
			charsInBuffer += numChars;
		}
	}
	return *this;
}

/**
* Add a newline to the end of the log message and send it to the log.
*/
Logger& Logger::operator<<(EndOfLogMessage end) {
	if (destination != OFF) {
		buffer[charsInBuffer] = '\n';
		buffer[charsInBuffer + 1] = '\0';
		sendMessage(buffer);
	}
	charsInBuffer = 0;
	buffer[0] = '\0';
	
	return *this;
}

/** The global instance for logging INFO level messages.  Until setup is called, initialize to do nothing. */
Logger* Logger::infoLogger = new Logger(OFF);

/** The global instance for logging ERROR level messages.  Until setup is called, initialize to do nothing. */
Logger* Logger::errorLogger = new Logger(OFF);

/** String buffer for building log messages. */
char* buffer;

/** Current size of buffer */
int bufferSize;

/** How many characters are in the buffer. */
int charsInBuffer;

/**
* Constructor.  Don't call directly.  Use setup().
*/
Logger::Logger(int inDestination) :
	destination(inDestination),
	buffer(new char[1024]),
	bufferSize(1024),
	charsInBuffer(0)
{
	buffer[0] = '\0';
}

void Logger::sendMessage(const char* message) {
	// TODO: Implement
}