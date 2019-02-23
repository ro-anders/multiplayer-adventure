import boto3
import json
import random
import time

'''
Update (or insert if PK/SK pair does not already exist) a scheduled game object 
in Dynamo

Copy this file into UpsertSchedule lambda
'''
def lambda_handler(event, context):
    
    try:
        print("event = " + json.dumps(event))
    
        dynamodb = boto3.resource('dynamodb', region_name='us-east-2')
        table = dynamodb.Table('global')
        
        item = {
                'PK': 'Schedule',
                'SK': event['SK'],
                'Host': event['Host'],
                'Time': event['Time'],
                'Duration': event['Duration'],
                'Others': event['Others'],
                'Comments': event['Comments']
        }
        if not item['SK']:
            # Generate a sort key for this new scheduled game
            # Mostly sort on start time but throw some things on the end for uniqueness
            item['SK'] = "{}-{}-{}".format(starttime, int(time.time()), random.randint(0, 999))
        if not item['Host']:
            del item['Host']
        if not item['Others']:
            del item['Others']
        if not item['Comments']:
            del item['Comments']
        
        print("inserting " + json.dumps(item))
        response = table.put_item(Item=item)
    
        return {
            'statusCode': 200,
            'body': 'PutItem succeeded:' + json.dumps(response)
        }
    except Exception as e:
        message = 'PutItem failed: {}'.format(e)
        print("Hit exception.  Returning: " + message)
        return {
            'statusCode': 500,
            'body': message
        }
