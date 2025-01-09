import { Chat } from "../domain/LobbyState"

/**
 * A service for CRUD operations on Chat messages, but since chat messages are immutable and
 * the lobby service returns them with other stuff, the only CRUD operation needed is CREATE
 */

export default class ChatService {

	 static back_end = process.env.REACT_APP_LOBBY_BE_HOST


	 static async postChat(current_player: string, message: string): Promise<void> {
		const headers: Headers = new Headers()
		// Add a few headers
		headers.set('Content-Type', 'application/json')
		headers.set('Accept', 'application/json')

		// A player is only a name and a timestamp (which is always now) so no body needs
		// to be sent.  Just a URL with the playername.
		const newChat: Chat = {
			player_name: current_player,
			message: message
		};
		const request: RequestInfo = new Request(`${ChatService.back_end}/newchat`, {
			method: 'POST',
			headers: headers,
			body: JSON.stringify(newChat)
		})

		fetch(request)
			.then(res => {
				// Don't need to do anything
			})
	 }	  


}

