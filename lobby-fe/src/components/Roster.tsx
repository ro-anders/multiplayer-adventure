import React, { useEffect, useState } from 'react';
import ListGroup from 'react-bootstrap/ListGroup';
import '../App.css';
import PlayerService from '../services/PlayerService'

/**
 * 
 */
function Roster() {
  let [names, setNames] = useState<string[]>(["loading..."]);

  async function getOnlinePlayers() {
    const player_names = await PlayerService.getOnlinePlayers();
    setNames(player_names)
  }

  useEffect(() => {
    // Query now and once every interval
    getOnlinePlayers();
    const interval = setInterval(() => {
        getOnlinePlayers();
    }, 10000);

    //Must clearing the interval to avoid memory leak.
    return () => clearInterval(interval);
  }, []);

  return (

    <div className="Roster">
      <h2>Online Players</h2>
      <header className="Roster-header">
        <ListGroup>
            {names.map((name) => (
            <ListGroup.Item key={name}>{name}</ListGroup.Item>
            ))}
        </ListGroup>
      </header>
    </div>
  );
}

export default Roster;
