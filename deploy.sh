#!/bin/bash

export AWS_PROFILE=h2hadventure
export AWS_REGION=us-east-2

# Determine environment and setup environment variables
if [ $# != 1 ]; then
	echo "\"deploy.sh prod\" or \"deploy.sh test\""
	exit 1
fi
env=$1
if [ "$env" != "prod" ] && [ "$env" != "test" ]; then
	echo "\"deploy.sh prod\" or \"deploy.sh test\""
	exit 1
fi
if [ "$env" == "prod" ]; then
	env_suffix=""
else
	env_suffix="-$env"
fi


# Build website stack.
if [ "$env" == "prod" ]; then
	bucket_name="play.h2hadventure.com"
else
	bucket_name="test.h2hadventure.com"
fi
echo "Building website ${bucket_name}"
stackname=s3-website${env_suffix}
aws cloudformation deploy --stack-name s3-website-${env} \
   --no-fail-on-empty-changeset \
   --template-file ./lobby-fe/deploy/s3website.cfn.yml \
   --parameter-overrides Environment=${env}

# Copy the game files to the website bucket
aws s3 cp --recursive H2HAdventure/target s3://${bucket_name}/game

# Build lobby backend
echo "Building ${env} lobby back end"
# We need to know if the API Gateway ID has changed
start_api_id=`aws cloudformation describe-stacks \
  --stack-name lobby-be-${env} \
  --query "Stacks[0].Outputs[?OutputKey=='WebEndpoint'].OutputValue" \
  --output text 2> /dev/null`

pushd lobby-be
sam build
sam deploy --config-env ${env}
end_api_id=`aws cloudformation describe-stacks \
  --stack-name lobby-be-${env} \
  --query "Stacks[0].Outputs[?OutputKey=='WebEndpoint'].OutputValue" \
  --output text 2> /dev/null`

if [ "$start_api_id" != "$end_api_id" ]; then
	echo
	echo "API GATEWAY ID HAS CHANGED.  New ID = ${end_api_id}"
	echo "ID must be changed in lobby-fe/.env.${env}"
	read -p "Press [Enter] to continue..."
fi
popd

# Build lobby front end
echo "Building ${env} front end"
pushd lobby-fe
npm run build
aws s3 cp --recursive build/ s3://${bucket_name}/
popd

# Build game back end
echo "Building game backend"
aws cloudformation deploy --stack-name game-be-${env} \
   --no-fail-on-empty-changeset \
   --template-file game-be/deploy/fargateservice.cfn.yml \
   --capabilities "CAPABILITY_NAMED_IAM" \
   --parameter-overrides Environment=${env}








