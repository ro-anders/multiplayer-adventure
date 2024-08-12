To Build (this is all outdated)
Download AWS Mobile SDK for Unit
Assets -> Import Package -> Custom Package -> AWSSDK.Lambda.3.3.108.4.unitypackage
Asset Store -> My Assets -> Dissonance Voice Chat -> Download then Import
In popup select UNet HLAPI
Download and Install Dissonance HLAPI

To Run the Whole Suite Locally:
1. Run the game-be backend game server by running 
  - cd game-be
  - docker build --platform linux/amd64 . -t roanders/h2hadv-server
  - docker run -p 4000:4000 roanders/h2hadv-server
  This mimics the fargate task started at game time
2. Run a DynamoDB locally with `docker run -p 8000:8000 amazon/dynamodb-local`
2. cd to lobby-be and run "npx ts-node src/index.ts" to run the Lobby Backend Express server
   which mimics the APIGateway & Lambda
3. Run the webserver serving the game 
  - open the Unity project and selecting File --> Build and Run
  - cd multiplayer-adventure/H2HAdventure/target
  - docker run -it --rm -p 8080:80 --name web -v ./H2HAdventure2P:/usr/share/nginx/html nginx
5. cd to lobby-fe and run npm start to run the Lobby Frontend React website
   which would normally be hosted on a webserver or S3 bucket

To Deploy and Run the System:
1. Authenticate with AWS, credentials stored in ~/.aws/credentials
  - export AWS_PROFILE=h2hadventure
  - export AWS_REGION=us-east-2
1. Build the Game Back-End Server
  - just commit to Github and Github action will deploy the latest to Docker.io
3. Launch game-be in a fargate service by standing up deploy/fargateservice.cfn.yml
  - aws cloudformation create-stack --stack-name game-be --template-body file://game-be/deploy/fargateservice.cfn.yml --capabilities "CAPABILITY_NAMED_IAM"
4. Build game package
 - Unity File->Build and Run
6. Deploy game package
 - aws cloudformation create-stack --stack-name s3-website  --template-body file://deploy/s3website.cfn.yml
 - aws s3 cp --recursive H2HAdventure/target/H2HAdventure2P s3://h2adventure-website/game/H2HAdventureMP
7. Build lobby-fe
 - cd lobby-fe
 - npm run build
8. Deploy lobby-fe
 - aws s3 cp --recursive build/ s3://h2adventure-website/
9. Play game
 - goto http://h2adventure-website.s3-website.us-east-2.amazonaws.com 
10. Get IP
 - Get the ENI of the Fargate task by copying the task ARN into the following command
     aws ecs describe-tasks --cluster h2hadv-serverCluster --task <<task-arn>> | jq -r -e '.tasks[0].attachments[0].details[] | select(.name=="networkInterfaceId").value'
 - Get the Public IP address by copying the ENI into the following command
     aws ec2 describe-network-interfaces --network-interface-ids <<eni>> | jq -r -e ".NetworkInterfaces[0].Association.PublicIp"
 - Enter IP in the web page