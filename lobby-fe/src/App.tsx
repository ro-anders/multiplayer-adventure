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
  const router = createBrowserRouter([
    {
      path: "/",
      element: <LoginPage/>,
    },
    {
      path: "/lobby",
      element: <LobbyPage/>
    }
  ]);
  return (

    <div className="App">
      <header className="App-header">
        <img src={logo} className="App-logo" alt="logo" />
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
      </header>
      <RouterProvider router={router} />
    </div>
  );
}

export default App;
