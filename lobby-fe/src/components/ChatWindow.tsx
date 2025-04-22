import { useState } from 'react';
import Button from 'react-bootstrap/Button';
import '../App.css';
import '../css/Lobby.css'
import { ReceivedChat } from '../domain/LobbyState'

interface ChatWindowProps {
  /** The name of the currently logged in user */
  current_user: string;
  chats: ReceivedChat[];
  /** Callback to call when new chats are posted */
  new_chat_callback: (new_chat_message: string) => void;
  /** Whether to enable the post-new-chat button.  It is disabled while syncing with the server. */
  actions_disabled: boolean;
}

/**
 * Displays a list of players that are online
 */
function ChatWindow({current_user, chats, new_chat_callback, actions_disabled}: ChatWindowProps) {

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

  /**
   * The user posts a chat either with the post button or the return key.
   * Monitor the input field for the return key.
   * @param event the key down event 
   */
  const handleKeyDown = (event: React.KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'Enter') {
      postChatMessage()
    }
  };

  return (

    <div className="lobby-column lobby-chat-column lobby-room">
      <h3>Chat</h3>
      <div className="chat-box border p-2">
        {chats.map((chat) => (
          <div key={chat.player_name + chat.timestamp} className="chat-message"><strong>{chat.player_name}:</strong> {chat.message}</div>
        ))}
          <div id="scroll-anchor"></div>
        </div>
      <input type="text" placeholder="Type a message..." 
        value={newChatText} 
        onChange={(event) => setNewChatText(event.target.value)}
        onKeyDown={handleKeyDown} />
      <Button disabled={actions_disabled} onClick={postChatMessage}>Send</Button>
    </div>
  );
}

export default ChatWindow;
