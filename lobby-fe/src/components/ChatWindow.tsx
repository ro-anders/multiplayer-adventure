import { useState } from 'react';
import '../App.css';
import '../css/Lobby.css'
import { ReceivedChat } from '../domain/LobbyState'
import ChatService from '../services/ChatService';

interface ChatWindowProps {
  /** The name of the currently logged in user */
  current_user: string;
  chats: ReceivedChat[];
  /** Callback to call when new chats are posted */
  new_chat_callback: (new_chat_message: string) => void;
}

/**
 * Displays a list of players that are online
 */
function ChatWindow({current_user, chats, new_chat_callback}: ChatWindowProps) {

  let [newChatText, setNewChatText] = useState<string>("");
  
  /**
   * Post the text in the input field as a new chat message from the current user.
   * Don't worry about putting the chat in the chat window.  That will happen once the
   * server broadcasts the message.
   */
  function postChatMessage() {
    if (newChatText.trim()) {
      new_chat_callback(newChatText)
      setNewChatText("")
    }
  }

  return (

    <div className="lobby-chat-column lobby-room">
      <h3>Chat</h3>
      <div className="chat-box border p-2">
        {chats.map((chat) => (
          <div key={chat.player_name + chat.timestamp} className="chat-message"><strong>{chat.player_name}:</strong> {chat.message}</div>
        ))}
          <div id="scroll-anchor"></div>
        </div>
      <input type="text" placeholder="Type a message..." value={newChatText} onChange={(event) => setNewChatText(event.target.value)}/>
      <button onClick={postChatMessage}>Send</button>
    </div>
);
}

export default ChatWindow;
