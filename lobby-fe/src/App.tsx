import React, { useEffect, useState } from 'react';
import {
  createBrowserRouter,
  RouterProvider,
} from "react-router-dom";
import './App.css';
import LoginPage from "./pages/Login";
import LobbyPage from "./pages/Lobby";
import PlayerService from './services/PlayerService'




function App() {

  let [username, setUsername] = useState<string>("");
  useEffect(() => {
    async function registerCurrentPlayer() {
      if (username) {
        await PlayerService.registerPlayer(username);
      }
    }
    
    // Constantly re-register the current user.
    registerCurrentPlayer();
    const interval = setInterval(() => {
        registerCurrentPlayer();
    }, 60000);

    //Must clearing the interval to avoid memory leak.
    return () => clearInterval(interval);
  }, [username]);


  let defaultPage = (!!username ? 
    <LobbyPage username={username}/> : 
    <LoginPage username={username} setUsername={setUsername}/>
  )
  const router = createBrowserRouter([
    {
      path: "/",
      element: defaultPage,
    },
    {
      path: "/login",
      element: <LoginPage username={username} setUsername={setUsername}/>
    },
    {
      path: "/lobby",
      element: <LobbyPage username={username}/>
    }
  ]);
  return (

    <div className="App">
      <header className="App-header">
        <h1>Head-to-Head Atari Adventure</h1>
        <RouterProvider router={router} />
        </header>
    </div>
  );
}

export default App;
