import { useEffect, useState } from 'react';
import '../App.css';
import GameBroker from '../components/GameBroker'
import LobbyService from '../services/LobbyService'
import { GameInLobby, GAMESTATE_RUNNING } from '../domain/GameInLobby'
import { LobbyState, LobbyStateToString, ReceivedChat } from '../domain/LobbyState'
import ChatWindow from '../components/ChatWindow'
import Roster from '../components/Roster'
import ProposedGameList from '../components/ProposedGameList';
import ChatService from '../services/ChatService';
import GameService from '../services/GameService';


interface LobbyProps {
  /** The name of the currently logged in user */
  username: string;
  /** How much help they need.  0=no help, 1=map guides, 2=map guides & popup hints */
  experience_level: number;
}

function Lobby({username, experience_level}: LobbyProps) {
  const [lobbyState, setLobbyState] = useState<LobbyState>(
    {online_player_names: ['loading...'], games: [], recent_chats: []}
  )
  const [allChats, setAllChats] = useState<ReceivedChat[]>([]);
  const [actionsDisabled, setActionsDisabled] = useState<boolean>(false);

  /**
   * Callback called when a new chat message needs to be posted.
   * Creates a "local" chat to display until the new chat message
   * is synced with the server, then posts the new chat and syncs
   * with the lobby backend.
   * @param new_chat_message the message of the chat to post. 
   */
  async function new_chat_callback(new_chat_message: string) {
    // First add a local chat to the list do it displays while synching
    const localChat: ReceivedChat = {
      player_name: username,
      message: new_chat_message,
      timestamp: 0
    }
    setActionsDisabled(true);
    setAllChats([...allChats, localChat])
    const action = async function (): Promise<void> {await ChatService.postChat(username, new_chat_message)};
    const new_lobby_state = await LobbyService.sync_action_with_backend(action);
    updateLobbyState(new_lobby_state);
    setActionsDisabled(false);
  }

  /**
   * Callback called by subcomponents that change the game state.
   * It immediately updates the list, but then syncs the change
   * with the backend server.
   * @param new_game_list a modified list of games
   */
  async function game_change_callback(updated_game: GameInLobby) {
    setActionsDisabled(true);
    const updated_game_in_list = lobbyState.games.find((game)=> game.session === updated_game.session)
    if (!updated_game_in_list) {
      // This is a new game.  Add a new game to the lobby state then post a create game request
      const new_lobby_state: LobbyState = {
        online_player_names: lobbyState.online_player_names,
        games: lobbyState.games.concat([updated_game]),
        recent_chats: []
      }
      console.log(`Locally created new game.  Updating local lobby state to ${LobbyStateToString(new_lobby_state)}.`)
      setLobbyState(new_lobby_state);
      console.log("Sending game change to server.")
      const action = async function (): Promise<void> {await GameService.proposeNewGame(updated_game)}
      const synced_lobby_state = await LobbyService.sync_action_with_backend(action);
      updateLobbyState(synced_lobby_state);
    } else {
      // Player has either joined or left a game
      const index_of_game = lobbyState.games.indexOf(updated_game_in_list)
      const new_game_list = lobbyState.games.slice()
      new_game_list[index_of_game] = updated_game;
      const new_lobby_state: LobbyState = {
        online_player_names: lobbyState.online_player_names,
        games: new_game_list,
        recent_chats: []
      }
      console.log(`Locally updated game.  Updating local lobby state to ${LobbyStateToString(new_lobby_state)}.`)
      setLobbyState(new_lobby_state);
      console.log("Sending game change to server.")
      const action = async function (): Promise<void> {await GameService.updateGame(updated_game)}
      const synced_lobby_state = await LobbyService.sync_action_with_backend(action);
      updateLobbyState(synced_lobby_state);
    }
    setActionsDisabled(false);
  }

  /**
   * Whether a chat is contained by a list of chats
   * @param chats a list of chats
   * @param other_chat a chat to look for
   * @returns true if the list contains a chat with the same sender, message and timestamp
   */
  function containsChat(chats: ReceivedChat[], other_chat: ReceivedChat) : boolean {
    const foundChat = chats.find((chat: ReceivedChat)=>{
      return chat.message===other_chat.message && 
        other_chat.player_name===chat.player_name &&
        other_chat.timestamp===chat.timestamp})
    return !!foundChat;
  }

  /**
   * Merge two sets of chats.
   * @param old_chats a list of chat messages that may have one or more locally created chats
   *   on the end (chats posted by this client but not yet registered in the server).  
   * @param new_chats a list of chats that have been posted to the server 
   *   recently.  It may contain duplicates of chats in old_chats.  
   * @return a list of old chats and new chats without and duplication and without any locally 
   * created chats (if the syncing is done right, the new chats from the server
   * should include them)
   */
  function mergeChats(old_chats: ReceivedChat[], new_chats: ReceivedChat[]) : ReceivedChat[] {
    // First remove all locally created chats from old_chats
    const old_chats_no_local = old_chats.filter((chat: ReceivedChat)=>{return chat.timestamp > 0})
    // Remove all new chats that are already in old_chats.  
    var new_chats_no_dupes: ReceivedChat[] = new_chats;
    // Both lists are sorted by timestamp
    // so we only have to dedupe if the last chat in old_chats is older than the
    // first chat in new_chats.
    if ((old_chats.length > 0) && (new_chats.length > 0) &&
        (old_chats[old_chats.length-1].timestamp >= new_chats[0].timestamp)) {
      new_chats_no_dupes = 
        new_chats.filter((chat: ReceivedChat)=>{return !containsChat(old_chats, chat)});
    }
    const merged = old_chats_no_local.concat(new_chats_no_dupes)
    return merged;
  }

  /**
   * A new lobby state has been received from the server.  
   * Call the setLobbyState() and setAllChats() appropriately.
   * @param new_lobby_state new state received from LobbyService
   */
  function updateLobbyState(new_lobby_state: LobbyState) {
    console.log(`Received new lobby state from server.  Updating local lobby state to ${LobbyStateToString(new_lobby_state)}.`)
    const new_all_chats = mergeChats(allChats, new_lobby_state.recent_chats)
    console.log(`Merging ${allChats.length} old with ${new_lobby_state.recent_chats.length} new = ${new_all_chats.length} chats`)
    // We do limit the number of chats we track
    if (new_all_chats.length > 200) {
      new_all_chats.length = 200;
    }
    setAllChats(new_all_chats)
    setLobbyState(new_lobby_state)
  }
  
  /**
   * Setup a timer to update the lobby state.
   * It starts out updating at a frequent rate but
   * as no changes are detected it updates less frequently.
   */
  useEffect(() => {
    async function refreshLobbyState() {
      try {
        const new_lobby_state = await LobbyService.getLobbyState();
        if (new_lobby_state) {
          updateLobbyState(new_lobby_state)
        }
      }
      catch(e) {
        console.error(`Error requesting lobby state: ${e}`)
      }
    }
  
    const timer = setInterval(() => {
      refreshLobbyState();
    }, 1000);
    return () => {
      clearInterval(timer);
    };
  }, [lobbyState]);

  console.log("Rendering lobby")
  return (
      <div className="lobby-main">
        <Roster player_names={lobbyState.online_player_names}/>
        <div className="lobby-games-column">
          <div className="lobby-room">
            <GameBroker 
              username={username} 
              experience_level={experience_level}
              proposed_games={lobbyState.games}
              game_change_callback={game_change_callback}
              actions_disabled={actionsDisabled}
            />
          </div>
          <div className="lobby-room">
            <ProposedGameList 
              current_user={username} 
              experience_level={experience_level}
              games={lobbyState.games}
              game_change_callback={(games)=>{}} // A noop callback
              state_to_display={GAMESTATE_RUNNING}
              actions_disabled={actionsDisabled}
            />
          </div>
        </div>
        <ChatWindow 
          current_user={username} 
          chats={allChats}
          new_chat_callback={new_chat_callback}
          actions_disabled={actionsDisabled}/>
      </div>
  );
}

export default Lobby;
