import React, { useEffect, useState } from 'react';
import { Accordion, Form, Button } from 'react-bootstrap';
import { useNavigate } from "react-router-dom";

import '../App.css';
import '../css/Login.css'

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

    <div className="login-page">
      <header className="login-header">
        Head-to-Head Atari Adventure
      </header>
      <img 
        src="castle.png" 
        alt="Gold Castle" 
        className="login-main-image" 
      />
      <Accordion className="login-accordion-section">
        <Accordion.Item eventKey="0">
          <Accordion.Header>Play Against Others</Accordion.Header>
          <Accordion.Body>
            <Form>
              <Form.Group controlId="screenName">
                <Form.Label>Screen name</Form.Label>
                <Form.Control type="text" placeholder="Acererak" onChange={(value) => setNewUsername(value.target.value)}/>
              </Form.Group>
              <Form.Group>
                <Form.Check 
                  type="switch" 
                  label="This is my first time" 
                  name="experience" 
                  id="firstTime" 
                />
                <Form.Check 
                  type="switch" 
                  label="I need help with the maps" 
                  name="experience" 
                  id="helpWithMaps" 
                />
              </Form.Group>
              <Button variant="primary" type="submit" onClick={loginClicked}>Go</Button>
            </Form>
          </Accordion.Body>
        </Accordion.Item>
        
        <Accordion.Item eventKey="1">
          <Accordion.Header>Play Against the Computer</Accordion.Header>
        </Accordion.Item>
        
        <Accordion.Item eventKey="2">
          <Accordion.Header>Find Other Players</Accordion.Header>
        </Accordion.Item>
        
        <Accordion.Item eventKey="3">
          <Accordion.Header>More Info</Accordion.Header>
        </Accordion.Item>
      </Accordion>
    </div>
  );
}

export default LoginPage;
