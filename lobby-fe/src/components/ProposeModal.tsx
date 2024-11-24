import { useState } from 'react';
import { Modal, Button, Form } from 'react-bootstrap';
import { GameInLobby } from '../domain/GameInLobby';
import "../css/ProposeModal.css"

interface ProposeModalProps {
    /** The current username */
    username: string;
    /** Whether the modal is visible */
    show: boolean;
    /** The callback to register a newly proposed game */
    propose_game_callback: (new_game: GameInLobby) => void;
    /** The callback to hide the modal */
    onHide: () => void;
  }
  
/**
 * Displays the list of proposed games, allowing the user to join one.
 * Also displays an option to propose a new game.
 */
function ProposeModal({username, show, onHide, propose_game_callback}: ProposeModalProps) {
  const [selectedGame, setSelectedGame] = useState("2");
  const [selectedNumPlayers, setSelectedNumPlayers] = useState("2.5");
  const [isFastDragonsOn, setIsFastDragonsOn] = useState(false);
  const [isScarySwordOn, setIsScarySwordOn] = useState(false);

  function proposeClicked() {
    const new_game: GameInLobby = {
      session: 0, // Backend will provide real session number
      game_number: parseInt(selectedGame) ,
      number_players: parseFloat(selectedNumPlayers),
      fast_dragons: isFastDragonsOn,
      fearful_dragons: isScarySwordOn,
      player_names: [username],
      state: 0
    };
    propose_game_callback(new_game);
    onHide();
  }

  return (
        <Modal 
          dialogClassName="propose-modal App" 
          size="lg"
          show={show} 
          onHide={onHide}>
        <Modal.Header closeButton>
          <Modal.Title>Choose Game Settings</Modal.Title>
        </Modal.Header>
        <Modal.Body className="propose-modal-body">
          <div className="propose-modal-field">
            <Form.Label>Choose a game board</Form.Label>
            <Form.Select 
                aria-label="Choose a game board" 
                value={selectedGame}
                onChange={(e) => setSelectedGame(e.target.value)}
            >
              <option value="0" key="0">Game 1: Small starter map</option>
              <option value="1" key="1">Game 2: Standard map</option>
              <option value="2" key="2">Game 3: Standard map randomized</option>
            </Form.Select>
          </div>
          <div className="propose-modal-field">
            <Form.Label>How many players?</Form.Label>
            <Form.Select 
                aria-label="Number players" 
                value={selectedNumPlayers}
                onChange={(e) => setSelectedNumPlayers(e.target.value)}
            >
              <option value="2" key="0">Exactly 2 players</option>
              <option value="3" key="1">Exactly 3 players</option>
              <option value="2.5" key="2">Minimum 2, Maximum 3</option>
            </Form.Select>
          </div>
          <div className="propose-modal-field">
            <Form.Check
              type='switch'
              id='diffl'
              label='Fast dragons'
              checked={isFastDragonsOn}
              onChange={() => setIsFastDragonsOn(!isFastDragonsOn)}
            />
            <Form.Check
              type='switch'
              id='diffr'
              label='Scary sword (dragons run from it)'
              checked={isScarySwordOn}
              onChange={() => setIsScarySwordOn(!isScarySwordOn)}
            />
          </div>
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={onHide}>
            Cancel
          </Button>
          <Button variant="primary" onClick={proposeClicked}>
            Propose
          </Button>
        </Modal.Footer>
      </Modal>
      );
}
    
export default ProposeModal;
    