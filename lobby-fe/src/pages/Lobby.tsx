import { useEffect, useState } from 'react';
import '../App.css';
import GameBroker from '../components/GameBroker'
import LobbyService from '../services/LobbyService'
import { GameInLobby, GAMESTATE_RUNNING } from '../domain/GameInLobby'
import { LobbyState } from '../domain/LobbyState'
import ChatWindow from '../components/ChatWindow'
import Roster from '../components/Roster'
import ProposedGameList from '../components/ProposedGameList';


// When changes come in, poll every second for new changes, but back off as 
// no new changes come in, until you are polling at infrequently as once a minute. */
const MIN_TIME_BETWEEN_POLL = 1000;
const MAX_TIME_BETWEEN_POLL = 60000;


interface LobbyProps {
  /** The name of the currently logged in user */
  username: string;
  /** How much help they need.  0=no help, 1=map guides, 2=map guides & popup hints */
  experience_level: number;
}

function Lobby({username, experience_level}: LobbyProps) {
  const [pollWait, setPollWait] = useState(MIN_TIME_BETWEEN_POLL);
  const [lobbyState, setLobbyState] = useState<LobbyState>(
    {online_player_names: ['loading...'], games: [], recent_chats: []}
  )

  /**
   * Callback called by subcomponents that change the game state.
   * It immediately updates the list, but then schedules a new poll
   * to poll the server for it's updated list.
   * @param new_game_list a modified list of games
   */
  function game_change_callback(new_game_list: GameInLobby[]) {
    // TODO: This is causing weird flicker.  Comment out to see if that works.
    // const new_lobby_state: LobbyState = {
    //   online_player_names: lobbyState.online_player_names,
    //   games: new_game_list,
    //   recent_chats: lobbyState.recent_chats
    // }
    //setLobbyState(new_lobby_state);
    
    // We want to get the latest from the server, but we do wait
    // a second to make sure whatever local change has posted to the server.
    setPollWait(MIN_TIME_BETWEEN_POLL)
  }
  
  /**
   * Setup a timer to update the lobby state.
   * It starts out updating at a frequent rate but
   * as no changes are detected it updates less frequently.
   */
  useEffect(() => {
    async function updateLobbyState() {
      try {
        const old_chats = lobbyState.recent_chats;
        const last_chat_timestamp = (old_chats.length === 0 ? -1 : old_chats[old_chats.length-1].timestamp)
        const new_lobby_state = await LobbyService.getLobbyState(last_chat_timestamp);
        // Only the most recent chats are returned, so retain the old chats from the previous state.
        new_lobby_state.recent_chats = old_chats.concat(new_lobby_state.recent_chats);
        if (LobbyService.isLobbyStateEqual(lobbyState, new_lobby_state)) {
          // No change in state.  Increase the time between polling.
          if (pollWait < MAX_TIME_BETWEEN_POLL) {
            setPollWait(pollWait < MAX_TIME_BETWEEN_POLL/2 ? 2*pollWait : MAX_TIME_BETWEEN_POLL)
          }
        }
        else {
          // 
          console.log("Lobby state changed")
            setLobbyState(new_lobby_state)
            setPollWait(MIN_TIME_BETWEEN_POLL)
        }
      }
      catch(e) {
        console.error(`Error requesting lobby state: ${e}`)
        setPollWait(MAX_TIME_BETWEEN_POLL)
      }
    }
  
    const timer = setInterval(() => {
      updateLobbyState();
    }, pollWait);
    return () => {
      clearInterval(timer);
    };
  }, [lobbyState, pollWait]);

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
            />
          </div>
          <div className="lobby-room">
            <ProposedGameList 
              current_user={username} 
              experience_level={experience_level}
              games={lobbyState.games}
              game_change_callback={(games)=>{}} // A noop callback
              state_to_display={GAMESTATE_RUNNING}
            />
          </div>
        </div>
        <ChatWindow 
          current_user={username} 
          chats={lobbyState.recent_chats}
          new_chat_callback={()=>{game_change_callback(lobbyState.games)}}/>
      </div>
  );
}

export default Lobby;
