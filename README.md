To Build (this is all outdated)
Download AWS Mobile SDK for Unit
Assets -> Import Package -> Custom Package -> AWSSDK.Lambda.3.3.108.4.unitypackage
Asset Store -> My Assets -> Dissonance Voice Chat -> Download then Import
In popup select UNet HLAPI
Download and Install Dissonance HLAPI

To Run the Whole Suite Locally:
1. Run a DynamoDB locally (port 8000)
  - `docker run -p 8000:8000 amazon/dynamodb-local`
2. Run the Lobby Backend lambdas using SAM (port 3000)
  - `sam local start-api`
  This mimics the APIGateway & Lambda.
  If you want to actually send emails, you need to specify the gmail password with
  - `sam local start-api --parameter-overrides EnvironmentType=Dev GMailPassword=<<password>>`
  You can get the password from `lobby-be/.password` but never check that file into Github.
3. Run the game-be backend game server (port 4000) 
  - `docker build --platform linux/amd64 . -t roanders/h2hadv-server`
  - `docker run -p 4000:4000 -e NODE_ENV=development --network=host roanders/h2hadv-server`
  This mimics the fargate task started at game time
4. Run the webserver serving the game (port 8080)
  - open the Unity project and selecting File --> Build and Run
  - cd multiplayer-adventure/H2HAdventure/target
  - docker run -it --rm -p 8080:80 --name web -v ./H2HAdventureMP:/usr/share/nginx/html nginx
5. Run the Lobby Frontend webapp (port 5000)
  - npm start
  This mimics a webapp hosted on a webserver or S3 bucket

To Deploy and Run the System:
1. Authenticate with AWS, credentials stored in ~/.aws/credentials
  - export AWS_PROFILE=h2hadventure
  - export AWS_REGION=us-east-2
2. Build SinglePlayer Unity game package
 - rm -rf H2HAdventure/target/*
 - Open Unity
 - Unity File->Build Settings...
 - Select Scenes/SinglePlayerScreen and unselect others
 - Uncheck "Development Build"
 - Select "Runtime Speed" for "Code Optimization"
 - Click "Build"
 - Enter "H2HAdventure1P"
 - Click Save and then click Replace
3. Build MultiPlayer Unity game package
 - Select Scenes/ProtoMPlayer and unselect others
 - Click "Build"
 - Enter "H2HAdventureMP"
 - Click Save and then click Replace
4. Deploy Unity game packages
 - aws cloudformation deploy --stack-name s3-website-prod|test  --template-file lobby-fe/deploy/s3website.cfn.yml --parameter-overrides Environment=prod|test
 - aws s3 cp --recursive H2HAdventure/target s3://test.h2hadventure.com/game
5. Build and deploy lobby-be
 - cd lobby-be
 - sam build
 - sam deploy
6. Build and deploy lobby-fe
 - cd lobby-fe
 - if you rebuilt the backend from scratch, put the new API gateway URL (e.g. https://xx11yyyy22.execute-api.us-east-2.amazonaws.com/Prod) in lobby-fe/.env.prod|test
 - npm run build
 - aws s3 cp --recursive build/ s3://test.h2hadventure.com/
7. Build the Game Back-End Server
  - commit to Github and Github action will deploy the latest to Docker.io
8. Define the game server by standing up deploy/fargateservice.cfn.yml
  - aws cloudformation deploy --stack-name game-be-prod|test \
   --template-file game-be/deploy/fargateservice.cfn.yml --capabilities "CAPABILITY_NAMED_IAM" --parameter-overrides Environment=prod|test
9. Play game
 - goto https://play.h2hadventure.com