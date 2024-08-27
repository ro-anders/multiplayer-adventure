import React, { useState } from 'react';
import Button from 'react-bootstrap/Button';
import Form from 'react-bootstrap/Form';
import '../App.css';
import GameBroker from '../components/GameBroker'
import Roster from '../components/Roster'

interface LobbyProps {
  /** The name of the currently logged in user */
  username: string;
}

function Lobby({username}: LobbyProps) {
  let [chosenSlot, setChosenSlot] = useState<Number>(-1);
  let [chosenSession, setChosenSession] = useState<string>("");
  let [hostIp, setHostIp] = useState<string>("127.0.0.1");
  let [url, setUrl] = useState<string>("");

  return (

    <div className="App">
      <header className="App-header">
        <Form.Label>Welcome {username}</Form.Label>
        <Roster/>
        <GameBroker username={username}/>
        <hr/>
        <Form.Select aria-label="Slot Chooser" onChange={(value)=>setChosenSlot(parseInt(value.target.value))}>
          <option>Which player:</option>
          <option value="0" key="0">Player 1</option>
          <option value="1" key="1">Player 2</option>
        </Form.Select>
        <Form.Label>Game Backend IP</Form.Label>
        <Form.Control type="text" placeholder="127.0.0.1" onChange={(value)=>setHostIp(value.target.value)} />
        <Button href={process.env.REACT_APP_MPLAYER_GAME_URL+"?gamecode="+chosenSession+"&slot="+chosenSlot+"&host="+hostIp}>Launch Game</Button>
        <a
          className="App-link"
          href="https://reactjs.org"
          target="_blank"
          rel="noopener noreferrer"
        >
          React.js
        </a>
      </header>
    </div>
  );
}

export default Lobby;
