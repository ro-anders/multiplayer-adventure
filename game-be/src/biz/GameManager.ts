/**
 * The game manager tracks the life of games.
 */

import WebSocket from 'ws';

/** The byte code to indicate a message is a connect message */
const CONNECT_CODE = 0x01;

interface Game {
	session: string;	
	clients: WebSocket[];
}

export default class GameManager {
	constructor(private games: {[session: string]: Game;} = {}) 
	{}	 

	process_message(data: any, client_socket: WebSocket) {
		if (typeof(data) === "string") {
			console.log("Unexpected string received from client -> \"" + data + "\"");
		  } else if (data.length < 2) {
			console.log("Unexpected message too short. " + Array.from(data).join(", ") + "")
		  } else {
			console.log("Received " + typeof(data) + " " + Object.prototype.toString.call(data) + " with data [" + Array.from(data).join(" ") + "]")
			// The very first byte should indicate the session
			const session = data[0]
			// This is either a connection request or a message to be broadcast.
			// The second byte is 0x01 for connection requests and 0x00 for messages
			if (data[1] == CONNECT_CODE) {
			  console.log("Request to join session " + session)
			  this.join_session(session, client_socket)        
			}
			else {
			  console.log("Request to broadcast message to " + session)
			  this.broadcast_message(session, data, client_socket);
			}
		  }
	  
	}

	/**
	 * Add a client to  an existing session or create a new session if one doesn't exist.
	 * Then report the join to all other clients of that session.
	 * @param {byte} session - a number indicating the unique key of the game/session
	 * @param {WebSocket} client_socket - a websocket to a new client
	 */
	join_session(session, client_socket: WebSocket) {
		if (!(session in this.games)) {
			this.games[session] = {
				session: session,
				clients: []
			}
		}
		const game = this.games[session]
		game.clients.push(client_socket)
		console.log("client joined session " + session + ".");
	
		// Now send a message to all clients containing the total number of clients 
		// currently in the session.
		const response_data = new Uint8Array([session, CONNECT_CODE, game.clients.length])
		this.broadcast_message(session, response_data, null);
	}
	
	/**
	 * Send the message from one client to every client in the specified session 
	 * Will not send the message to the originator client.
	 * @param {byte} session - a number indicating the unique key of the game/session
	 * @param {byte[]} data - the body of the message
	 * @param {WebSocket} originator - the client that originally sent the message.
	 */
	broadcast_message(session, data, originator) {
		if (session in this.games) {
			const game = this.games[session]
			for (let socket of game.clients) {
				if (socket != originator) {
					socket.send(data); 
				}
			}
		}
	}
  
  
}

