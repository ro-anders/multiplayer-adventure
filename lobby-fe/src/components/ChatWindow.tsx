import ListGroup from 'react-bootstrap/ListGroup';
import '../App.css';
import '../css/Lobby.css'
import { Chat } from '../domain/LobbyState'

interface ChatWindowProps {
  /** The name of the currently logged in user */
  current_user: string;
  chats: Chat[];
}

/**
 * Displays a list of players that are online
 */
function ChatWindow({current_user, chats}: ChatWindowProps) {
  return (

    <div className="lobby-chat-column lobby-room">
      <h3>Chat</h3>
      <div className="chat-box border p-2">
          <div key={1} className="chat-message"><strong>Player 3:</strong> Ready to play!</div>
          <div key={2} className="chat-message"><strong>Player 4:</strong> Let's go!</div>
          <div key={3} className="chat-message"><strong>Player 1:</strong> Ready to play!</div>
          <div key={4} className="chat-message"><strong>Player 2:</strong> Let's go!</div>
          <div key={5} className="chat-message"><strong>Player 1:</strong> Ready to play!</div>
          <div key={6} className="chat-message"><strong>Player 2:</strong> Let's go!</div>
          <div key={7} className="chat-message"><strong>Player 1:</strong> Ready to play!</div>
          <div key={8} className="chat-message"><strong>Player 2:</strong> Let's go!</div>
          <div key={9} className="chat-message"><strong>Player 1:</strong> Ready to play!</div>
          <div key={10} className="chat-message"><strong>Player 2:</strong> Let's go!</div>
          <div key={11} className="chat-message"><strong>Player 1:</strong> Ready to play!</div>
          <div key={12} className="chat-message"><strong>Player 2:</strong> Let's go!</div>
          <div key={13} className="chat-message"><strong>Player 1:</strong> Ready to play!</div>
          <div key={14} className="chat-message"><strong>Player 2:</strong> Let's go!</div>
          <div key={15} className="chat-message"><strong>Player 1:</strong> Ready to play!</div>
          <div key={16} className="chat-message"><strong>Player 2:</strong> Let's go!</div>
          <div key={17} className="chat-message"><strong>Player 1:</strong> Ready to play!</div>
          <div key={18} className="chat-message"><strong>Player 2:</strong> Let's go!</div>
          <div key={19} className="chat-message"><strong>Player 1:</strong> Ready to play!</div>
          <div key={20} className="chat-message"><strong>Player 2:</strong> Let's go!</div>
          <div key={21} className="chat-message"><strong>Player 1:</strong> Ready to play!</div>
          <div key={22} className="chat-message"><strong>Player 2:</strong> Let's go!</div>
          <div key={23} className="chat-message"><strong>Player 1:</strong> Ready to play!</div>
          <div key={24} className="chat-message"><strong>Player 2:</strong> Let's go!</div>
          <div key={25} className="chat-message"><strong>Player 1:</strong> Ready to play!</div>
          <div key={26} className="chat-message"><strong>Player 2:</strong> Let's go!</div>
          <div key={27} className="chat-message"><strong>Player 1:</strong> Ready to play!</div>
          <div key={28} className="chat-message"><strong>Player 2:</strong> Let's go!</div>
          <div key={29} className="chat-message"><strong>Player 1:</strong> Ready to play!</div>
          <div key={30} className="chat-message"><strong>Player 2:</strong> Let's go!</div>
          <div key={31} className="chat-message"><strong>Player 1:</strong> Ready to play!</div>
          <div key={32} className="chat-message"><strong>Player 2:</strong> Let's go!</div>
          <div key={33} className="chat-message"><strong>Player 1:</strong> Ready to play!</div>
          <div key={34} className="chat-message"><strong>Player 2:</strong> Let's go!</div>
          <div key={35} className="chat-message"><strong>Player 1:</strong> Ready to play!</div>
          <div key={36} className="chat-message"><strong>Player 2:</strong> Let's go!</div>
          <div key={37} className="chat-message"><strong>Player 1:</strong> Ready to play!</div>
          <div key={38} className="chat-message"><strong>Player 2:</strong> Let's go!</div>
          <div key={39} className="chat-message"><strong>Player 1:</strong> Ready to play!</div>
          <div key={40} className="chat-message"><strong>Player 2:</strong> Let's go!</div>
          <div key={41} className="chat-message"><strong>Player 1:</strong> Ready to play!</div>
          <div key={42} className="chat-message"><strong>Player 2:</strong> Let's go!</div>
          <div key={43} className="chat-message"><strong>Player 1:</strong> Ready to play!</div>
          <div key={44} className="chat-message"><strong>Player 2:</strong> Let's go!</div>
          <div key={45} className="chat-message"><strong>Player 1:</strong> Ready to play!</div>
          <div key={46} className="chat-message"><strong>Player 2:</strong> Let's go!</div>
          <div key={47} className="chat-message"><strong>Player 1:</strong> Ready to play!</div>
          <div key={48} className="chat-message"><strong>Player 2:</strong> Let's go!</div>
          <div key={49} className="chat-message"><strong>Player 1:</strong> Ready to play!</div>
          <div key={50} className="chat-message"><strong>Player 2:</strong> Let's go!</div>
          <div key={51} className="chat-message"><strong>Player 1:</strong> Ready to play!</div>
          <div key={52} className="chat-message"><strong>Player 2:</strong> Let's go!</div>
          <div key={53} className="chat-message"><strong>Player 1:</strong> Ready to play!</div>
          <div key={54} className="chat-message"><strong>Player 2:</strong> Ok, here's a longer message, just to see how that gets displayed</div>
          <div key={55} className="chat-message"><strong>Player 3:</strong> Ready to play!</div>
          <div key={56} className="chat-message"><strong>Player 4:</strong> Let's go!</div>
          <div id="scroll-anchor"></div>
        </div>
      <input type="text" placeholder="Type a message..." />
      <button>Send</button>
    </div>
);
}

export default ChatWindow;
