import React, { useEffect, useState } from 'react';
import ListGroup from 'react-bootstrap/ListGroup';
import '../App.css';
import '../css/Lobby.css'
import PlayerService from '../services/PlayerService'

interface RosterListProps {
  /** The name of the currently logged in user */
  player_names: string[];
}

/**
 * Displays a list of players that are online
 */
function Roster({player_names}: RosterListProps) {
  return (

    <div className="lobby-roster-column lobby-room">
      <h2>Online Players</h2>
      <header className="Roster-header">
        <ListGroup>
            {player_names.map((name) => (
            <ListGroup.Item key={name}>{name}</ListGroup.Item>
            ))}
        </ListGroup>
      </header>
    </div>
  );
}

export default Roster;
