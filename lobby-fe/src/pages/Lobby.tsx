import React, { useEffect, useState } from 'react';
import Button from 'react-bootstrap/Button';
import Form from 'react-bootstrap/Form';
import '../App.css';
import GameBroker from '../components/GameBroker'
import LobbyService from '../services/LobbyService'
import { GameInLobby } from '../domain/GameInLobby'
import { LobbyState } from '../domain/LobbyState'
import Roster from '../components/Roster'


// When changes come in, poll every second for new changes, but back off as 
// no new changes come in, until you are polling at infrequently as once a minute. */
const MIN_TIME_BETWEEN_POLL = 1000;
const MAX_TIME_BETWEEN_POLL = 60000;


interface LobbyProps {
  /** The name of the currently logged in user */
  username: string;
}

function Lobby({username}: LobbyProps) {
  const [pollWait, setPollWait] = useState(MIN_TIME_BETWEEN_POLL);
  const [lobbyState, setLobbyState] = useState<LobbyState>({online_player_names: ['loading...'], games: []})
  let [chosenSlot, setChosenSlot] = useState<Number>(-1);
  let [chosenSession, setChosenSession] = useState<string>("");
  let [hostIp, setHostIp] = useState<string>("127.0.0.1");
  let [url, setUrl] = useState<string>("");

  async function updateLobbyState() {
    try {
      const new_lobby_state = await LobbyService.getLobbyState();
      if (LobbyService.isLobbyStateEqual(lobbyState, new_lobby_state)) {
        // No change in state.  Increase the time between polling.
        if (pollWait < MAX_TIME_BETWEEN_POLL) {
          setPollWait(pollWait < MAX_TIME_BETWEEN_POLL/2 ? 2*pollWait : MAX_TIME_BETWEEN_POLL)
        }
      }
      else {
        console.log("Lobby state changed")
          setLobbyState(new_lobby_state)
          setPollWait(MIN_TIME_BETWEEN_POLL)
      }
      console.log(`Waiting ${pollWait}`)
    }
    catch(e) {
      console.error(`Error requesting lobby state: ${e}`)
      setPollWait(MAX_TIME_BETWEEN_POLL)
    }
  }

  /**
   * Callback called by subcomponents that change the game state.
   * It immediately updates the list, but then schedules a new poll
   * to poll the server for it's updated list.
   * @param new_game_list a modified list of games
   */
  function game_change_callback(new_game_list: GameInLobby[]) {
    const new_lobby_state: LobbyState = {
      online_player_names: lobbyState.online_player_names,
      games: new_game_list
    }
    setLobbyState(new_lobby_state);
    // We want to get the latest from the server, but we do wait
    // a second to make sure whatever local change has posted to the server.
    setPollWait(MIN_TIME_BETWEEN_POLL)
  }
  
  /**
   * Setup a timer to update the lobby state.
   * It starts out updating at a frequent rate but
   * as if no changes are detected it updates less frequently.
   */
  useEffect(() => {
    const timer = setInterval(() => {
      updateLobbyState();
    }, pollWait);
    return () => {
      clearInterval(timer);
    };
  }, [lobbyState, pollWait]);

  return (
    <div>
      <div className="lobby-main">
        <Roster player_names={lobbyState.online_player_names}/>
        <div className="lobby-game-column">
          <div className="lobby-room">
            <GameBroker 
              username={username} 
              proposed_games={lobbyState.games}
              game_change_callback={game_change_callback}
            />
          </div>
          <div className="lobby-room">
            <h2>Running Games</h2>
          </div>
        </div>
        <div className="lobby-chat-column lobby-room">
          <h2>Chat</h2>
          <div className="chat-box">
            <p><strong>Player 1:</strong> Ready to play!</p>
            <p><strong>Player 2:</strong> Let’s go!</p>
          </div>
          <input type="text" placeholder="Type a message..." />
          <button>Send</button>
        </div>
      </div>
      <a
            className="App-link"
            href="https://reactjs.org"
            target="_blank"
            rel="noopener noreferrer"
          >
            React.js
      </a>
    </div>
  );
}

export default Lobby;
