import Cookies from 'js-cookie';
import React, { useEffect, useState } from 'react';
import { Form, Button } from 'react-bootstrap';
import { useNavigate } from "react-router-dom";

import '../App.css';
import '../css/Login.css'

const USERNAME_COOKIE = 'h2hadventure.username';
const EXPERIENCE_LEVEL_COOKIE = 'h2hadventure.experience';

interface LoginProps {
  /** The name of the currently logged in user */
  username: string;
  /** The callback to call to change the user name */
  setUsername: (_: string)=>void;
  /** How much help they need.  3=no help, 2=map guides, 1=map guides & popup hints */
  experienceLevel: number;
  /** The callback to call to change the experience level */
  setExperienceLevel: (_: number)=>void;
}

function LoginPage({username, setUsername, experienceLevel, setExperienceLevel}: LoginProps) {

  const navigate = useNavigate()

  /** What the user has typed into the form for username. 
   * We don't actually set the App username until the user takes an action. */
  let [formUsername, setFormUsername] = useState<string>(username);
  let [warning, setWarning] = useState<string>("");

  /* We store the last used name and experience in a cookie.  Try to load it. */
  useEffect(() => {
    if (!!!formUsername) {
      const lastUsername = Cookies.get(USERNAME_COOKIE);
      if (lastUsername) {
        setFormUsername(lastUsername);
      }
    }
    if (!!!experienceLevel) {
      const cookieStr = Cookies.get(EXPERIENCE_LEVEL_COOKIE)
      const lastExperienceLevel = (cookieStr ? parseInt(cookieStr) : 0) 
      if (lastExperienceLevel) {
        setExperienceLevel(lastExperienceLevel);
      }
      else {
        setExperienceLevel(3);
      }
    }
  }, [experienceLevel, formUsername, setExperienceLevel]);  

  function handlePlayOthers() {
    if (!formUsername) {
      setWarning("Please enter a name")
    }
    else {
      Cookies.set(USERNAME_COOKIE, formUsername)
      Cookies.set(EXPERIENCE_LEVEL_COOKIE, experienceLevel.toString())
      setUsername(formUsername)
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
        {warning && <p className='login-error-message'>{warning}</p>}
        <Form className="login-form">
        <Form.Label className="login-form-label" >name</Form.Label>
        <Form.Control className="login-form-field" type="text" placeholder="Acererak" value={formUsername} onChange={(event) => setFormUsername(event.target.value)}/>
        <Form.Group className='login-form-experience'>
          <Form.Check 
            type="radio" 
            name="experience" 
            id="experienced" 
            label="I don't need help" 
            value="3"
            checked={experienceLevel === 3}
            onChange={(event) => setExperienceLevel(parseInt(event.target.value))}    
          />
          <Form.Check 
            type="radio" 
            name="experience" 
            id="helpWithMaps" 
            label="I need help with the maps" 
            value="2"
            checked={experienceLevel === 2}
            onChange={(event) => setExperienceLevel(parseInt(event.target.value))}    
          />
          <Form.Check 
            type="radio" 
            name="experience" 
            id="firstTime" 
            label="Help, this is my first time!" 
            value="1"
            checked={experienceLevel === 1}
            onChange={(event) => setExperienceLevel(parseInt(event.target.value))}    
          />
        </Form.Group>
      </Form>
      <Button onClick={handlePlayOthers}>Play Against Others</Button>
      <Button>Play Against the Computer</Button>
      <Button>Find Other Players</Button>
      <Button>More Info</Button>
    </div>
  );
}

export default LoginPage;
