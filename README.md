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
  This mimics the APIGateway & Lambda
3. Run the game-be backend game server (port 4000) 
  - `docker build --platform linux/amd64 . -t roanders/h2hadv-server`
  - `docker run -p 4000:4000 -e NODE_ENV=development --network=host roanders/h2hadv-server`
  This mimics the fargate task started at game time
4. Run the webserver serving the game (port 8080)
  - open the Unity project and selecting File --> Build and Run
  - cd multiplayer-adventure/H2HAdventure/target
  - docker run -it --rm -p 8080:80 --name web -v ./H2HAdventure2P:/usr/share/nginx/html nginx
5. Run the Lobby Frontend webapp (port 5000)
  - npm start
  This mimics a webapp hosted on a webserver or S3 bucket

To Deploy and Run the System:
1. Authenticate with AWS, credentials stored in ~/.aws/credentials
  - export AWS_PROFILE=h2hadventure
  - export AWS_REGION=us-east-2
2. Build game package
 - Unity File->Build and Run
3. Deploy game package
 - aws cloudformation update-stack --stack-name s3-website  --template-body file://lobby-fe/deploy/s3website.cfn.yml
 - aws s3 cp --recursive H2HAdventure/target/H2HAdventure2P s3://h2adventure-website/game/H2HAdventureMP
4. Build and deploy lobby-be
 - cd lobby-be
 - sam build
 - sam deploy
5. Build lobby-fe
 - cd lobby-fe
 - npm run build
6. Deploy lobby-fe
 - aws s3 cp --recursive build/ s3://h2adventure-website/
7. Build the Game Back-End Server
  - commit to Github and Github action will deploy the latest to Docker.io
  - Define the backend server by standing up deploy/fargateservice.cfn.yml
  - aws cloudformation update-stack --stack-name game-be \
   --template-body file://game-be/deploy/fargateservice.cfn.yml --capabilities "CAPABILITY_NAMED_IAM"
8. Play game
 - goto http://h2adventure-website.s3-website.us-east-2.amazonaws.com 
9. Until H2HAdventure knows how to launch it's own Fargate task, when ready to play a game manually start the task.
  - aws ecs run-task \
   --cluster h2hadv-serverCluster \ 
   --task-definition arn:aws:ecs:us-east-2:637423607158:task-definition/h2hadv-serverTaskDefinition \
   --launch-type FARGATE \
   --network-configuration "awsvpcConfiguration={subnets=[subnet-0d46ce42b6ae7a1ee,subnet-011083badbc3f216e],securityGroups=[sg-07539077994dfb96c],assignPublicIp=ENABLED}" \
   --overrides '{ "containerOverrides": [ { "name": "h2hadv-server", "environment": [ { "name": "LOBBY_URL", "value": "https://z2rtswo351.execute-api.us-east-2.amazonaws.com/Prod" } ] } ] }'

