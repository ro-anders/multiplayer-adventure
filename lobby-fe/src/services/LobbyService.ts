import {GameInLobby, reorder} from '../domain/GameInLobby'
import { LobbyState, LobbyStateToString } from '../domain/LobbyState'
import GameService from './GameService'

/**
 * Fetches from the backend state of the entire lobby but imposes a 
 * rate limit and requests for lobby state made more often than the rate
 * limit return no changes.  The rate limit starts out high, but decreases during
 * long periods of no changes.
 * Also imposes synchronization when the local client makes changes.  It blocks returning
 * server-side changes until the local changes have been registered in the server.
 */
export default class LobbyService {

	static back_end = process.env.REACT_APP_LOBBY_BE_HOST

	/** The lobbby service keeps around what the last state retrieved from the server
	 * was, so that it can tell if there have been server-side changes.
	 */
	static last_state: LobbyState = {online_player_names: [], games: [], recent_chats: []}

	// When changes come in, poll every second for new changes, but back off as 
	// no new changes come in, until you are polling at infrequently as once a minute. */
	static MIN_TIME_BETWEEN_POLL = 1000;
	static MAX_TIME_BETWEEN_POLL = 60000;
	static poll_wait: number = LobbyService.MIN_TIME_BETWEEN_POLL;

	/** The time when the next poll should be made */
	static next_poll: number = 0;

	/** If we have locally made changes to the lobby state, we don't return
	 * server-side changes until the local changes have registered in the server.
	 * This tracks how many local changes we are waiting on the be registered
	 * in the server.
	 */
	static syncing_local_changes = 0;

	/** The lobby service remembers what time the last chat message
	 * came in so it can ask for only chats since that time. */
	static last_chat_timestamp = -1;

	/**
	 * Query for the latest lobby state, though this may return no changes
	 * without querying if it has queried recently enough, and it may
	 * return no changes without querying if it is synching local changes.
	 * @param force Return the last good state even if there were no changes, but
	 * still returns no changes if syncing state.
	 * @returns if there have been changes to the lobby state will return the new
	 * lobby state.  if not, will return null
	 */
	static async getLobbyState(force = false): Promise<LobbyState | null> {
		if (Date.now() < this.next_poll) {
			return (force ? this.last_state : null);
		} else if (this.syncing_local_changes > 0) {
			return null;
		} else {
			// Preemptively increases poll wait
			this.poll_wait = (this.poll_wait < LobbyService.MAX_TIME_BETWEEN_POLL/2 ?
				this.poll_wait * 1.25 : 
				LobbyService.MAX_TIME_BETWEEN_POLL
		    )
		    this.next_poll = Date.now() + this.poll_wait;

			// Now poll the server
	   		const state_from_server = await this.getLobbyStateFromServer(this.last_chat_timestamp)
			const new_state = this.cleanLobby(state_from_server)
			// While we were waiting, local changes could have come in
			if (this.syncing_local_changes > 0) {
				return null;
			}
			if (this.isLobbyStateEqual(this.last_state, new_state)) {
				// No changes.
				return (force ? this.last_state : null);
			}
			// Register a change, adjust the next poll time and last chat time,
			// and return the new state
			console.log(`Received lobby state ${LobbyStateToString(new_state)} which is different from  ${LobbyStateToString(this.last_state)}`)
			this.last_state = new_state;
			if (new_state.recent_chats.length > 0) {
				this.last_chat_timestamp = new_state.recent_chats[new_state.recent_chats.length-1].timestamp;
			}
			this.poll_wait = LobbyService.MIN_TIME_BETWEEN_POLL;
			this.next_poll = Date.now() + this.poll_wait;
			return new_state;
		}
	}

	/**
	 * Query for the latest lobby state. 
	 * @param since how far back in history to go for chats
	 * @returns online players, games and recent chats
	 */
	static async getLobbyStateFromServer(since: number): Promise<LobbyState> {
		const headers: Headers = new Headers()
		headers.set('Content-Type', 'application/json')
		headers.set('Accept', 'application/json')

		// If 'since' was defined, pass it as an argument.  Otherwise let the server
		// decicde what chats are recent enough.
		const param = (since <= 0 ? "" : `?lastactivity=${since}`)

		const request: RequestInfo = new Request(`${LobbyService.back_end}/lobby${param}`, {
			method: 'GET',
			headers: headers
		})

		return fetch(request)
			.then(res => res.json())
			.then(res => {
			return res
		})
	}

		/**
	 * Returns whether changes have happened in a lobby
	 * @param lobby1 a previous lobby state
	 * @param lobby2 the latest lobby state
	 * @returns true if no differences exist between the two lobby states.
	 */
		static isLobbyStateEqual(lobby1: LobbyState, lobby2: LobbyState): boolean {
			// Handle player names differ
			if (
				(lobby1.online_player_names.length !== lobby2.online_player_names.length) ||
				(lobby1.online_player_names.filter(x => !lobby2.online_player_names.includes(x)).length > 0))
			{
				console.log(`Change in players detected.  [${lobby1.online_player_names}] != [${lobby2.online_player_names}]`)
				return false;
			}
	
	
			// Handle games differ.
			if (lobby1.games.length !== lobby2.games.length) {
				console.log(`Change in games detected.  Games have been ${(lobby1.games.length < lobby2.games.length ? "created" : "deleted")}`)
				return false;
			}
			var same_games = true;
			const games1 = lobby1.games.sort(LobbyService.gameSortFunc)
			const games2 = lobby2.games.sort(LobbyService.gameSortFunc)
			for (var ctr=0; same_games && ctr < lobby1.games.length; ++ctr) {
				same_games = LobbyService.sameGame(games1[ctr], games2[ctr])
			}
			if (!same_games) {
				return false;
			}
	
			// Handle chats differ
			if (lobby2.recent_chats.length > 0) {
				console.log(`Changes in chats detected.  New chats detected.`)
				return false;
			}
			return true;
		}
	
