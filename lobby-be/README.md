The lobby backend is a lambda application routed by API Gateway with a DynamoDB database.

The entire backend can be run locally.  It was setup following this sample project https://github.com/aws-samples/aws-sam-java-rest.


To run the dynamodb locally on port 8000, run 
  docker run -p 8000:8000 amazon/dynamodb-local
To run the API Gateway/Lambdas locally on port 3000 run
  sam local start-api
To hit the API run:
  curl http://localhost:3000/