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

To Deploy and Run the System:
1. Login to DockerHub as roanders
  - delete /Library/Application Support/com.docker.docker/registry.json
  - set default browser to Opera
  - docker login -u roanders
2. Upload game-be to docker
  - cd game-be
  - docker build --platform linux/amd64 . -t roanders/h2hadv-server
  - docker tag roanders/h2hadv-server roanders/h2hadv-server
  - docker push roanders/h2hadv-server
3. Launch game-be in a fargate service by standing up deploy/fargateservice.cfn.yml
  - clokta-sandbox
  - export AWS_PROFILE=sandbox
  - aws cloudformation ... keep working on this
4. Configure game-be location
 - Edit multiplayer/H2HAdventure/Assets/Scripts/GameScene/WebSocketTransport.cs
 - Line 20, set HOST_ADDRESS to ws://rantonucci-fargate-test.sandbox.aws.arc.pub:3000
5. Create game package
 - Unity File->Build and Run
1. Until the lobby-be has the ability to spawn a Fargate cluster, 

