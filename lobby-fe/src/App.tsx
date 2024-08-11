import React, { useEffect, useState } from 'react';
import {
  createBrowserRouter,
  RouterProvider,
} from "react-router-dom";
import logo from './logo.svg';
import './App.css';
import LoginPage from "./pages/Login";
import LobbyPage from "./pages/Lobby";




function App() {

  let [username, setUsername] = useState<string>("");

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
        <p>
          Root
        </p>
        <a
          className="App-link"
          href="https://reactjs.org"
          target="_blank"
          rel="noopener noreferrer"
        >
          React.js
        </a>
        <RouterProvider router={router} />
        </header>
    </div>
  );
}

export default App;
