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

  // Username is state, but we persist it across sessions in local storage
  let [username, setUsername] = useState<string>(getInitialUsername());
  function getInitialUsername(): string {
      return localStorage.getItem( 'h2h.username' ) || "";    
  }
  function setNewUsername(new_username: string) {
    if (new_username) {
      localStorage.setItem("h2h.username", new_username);
    }
    setUsername(new_username);
  }

  // Experience level is also state persisted in local storage
  let [experienceLevel, setExperienceLevel] = useState<number>(getInitialExperienceLevel());
  function getInitialExperienceLevel(): number {
    const exp_level_str: string = localStorage.getItem( 'h2h.experience_level' ) || "0"; 
    return parseInt(exp_level_str)   
  }
  function setNewExperienceLevel(new_exp_level: number) {
    localStorage.setItem("h2h.experience_level", new_exp_level.toString());
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


  let loginPage = <LoginPage username={username} setUsername={setNewUsername} experienceLevel={experienceLevel} setExperienceLevel={setExperienceLevel}/>
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
