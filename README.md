To Build (this is all outdated)
Download AWS Mobile SDK for Unit
Assets -> Import Package -> Custom Package -> AWSSDK.Lambda.3.3.108.4.unitypackage
Asset Store -> My Assets -> Dissonance Voice Chat -> Download then Import
In popup select UNet HLAPI
Download and Install Dissonance HLAPI

To Run the Whole Suite Locally:
1. Run the game-be backend game server (port 4000) 
  - `docker build --platform linux/amd64 . -t roanders/h2hadv-server`
  - `docker run -p 4000:4000 roanders/h2hadv-server`
  This mimics the fargate task started at game time
2. Run a DynamoDB locally (port 8000)
  - `docker run -p 8000:8000 amazon/dynamodb-local`
3. Run the Lobby Backend lambdas using SAM (port 3000)
  - `sam local start-api`
  This mimics the APIGateway & Lambda
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
2. Build the Game Back-End Server
  - just commit to Github and Github action will deploy the latest to Docker.io
3. Launch game-be in a fargate service by standing up deploy/fargateservice.cfn.yml and running a task
  - aws cloudformation create-stack --stack-name game-be \
   --template-body file://game-be/deploy/fargateservice.cfn.yml --capabilities "CAPABILITY_NAMED_IAM"
  - aws ecs run-task \
   --cluster h2hadv-serverCluster \
   --task-definition arn:aws:ecs:us-east-2:637423607158:task-definition/h2hadv-serverTaskDefinition:7 \
   --launch-type FARGATE \
   --network-configuration "awsvpcConfiguration={subnets=[subnet-0d46ce42b6ae7a1ee,subnet-011083badbc3f216e],securityGroups=[sg-07539077994dfb96c],assignPublicIp=ENABLED}"
4. Build game package
 - Unity File->Build and Run
5. Deploy game package
 - aws cloudformation create-stack --stack-name s3-website  --template-body file://deploy/s3website.cfn.yml
 - aws s3 cp --recursive H2HAdventure/target/H2HAdventure2P s3://h2adventure-website/game/H2HAdventureMP
6. Build and deploy lobby-be
 - cd lobby-be
 - sam build
 - sam deploy
7. Build lobby-fe
 - cd lobby-fe
 - npm run build
8. Deploy lobby-fe
 - aws s3 cp --recursive build/ s3://h2adventure-website/
9. Post IP by running the following commands
 - TASKARN=$(aws ecs list-tasks --cluster h2hadv-serverCluster | jq -re ".taskArns[0]")
 - ENI=$(aws ecs describe-tasks --cluster h2hadv-serverCluster --task $TASKARN | jq -r -e '.tasks[0].attachments[0].details[] | select(.name=="networkInterfaceId").value')
 - IP=$(aws ec2 describe-network-interfaces --network-interface-ids $ENI | jq -r -e ".NetworkInterfaces[0].Association.PublicIp")
 - curl -d "{\"name\": \"game_server_ip\", \"value\": \"$IP\"}" -H "Content-Type: application/json" -X PUT https://z2rtswo351.execute-api.us-east-2.amazonaws.com/Prod/setting/game_server_ip
10. Play game
 - goto http://h2adventure-website.s3-website.us-east-2.amazonaws.com 
