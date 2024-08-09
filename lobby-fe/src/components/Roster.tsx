import React, { useEffect, useState } from 'react';
import ListGroup from 'react-bootstrap/ListGroup';
import '../App.css';
import GameService from '../services/GameService'

/**
 * 
 */
function Roster() {
  let [names, setNames] = useState<string[]>(["loading..."]);

  useEffect(() => {
    // Query now and once every interval
    const interval = setInterval(() => {
        setNames(["Parzival", "Art3mis", "Aech"]);
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
