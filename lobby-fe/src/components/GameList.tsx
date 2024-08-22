import React, { useEffect, useState } from 'react';
import ListGroup from 'react-bootstrap/ListGroup';
import '../App.css';
import {Game} from '../domain/Game'
import GameService from '../services/GameService'

/**
 * 
 */
function GameList() {
  let [proposedGames, setProposedGames] = useState<Game[]>([]);

  async function getProposedGames() {
    const games = await GameService.getGames("proposed");
    setProposedGames(games)
  }

  useEffect(() => {
    // Query now and once every interval
    getProposedGames();
    const interval = setInterval(() => {
        getProposedGames();
    }, 10000);

    //Must clearing the interval to avoid memory leak.
    return () => clearInterval(interval);
  }, []);

  return (

    <div className="Roster">
      <h2>Join a proposed game</h2>
      <header className="Roster-header">
        <ListGroup>
            {proposedGames.map((game) => (
            <ListGroup.Item key={game.player1_name}>Game {game.game_number} proposed by {game.player1_name}</ListGroup.Item>
            ))}
        </ListGroup>
      </header>
    </div>
  );
}

export default GameList;
