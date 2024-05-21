import React, { useEffect, useState } from 'react';
import Button from 'react-bootstrap/Button';
import Form from 'react-bootstrap/Form';
import logo from './logo.svg';
import './App.css';
import GameService from './services/GameService'

function App() {
  const label = GameService.getLabel();
  let [load_games, setLoadGames] = useState<boolean>(true);
  let [games, setGames] = useState<string[]>([]);
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
        <Form.Select aria-label="Game Chooser" onChange={(value)=>setUrl("http://localhost:56205/?session=" + value.target.value)}>
          <option>Choose a game:</option>
          {games.map((game) => (
            <option value={game} key={game}>{game}</option>
          ))}
        </Form.Select>
        <Button href={url}>Launch Game</Button>
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
