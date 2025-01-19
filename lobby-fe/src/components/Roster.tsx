import ListGroup from 'react-bootstrap/ListGroup';
import '../App.css';
import '../css/Lobby.css'

interface RosterListProps {
  /** The name of the currently logged in user */
  player_names: string[];
}

/**
 * Displays a list of players that are online
 */
function Roster({player_names}: RosterListProps) {
  return (

    <div className="lobby-column lobby-room">
      <h3>Online Players</h3>
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
