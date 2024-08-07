import React, { useEffect, useState } from 'react';
import Button from 'react-bootstrap/Button';
import Form from 'react-bootstrap/Form';
import logo from './logo.svg';
import './App.css';
import GameService from './services/GameService'

function App() {
  const label = GameService.getLabel();
  let [load_games, setLoadGames] = useState<boolean>(true);
  let [games, setGames] = useState<string[]>(["1", "2", "3"]);
  let [chosenSlot, setChosenSlot] = useState<Number>(-1);
  let [chosenSession, setChosenSession] = useState<string>("");
  let [hostIp, setHostIp] = useState<string>("127.0.0.1");
  let [url, setUrl] = useState<string>("");

  useEffect(() => {
    async function fetchGames() {
      if (load_games) { 
        const fetched_games = await GameService.getGames()
        setGames(fetched_games)
      }
      setLoadGames(false)
    }

    fetchGames();
  }, [load_games])

  return (

    <div className="App">
      <header className="App-header">
        <img src={logo} className="App-logo" alt="logo" />
        <Form.Select aria-label="Game Chooser" onChange={(value)=>setChosenSession(value.target.value)}>
          <option>Choose a game:</option>
          {games.map((game) => (
            <option value={game} key={game}>{game}</option>
          ))}
        </Form.Select>
        <Form.Select aria-label="Slot Chooser" onChange={(value)=>setChosenSlot(parseInt(value.target.value))}>
          <option>Which player:</option>
          <option value="0" key="0">Player 1</option>
          <option value="1" key="1">Player 2</option>
        </Form.Select>
        <Form.Label>Game Backend IP</Form.Label>
        <Form.Control type="text" placeholder="127.0.0.1" onChange={(value)=>setHostIp(value.target.value)} />
        <Button href={process.env.REACT_APP_MPLAYER_GAME_URL+"?gamecode="+chosenSession+"&slot="+chosenSlot+"&host="+hostIp}>Launch Game</Button>
        <p>
          {label}
        </p>
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

export default App;
