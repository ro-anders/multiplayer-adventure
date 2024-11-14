import React, { useEffect, useState } from 'react';
import {
  createBrowserRouter,
  RouterProvider,
} from "react-router-dom";
import './App.css';
import 'bootstrap/dist/css/bootstrap.min.css';
import LoginPage from "./pages/Login";
import LobbyPage from "./pages/Lobby";
import PlayerService from './services/PlayerService'
import RetroGamePage from './pages/RetroGamePage';




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
    },
    {
      path: "/retro",
      element: <RetroGamePage/>
    }
  ]);
  return (

    <div className="App">
      <header className="App-header">
        <RouterProvider router={router} />
      </header>
    </div>
  );
}

export default App;
