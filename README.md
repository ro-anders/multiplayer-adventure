To Build (this is all outdated)
Download AWS Mobile SDK for Unit
Assets -> Import Package -> Custom Package -> AWSSDK.Lambda.3.3.108.4.unitypackage
Asset Store -> My Assets -> Dissonance Voice Chat -> Download then Import
In popup select UNet HLAPI
Download and Install Dissonance HLAPI

To Run the Whole Suite Locally:
1. Run the game-be backend game server by running 
  - docker build --platform linux/amd64 . -t roanders/h2hadv-server
  - docker run -p 4080:3000 roanders/h2hadv-server
  This mimics the fargate task started at game time
2. cd to lobby-be and run "npx ts-node src/index.ts" to run the Lobby Backend Express server
   which mimics the APIGateway & Lambda
3. Run the webserver serving the game by opening the Unity project and selecting File --> Build and Run
   (take note of the port on the web page that pops up, but you may close the tab)
4. <<instructions for setting port>>
5. cd to lobby-fe and run npm start to run the Lobby Frontend React website
   which would normally be hosted on a webserver or S3 bucket

