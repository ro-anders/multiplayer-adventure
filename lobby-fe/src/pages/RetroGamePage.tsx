import React from 'react';
import '../RetroGamePage.css';

const RetroGamePage = () => {
  return (
    <div className="retro-container">
      <h1 className="retro-title">80's Game Maze Lobby</h1>

      <div className="retro-main">
        <div className="players-list room">
          <h2>Players Online</h2>
          <ul>
            <li>Player 1</li>
            <li>Player 2</li>
            <li>Player 3</li>
            <li>Player 4</li>
          </ul>
        </div>

        <div className="chat-panel room">
          <h2>Chat</h2>
          <div className="chat-box">
            <p><strong>Player 1:</strong> Ready to play!</p>
            <p><strong>Player 2:</strong> Let’s go!</p>
          </div>
          <input type="text" placeholder="Type a message..." />
          <button>Send</button>
        </div>

        <div className="games-list room">
          <h2>Ongoing Games</h2>
          <ul>
            <li>
              <p>Map: <strong>Desert</strong></p>
              <p>Level: <strong>3</strong></p>
              <p>Players: Player 1, Player 2</p>
            </li>
            <li>
              <p>Map: <strong>Forest</strong></p>
              <p>Level: <strong>5</strong></p>
              <p>Players: Player 3, Player 4</p>
            </li>
            <li>
              <p>Map: <strong>City</strong></p>
              <p>Level: <strong>7</strong></p>
              <p>Players: Player 1, Player 4</p>
            </li>
          </ul>
        </div>
      </div>

      <div className="retro-footer">
        <a href="#">Home</a> | <a href="#">Profile</a> | <a href="#">Logout</a>
      </div>
    </div>
  );
};

export default RetroGamePage;
