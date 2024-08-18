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
    const interval = setInterval(() => {
        getOnlinePlayers();
    }, 10000);

    //Must clearing the interval to avoid memory leak.
    return () => clearInterval(interval);
}, [names]);

  return (

    <div className="Roster">
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
