import { useState } from 'react';
import Button from 'react-bootstrap/Button';
import Form from 'react-bootstrap/Form';
import '../App.css';
import '../css/ProposeGameModal.css'
import { GameInLobby } from '../domain/GameInLobby';

interface ProposeGameModalProps {
  username: string,
  propose_game_callback: (new_game: GameInLobby) => void;
  close_modal_callback: () => void; 
}

/**
 * Displays form to specify and propose a game.
 */
function ProposeGameModal({username, propose_game_callback, close_modal_callback}: ProposeGameModalProps) {
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
      close_modal_callback();
    }

    return (
        <div className="modal-overlay">
          <div className="modal-dialog">
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
            <Button onClick={proposeClicked}>Propose</Button>
            <Button onClick={close_modal_callback}>Cancel</Button>
          </div>
        </div>
      );
    }

export default ProposeGameModal;