	static gameSortFunc(game1: GameInLobby, game2: GameInLobby): number {
		return (game1.session - game2.session);
	}

	static sameGame(game1: GameInLobby, game2: GameInLobby): boolean {
		if (game1.session !== game2.session) {
			const min_session = (game1.session < game2.session ? game1.session : game2.session)
			console.log(`Changes in game detected.  Game ${min_session} either created or deleted`)
			return false;
		}
		if ((game1.fast_dragons !== game2.fast_dragons) ||
			(game1.fearful_dragons !== game2.fearful_dragons) ||
			(game1.game_number !== game2.game_number)) {
			
			// This shouldn't really happen.  Only the players should change
			console.log(`Changes in game detected.  Game ${game1.session} had attributes changed.`)
			return false;
		}
		if (game1.number_players !== game2.number_players) {
			console.log(`Changes in game detected.  Game ${game1.session} changed number players. ${game1.number_players}->${game2.number_players}`)
			return false;
		}
		if (game1.display_names.toString() !== game2.display_names.toString()) {
			console.log(`Changes in game detected.  Game ${game1.session} changed players. ${game1.display_names}->${game2.display_names}`)
			return false;
		}
		if (game1.state !== game2.state) {
			console.log(`Changes in game detected.  Game ${game1.session} changed state. ${game1.state}->${game2.state}`)
			return false;
		}
		return true;
	}

	/**
	 * Examine the lobby state and find old data and clean it up
	 * by making calls the the lobby back end (and by modifying 
	 * the current state object)
	 * @param lobby the state of the lobby
	 * @returns a new, cleaned up state
	 */
	static cleanLobby(lobby: LobbyState) : LobbyState {
		// The backend already filters out players that have gone offline

		// Go through games removing players who are no longer active
		const empty_games: GameInLobby[] = []
		for (const game of lobby.games) {
			const originalNumPlayers = game.display_names.length
			game.display_names = game.display_names.filter((player_name: string) => lobby.online_player_names.indexOf(player_name)>=0)
	  		game.player_names = reorder(game.display_names, game.order)
			if (game.display_names.length === 0) {
				// We don't modify the list of games while iterating over it so we save
				// empty games and delete them afterwards
				empty_games.push(game)
			} else if (game.display_names.length !== originalNumPlayers) {
				// We make this call asynchronously.  Don't need to wait for response.
				GameService.updateGame(game, null)
			}
		}

		// Remove all games with no players
		for (const game of empty_games) {
			// We make this call asynchronously.  Don't need to wait for response.
			GameService.deleteGame(game)
		}
		lobby.games = lobby.games.filter((game: GameInLobby)=>empty_games.indexOf(game)<0)

		// Chats are always recent and don't need to be cleaned.

		return lobby;
	}

	/**
	 * When we make changes, to make sure we stay in sync with the server we
	 * follow a protocol.  First, stop all regular refreshes from the server.
	 * Any in flight refreshes will be ignored on return.
	 * Second, submit the network call that changes state and wait until it
	 * returns.
	 * Third, request the latest state from the server, and only once it returns
	 * do we unlock regular refreshes.
	 * While in this process, buttons are disabled to prevent more than one change
	 * in server state.
	 * @param action a function that will perform the network request like 
	 *   creating a game or posting a chat
	 * @returns a tuple of the new lobby state and whether the update was successful
	 */
	static async sync_action_with_backend(action: () => Promise<boolean>): Promise<[LobbyState, boolean]> {
		// First, stop all regular refreshes
		// Once syncing_local_changes is non-zero, refreshes are halted
		this.syncing_local_changes += 1;
		console.log(`${new Date().toISOString().substring(11,23)} - Locking changes.  Executing action.`)
		// Second, submit the network call
		const success = await action();

		console.log(`${new Date().toISOString().substring(11,23)} - Action returned.  Getting new state.`)
		// Third, request a refresh from the server
		const new_lobby_state = await this.getLobbyStateFromServer(this.last_chat_timestamp);

		// Update LobbyService state and unlock regular refreshes
		this.last_state = new_lobby_state;
		if (new_lobby_state.recent_chats.length > 0) {
			this.last_chat_timestamp = new_lobby_state.recent_chats[new_lobby_state.recent_chats.length-1].timestamp;
		}
		this.poll_wait = LobbyService.MIN_TIME_BETWEEN_POLL;
		this.next_poll = Date.now() + this.poll_wait;
		--this.syncing_local_changes;

		console.log(`${new Date().toISOString().substring(11,23)} - Received synced state: ${LobbyStateToString(new_lobby_state)}.  Unlocking changes.`)
		return [new_lobby_state, success];
  	}

}

