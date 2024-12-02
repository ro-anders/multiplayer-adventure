import React, { useEffect, useState } from 'react';
import {
  createBrowserRouter,
  RouterProvider,
} from "react-router-dom";
import 'bootstrap/dist/css/bootstrap.min.css';
import LoginPage from "./pages/Login";
import LobbyPage from "./pages/Lobby";
import PlayerService from './services/PlayerService'
import RetroGamePage from './pages/RetroGamePage';
import './App.css';
import Constants from './Constants';




function App() {

  let [username, setUsername] = useState<string>("");
  let [experienceLevel, setExperienceLevel] = useState<number>(0);
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
    }, Constants.PLAYER_PING_PERIOD);

    //Must clearing the interval to avoid memory leak.
    return () => clearInterval(interval);
  }, [username]);


  let loginPage = <LoginPage username={username} setUsername={setUsername} experienceLevel={experienceLevel} setExperienceLevel={setExperienceLevel}/>
  const router = createBrowserRouter([
    {
      path: "/",
      element: loginPage
    },
    {
      path: "/login",
      element: loginPage
    },
    {
      path: "/lobby",
      element: <LobbyPage username={username} experience_level={experienceLevel}/>
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
