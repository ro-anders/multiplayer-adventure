import React, { useEffect, useState } from 'react';
import {
  createBrowserRouter,
  Navigate,
  RouterProvider,
} from "react-router-dom";
import 'bootstrap/dist/css/bootstrap.min.css';
import LoginPage from "./pages/Login";
import LobbyPage from "./pages/Lobby";
import ConnectPage from "./pages/Connect";
import PlayerService from './services/PlayerService'
import RetroGamePage from './pages/RetroGamePage';
import './App.css';
import Constants from './Constants';
import UnsubscribePage from './pages/Unsubscribe';
import LeaderBoard from './pages/Leaders';




function App() {

  // Username is state, but it's also kept in session storage to survive reloads.
  // We also put it in local storage, but isn't used automatically.  It's only used to populate the 
  // login field if username is undefined.
  let [username, setUsername] = useState<string>(getInitialUsername());
  function getInitialUsername(): string {
      return sessionStorage.getItem( 'h2h.username' ) || "";    
  }
  function setNewUsername(new_username: string) {
    if (new_username) {
      sessionStorage.setItem("h2h.username", new_username);
      if (new_username !== username) {
        localStorage.setItem("h2h.username", new_username);
      }
    }
    setUsername(new_username);
  }

  // Experience level is also state persisted in session storage and in local storage.
  let [experienceLevel, setExperienceLevel] = useState<number>(getInitialExperienceLevel());
  function getInitialExperienceLevel(): number {
    const exp_level_str: string = sessionStorage.getItem( 'h2h.experience_level' ) || localStorage.getItem( 'h2h.experience_level' ) || "0"; 
    return parseInt(exp_level_str)   
  }
  function setNewExperienceLevel(new_exp_level: number) {
    sessionStorage.setItem("h2h.experience_level", new_exp_level.toString());
    if (new_exp_level !== experienceLevel) {
      localStorage.setItem("h2h.experience_level", new_exp_level.toString())
    }
    setExperienceLevel(new_exp_level);
  }

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


  const router = createBrowserRouter([
    {
      path: "/",
      element: <Navigate to="/login" replace />
    },
    {
      path: "/login",
      element: <LoginPage username={username} setUsername={setNewUsername} experienceLevel={experienceLevel} setExperienceLevel={setNewExperienceLevel}/>
    },
    {
      path: "/lobby",
      element: username ? <LobbyPage username={username} experience_level={experienceLevel}/> :
        <Navigate to="/login" replace />
    },
    {
      path: "/connect",
      element: username ? <ConnectPage username={username}/> : <Navigate to="/login" replace />
    },
    {
      path: "/leaders",
      element: <LeaderBoard/>
    },
    {
      path: "/unsubscribe",
      element: <UnsubscribePage/>
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
