import '../App.css';

interface AchieverListProps {
  /** A very short description of the achievment (e.g. found the crystal castle) */
  achievment_description: string;

  /** A list of all names of those who achieved this achievment in
   * order of who achieved it first.
   */
  achievers: string[];

  /** Whether this is the top achievment yet achieved */
  isTop: boolean;
}

/**
 * Displays a list of scheduled events.
 */
function AchieverList({achievment_description, achievers, isTop}: AchieverListProps) {

  // If this is the top achievment, we pull out the first achiever and 
  // display them separately.

  const list_to_display = (achievers ? [...achievers] : [])
  const topAchiever = (isTop ? list_to_display.shift() : "")
  const topMessage = (!isTop ? "" : 
    (achievment_description==="beat the crystal challenge" ?
      `${topAchiever} has won the egg.` :
      `${topAchiever} has ${achievment_description}, but only one can win the egg.`))

  return (

    <div className="leaders-achievment-list">
      {achievers &&
        <div>
          {isTop && 
            <p>{topMessage}</p>
          }
          {list_to_display.length > 0 && 
            <table>
              <thead>
                <tr><td>{isTop ? "Others" : "Players"} who have {achievment_description}</td></tr>
              </thead>
              <tbody>
                {list_to_display.map((playername: string) => (
                  <tr key={playername}><td>{playername}</td></tr>
                ))}
              </tbody>
            </table>
          }
        </div>
      }
    </div>
  );
}

export default AchieverList;
