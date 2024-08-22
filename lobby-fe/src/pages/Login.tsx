import React, { useEffect, useState } from 'react';
import Button from 'react-bootstrap/Button';
import Form from 'react-bootstrap/Form';
import { useNavigate } from "react-router-dom";

import '../App.css';

interface LoginProps {
  /** The name of the currently logged in user */
  username: string;
  /** The callback to call to change the user name */
  setUsername: (_: string)=>void;
}

function LoginPage({username, setUsername}: LoginProps) {

  const navigate = useNavigate()
  let [newUsername, setNewUsername] = useState<string>("");

  /**
   * set the username and forward to the lobby
   * @param formText the text in the form
   */
  function loginClicked() {
    if (!!newUsername.trim()) {
      setUsername(newUsername.trim());
      navigate("/lobby");
    }
  }

  return (

    <div className="App">
      <header className="App-header">
        <p>
          Login Page
        </p>
        <Form.Label>Screen name</Form.Label>
        <Form.Control type="text" placeholder="Acererak" onChange={(value) => setNewUsername(value.target.value)}/>
        <Button onClick={loginClicked}>Enter</Button>
      </header>
    </div>
  );
}

export default LoginPage;
