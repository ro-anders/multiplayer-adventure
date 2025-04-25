/**
 * The game manager tracks the life of games.  When games connect to the 
 * Game Backend Server they join a session and then any messages that come
 * in from one game get sent to all games in the session.
 */

import WebSocket from 'ws';
import output from '../Output'
import LobbyBackend from './LobbyBackend';
import Constants from './Constants';
import { RunningGame } from '../domain/RunningGame';

/** The byte code to indicate a message is a connect message */
const GAMEMSG_CODE = 0x00; // Code for when running games are sending game messages back and forth
const CONNECT_CODE = 0x01; // Code for when a client first connects
const READY_CODE = 0x02; // Code to indicate a client is ready to play
const CHAT_CODE = 0x03; // Code for chat messages
const MSG_TO_LOBBY_CODE = 0x04; // Code for player status changes like winning a game
const LATENCY_CHECK_CODE = 0x05; // Code for sending messages for the sole purpose of seeing how long they take to mirror back
/** 
 * All the client connections needed to run a game
 */
interface GamePlatform {
	clients: WebSocket[]
	game_info: RunningGame;
}

/**
 * Produce a string to identify a web socket
 */
const wsToString = (ws: WebSocket) => {
  return (ws as any)._socket?.remoteAddress + ":" + (ws as any)._socket?.remotePort
}


/**
 * This class broadcasts messages to all the players in a game.
 */
export default class GameMgr {

	constructor(private lobby_backend: LobbyBackend, 
		private games: {[session: string]: GamePlatform;} = {}) 
	{
		// Setup a periodic task to report the games' and players' active status to the lobby
		setInterval(this.periodic_lobby_update.bind(this), Constants.PLAYER_PING_PERIOD)
	}	 

	process_message(data: Uint8Array, client_socket: WebSocket) {
		if (data.length < 2) {
			output.error("Unexpected message too short. " + Array.from(data).join(", ") + "")
		} else {
			output.debug("Received message [" + Array.from(data).join(" ") + "]")
			// The very first byte should indicate the session
			const session = data[0]
			// The second byte indicates the type of message
			if (data[1] == GAMEMSG_CODE) {
				output.debug(`Request from ${wsToString(client_socket)} to broadcast message to ${session}`)
				this.broadcast_message(session, data, client_socket);
			}
			else if (data[1] == CONNECT_CODE) {
				this.join_session(session, client_socket)        
			}
			else if (data[1] == READY_CODE) {
				output.log(`Request from ${wsToString(client_socket)} to start game ${session}`)
				this.player_ready(session, client_socket);
			}
			else if (data[1] == CHAT_CODE) {
				output.debug(`Request from ${wsToString(client_socket)} to broadcast chat to session ${session}`);
				this.broadcast_message(session, data, client_socket);
			}
			else if (data[1] == MSG_TO_LOBBY_CODE) {
				this.message_to_lobby(session, data);
			}
			else if (data[1] == LATENCY_CHECK_CODE) {
				// Just send the message right back to the originator
				client_socket.send(data);
			}
			else {
			  output.error(`Unexpected message code ${data[1]} in message ${data} from ${wsToString(client_socket)}`)
			}
		}
	}

	/**
	 * Add a client to  an existing session or create a new session if one doesn't exist.
	 * Collect from the lobby backend the game info.  
	 * Then report the join to all other clients of that session.
	 * @param {byte} session - a number indicating the unique key of the game/session
	 * @param {WebSocket} client_socket - a websocket to a new client
	 */
	async join_session(session, client_socket: WebSocket) {
		if (!(session in this.games)) {
			output.log(`Request from ${wsToString(client_socket)} to join new session ${session}`)
			const game_info = await this.lobby_backend.get_game_info(session)
			// Double check.  Other player could have populated game info while awaiting
			if (!(session in this.games)) {
				output.log(`Retrieved game ${session} info from lobby: ${JSON.stringify(game_info)}`)
				game_info.ready_players = 0;
				this.games[session] = {
					clients: [],
					game_info: game_info
				}
			}
		}
		const game = this.games[session]
		game.clients.push(client_socket)
		game.game_info.joined_players = game.clients.length
		if (game.clients.length === game.game_info.player_names.length) {
			game.game_info.state = 1 /* started */
			output.log(`client ${wsToString(client_socket)} joined session ${session} as final player.  Game ready to start.`);
		} else {
			output.log(`client ${wsToString(client_socket)} joined session ${session}.  ${game.clients.length} of ${game.game_info.player_names.length} joined.`);
		}
	
		// Now serialize the game info and send to all clients
		const jsonData = JSON.stringify(game.game_info);
		const encoder = new TextEncoder();
		const encodedGameInfo = encoder.encode(jsonData);
	  
		const message = new Uint8Array(encodedGameInfo.length+2);
		message[0] = session;
		message[1] = CONNECT_CODE;
		message.set(encodedGameInfo, 2);
		this.broadcast_message(session, message, null);

		// Also notify the lobby
		this.lobby_backend.update_game(game.game_info)
	}

