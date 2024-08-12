import express from 'express';
import cors from 'cors';

const app = express();
app.use(cors());

app.get('/', (req, res) => {
    res.send('This is a test web page!');
})

app.get("/api/game", (req, res) => {
  console.log("Received request for games")
  res.send({"games": [1, 2]})
})



app.listen(3000, () => {
    console.log('The application is listening on port 3000!');
})
