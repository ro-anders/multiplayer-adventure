#!/bin/bash -ex

rm -rf EmailSubscriptions
mkdir EmailSubscriptions
cd EmailSubscriptions
for package in $(cat ../EmailSubscriptions.reqs.txt); do
	echo Installing $package
	pip install $package --target .
done
cp ../EmailSubscriptions.py .
rm ../EmailSubscriptions.zip
zip -r9 ../EmailSubscriptions.zip .
cd ..
aws --region us-east-2 lambda update-function-code --function-name EmailSubscriptions --zip-file fileb://EmailSubscriptions.zip