	/**
	 * Mark that a player is ready to start the game.  Once all players
	 * are ready to start broadcast a start message.
	 * @param {byte} session - a number indicating the unique key of the game/session
	 * @param {WebSocket} client_socket - the websocket of the client (we use it to 
	 *                    identify the player)
	 */
	player_ready(session, client_socket: WebSocket) {
		if (!(session in this.games)) {
			output.error(`Request to start in non-existent session ${session}`);
			return;
		}

		const game = this.games[session]
		const client_index = game.clients.indexOf(client_socket);
		if (client_index < 0) {
			output.error(`Request to start session ${session} from unknown client ${wsToString(client_socket)}`)
			return
		}

		// Keep a bitmask of players, setting each players bit to 1 when they are ready
		// to play.  When bitmask reaches 7 (or 3 in a 2 player game)
		// broadcast the start of the game.
		game.game_info.ready_players = game.game_info.ready_players | 2 ** client_index;
		output.log(`client ${wsToString(client_socket)} (#${client_index}) ` +
			`is ready to start session ${session}.` + 
			`  All ready clients = ${game.game_info.ready_players}`);
	
		// If all players have said they are ready, broadcast a start to all clients.
		if (game.game_info.ready_players === (2 ** game.game_info.player_names.length)-1) {
			output.log("Session " + session + " is ready to play.")
			const message = new Uint8Array(2);
			message[0] = session;
			message[1] = READY_CODE;
			this.broadcast_message(session, message, null);
		}
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

	/**
	 * An event has happened in the game that requires the lobby backend being
	 * updated.  Post the change to the lobby back end
	 * @param {byte} session - a number indicating the unique key of the game/session
	 * @param {byte[]} data - the body of the game change message
	 */
	async message_to_lobby(session, data) {
		// First identify the game and player that generated the event.
		if (!(session in this.games)) {
			output.error(`Request to report to server for non-existent session ${session}`)
			return;
		}

		const game = this.games[session]
		// With a game change message, it's a simple array of 4 bytes
		// byte 0 = session
		// byte 1 = GAMECHANGE_CODE
		// byte 2 = player slot (0-2)
		// byte 3 = message code (e.g. 0 == WON_GAME)

		const player_slot = data[2]
		const player_name = game.game_info.player_names[player_slot]
		const player_stats = await this.lobby_backend.get_player_stats(player_name)

		const code = data[3]
		if (code === 0 /* WON_GAME */) {
			// The GAME_WON code requires very different handling from all other codes.
			// If a game is won, we have to update the game that it is finished, then
			// update the winner's stats that they have won and update the losers' stats
			// that they have played a game without winning.
			output.log(`Game ${session} over. ${player_name} has won.  Notifying lobby.`)

			// Update the game is finished
			game.game_info.state = 2 /* finished */;
			this.lobby_backend.update_game(game.game_info)

			// Update the winner's stats
			player_stats.wins += 1;
			player_stats.games += 1;
			this.lobby_backend.update_player_stats(player_stats);

			// Update the loser's stats
			for(var next_player of game.game_info.player_names) {
				if (next_player != player_name) {
					const next_player_stats = await this.lobby_backend.get_player_stats(next_player);
					next_player_stats.games += 1;
					this.lobby_backend.update_player_stats(next_player_stats);
				}
			}
		}
		else {
			// For any other code, it's an achievment along the easter egg path.  
			// Update the achievment in the database if the new achievment is
			// further than the one in the database.
			if (code > player_stats.achvmts) {
				output.log(`${player_name} has achieved easter egg step ${code}.  Notifying lobby.`)
				player_stats.achvmts = code;
				player_stats.achvmt_time = Date.now()
				this.lobby_backend.update_player_stats(player_stats)
			}
		}

	}
  
	/**
	 * This service pings the lobby for every game that is still running and
	 * for every player that is in every running game.
	 */
	periodic_lobby_update() {
		// Run through every game
		for (const nextSession in this.games) {
			const nextGame = this.games[nextSession]
			if (nextGame.game_info.joined_players === nextGame.game_info.player_names.length) {
				this.lobby_backend.update_game(nextGame.game_info)
			}
			const playerNames: string[] = nextGame.game_info.player_names;
			for (const nextPlayer of playerNames) {
				this.lobby_backend.update_player(nextPlayer)
			}
		}
	}

  
}

