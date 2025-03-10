import { useNavigate } from "react-router-dom";

/**
 * Displays logo and link to home page on top left, and logged in username on top right
 */
function TitleBar() {
    const navigate = useNavigate()
    return (
  
      <div className="titlebar">
        <div className="titlebar-home" onClick={()=>navigate("/")}>
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 22" width="40" height="40">
                <path d="M10 3L2 9h3v7h4V10h2v6h4V9h3L10 3z"/>
            </svg>
        </div>
        <div className="titlebar-user">
            H2H Atari Adventure
        </div>
      </div>
    );
  }
  
  export default TitleBar;
  