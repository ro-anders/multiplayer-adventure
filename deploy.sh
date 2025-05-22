#!/bin/bash

export AWS_PROFILE=h2hadventure
export AWS_REGION=us-east-2

# Determine environment and setup environment variables
if [ $# != 1 ]; then
	echo "\"deploy.sh prod\" or \"deploy.sh stage\""
	exit 1
fi
env=$1
if [ "$env" != "prod" ] && [ "$env" != "stage" ]; then
	echo "\"deploy.sh prod\" or \"deploy.sh stage\""
	exit 1
fi
if [ "$env" == "prod" ]; then
	env_suffix=""
else
	env_suffix="-$env"
fi


# Build website stack.
stackname=s3-website${env_suffix}
aws cloudformation deploy --stack-name s3-website${env_suffix} \
   --no-fail-on-empty-changeset \
   --template-file ./lobby-fe/deploy/s3website.cfn.yml \
   --parameter-overrides Environment=${env}

# Copy the game files to the website bucket
aws s3 cp --recursive H2HAdventure/target s3://prerelease1414${env_suffix}.h2hadventure.com/game

