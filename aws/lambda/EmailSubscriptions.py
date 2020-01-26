import base64
import boto3
from boto3.dynamodb.conditions import Key, Attr
import decimal
import traceback
from email.mime.text import MIMEText
from googleapiclient.discovery import build
from googleapiclient.errors import HttpError
from google_auth_oauthlib.flow import InstalledAppFlow
from google.auth.transport.requests import Request
import json
import pickle
import time

'''
Run through all subscriptions and send a message

Copy this file into EmailSubscriptions lambda
'''

SENDER_EMAIL ='ro.c.anders@gmail.com'

def lambda_handler(event, context):
  """
  Call this to send an email to all those subscribed for email notification

  :param event: of the form
    {
      'Reason': 'CallOut',
      'Subject': 'billybob wants to play h2hadventure',
      'Message': 'You have subscribed to ...'
    }
  :type event: dict[string:string]
  :param context: unused
  :type context: ???
  """

  try:
    print("Received request {}".format(json.dumps(event)))
    recipients = retrieveEmailSubscriptions(event['Reason'])
    print("Retrieved {} email subscriptions to send {} email".format(len(recipients), event['Reason']))
    creds = getCreds()
    print("Retrieved GMail credentials")
    sendEmails(creds, recipients, event['Subject'], event['Message'])
  except Exception:
    print("Encountered fatal exception")
    print(traceback.format_exc())
    return {
      'statusCode': 500,
      'body': '{}'
    }
  return {
      'statusCode': 200,
      'body': '{}'
  }

def retrieveEmailSubscriptions(reason):
  '''
  Query dynamo for the list of email addresses to email about an event
  :param reason: 'CallOut' or 'NewSchedule'
  :type reason: string
  :return: list of emails to notify about this event
  :rtype: string[]:
  '''
  dynamodb = boto3.resource('dynamodb', region_name='us-east-2')
  
  table = dynamodb.Table('global')
  attribute = 'On' + reason
  response = table.query(
      KeyConditionExpression=Key('PK').eq('Subscription')
      #QueryFilter=Attr(attribute).eq('true') + Attr('Type').eq('EMAIL')
  )
  return [sub['Contact'] for sub in response['Items'] if sub['Type']=='EMAIL' and attribute in sub and sub[attribute]]

def getCreds():
  """
  Loads the Google API client token from Dynamo.
  If it needs to be refreshed it refreshes it and saves the refreshed token
  as the latest.
  """
  creds = loadCredsFromDynamo()
  creds = refreshCreds(creds)
  return creds

def loadCredsFromDynamo():
  """
  Load the token from Dynamo or throw an error if it can't
  :return: the credentials to use with the GoogleAPI
  :rtype: some sort of Google Credentials object
  """
  key={
    'PK': 'NotificationCreds',
    'SK': 'GmailToken'
  }
  dynamodb = boto3.resource('dynamodb', region_name='us-east-2')
  table = dynamodb.Table('global')
  response = table.get_item(Key=key)

  if (
      'Item' in response and 
      response['Item'] and 
      response['Item']['Token'] 
  ):
    creds = pickle.loads(response['Item']['Token'].value)
    return creds
  else:
    print("Unexpected response from Dynamo query = " + json.dumps(response))
    raise ValueError("Could not find token in dynamo")

def refreshCreds(creds):
  """
  If the credentials need to be refreshed, refresh them, then store them
  back in the database
  :param creds: the credentials to use with the GoogleAPI
  :type param: some sort of Google Credentials object
  :return: new credentials if they needed to be refreshed or the passed in
    ones if the didn't
  :rtype: some sort of Google Credentials object
  """
  if not creds:
    raise ValueError("Cannot refresh credentials.  No credentials passed in.")
  if creds.valid:
    return creds

  if creds.expired:
    if creds.refresh_token:
      print("Refreshing GMail credentials")
      creds.refresh(Request())
      if not creds.valid:
          raise ValueError("Failed to refresh token")
    else:
      raise ValueError("Expired token cannot be refreshed.")
  else:
    # Invalid but not expired.  Give up.
    raise ValueError("Invalid token.")

  # Store the refreshed credentials in the database
  dynamodb = boto3.resource('dynamodb', region_name='us-east-2')
  table = dynamodb.Table('global')
  pickled_creds = pickle.dumps(creds)
  item = {
    'PK': 'NotificationCreds',
    'SK': 'GmailToken',
    'Token': pickled_creds
  }
  table.put_item(Item=item)

  return creds
    
def sendEmails(creds, recipients, subject, message):
    print("Creating GMail service")
    num_failures = 0
    while True:
      if num_failures > 0:
        print("Trying again...")
      try:
        service = build('gmail', 'v1', credentials=creds, cache_discovery=False)
        break
      except:
        print("Failed to create GMail service")
        num_failures += 1
        if num_failures >= 5:
            raise
        time.sleep(60)
    print("Created GMAIL service")
    for recipient in recipients:
        email = create_message(SENDER_EMAIL, recipient, subject, message)
        print("Sending GMAIL message")
        response = send_message(service, 'me', email)
        print('Sent "{}" message to {}'.format(subject, recipient))
    print('Sent "{}" message to {} recipients'.format(subject, len(recipients)))

def create_message(sender, to, subject, message_text):
  """Create a message for an email.

  Args:
    sender: Email address of the sender.
    to: Email address of the receiver.
    subject: The subject of the email message.
    message_text: The text of the email message.

  Returns:
    An object containing a base64url encoded email object.
  """
  try:
    message = MIMEText(message_text)
  except:
    print('Could nt create MIMEText from "{}"'.format(message_text))
    raise
  message['to'] = to
  message['from'] = sender
  message['subject'] = subject
  encoded = base64.urlsafe_b64encode(message.as_string().encode("utf-8")).decode("ascii")
  return {'raw': encoded}

def send_message(service, user_id, message):
  """Send an email message.

  Args:
    service: Authorized Gmail API service instance.
    user_id: User's email address. The special value "me"
    can be used to indicate the authenticated user.
    message: Message to be sent.

  Returns:
    Sent Message.
  """
  try:
    message = (service.users().messages().send(userId=user_id, body=message)
               .execute())
    print('Message Id: %s' % message['id'])
    return message
  except HttpError as error:
    print('An error occurred: %s' % error)


if __name__ == '__main__':
  event = {
    'Reason': 'CallOut',
    'Subject': 'billybob wants to play h2hadventure',
    'Message': 'You have subscribed to ...'
  }
  lambda_handler(event, None)
    
