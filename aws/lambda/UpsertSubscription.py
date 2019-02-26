import boto3
import json
import random
import time

'''
Mostly just insert or update in Dynamo an email/text to be notified, but also
does some business logic.  If the Unsubscribe option is set, will delete 
instead of update and if the existing table has blacklist set, won't do anything

Copy this file into UpsertSchedule lambda
'''
def lambda_handler(event, context):
    
    try:
        print("event = " + json.dumps(event))
    
        dynamodb = boto3.resource('dynamodb', region_name='us-east-2')
        table = dynamodb.Table('global')
        
        item = {
                'PK': 'Subscription',
                'SK': event['Contact'],
                'Contact': event['Contact'],
                'Type': event['Type'],
                'OnCallOut': event['OnCallOut'],
                'OnNewSchedule': event['OnNewSchedule'],
                'Blacklist': event['Blacklist']
        }
        
        # Need to first see if this user is blacklisted
        key={
            'PK': {
                'S': item['PK']
                },
            'SK': {
                'S': item['SK']
               }
            }
        response = table.get_item(Key=key)
        blacklisted = 'Item' in response and response['Item'] and response['Item']['Blacklist']
        if blacklisted:
            return {
                'statusCode': 200,
                'body': 'Upsert request ignored'
            }

        if event['Unsubscribe']:
            # Delete the entry from the table
            response = table.delete_item(Key=key)
        else:
            response = table.put_item(Item=item)
    
        return {
            'statusCode': 200,
            'body': 'Upsert succeeded'
        }
    except Exception as e:
        message = 'Upsert failed: {}'.format(e)
        print("Hit exception.  Returning: " + message)
        return {
            'statusCode': 500,
            'body': message
        }
