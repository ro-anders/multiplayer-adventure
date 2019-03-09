
from __future__ import print_function
import base64
import boto3
from email.mime.text import MIMEText
from googleapiclient.discovery import build
from googleapiclient.errors import HttpError
from google_auth_oauthlib.flow import InstalledAppFlow
from google.auth.transport.requests import Request
import json
import os.path
import pickle
import sys

"""
This is a test program, not a lambda.  Run it if you need to regenerate a 
auth token or manually test emails.
"""

def getCreds(location):
  """
  Will attempt to load the credentials from disk or database.
  If it cannot load them or cannot renew loaded credentials will
  launch a browser to generate new credentials.
  Will save new or renewed credentials.

  :param location: where to look for the token.  one of 'db' 'file' or 'either'
  :type location: str
  :return: the credentials
  :rtype: byte[]
  """

  creds = None
  if location == 'db' or location == 'either':
    creds = getFromDynamo()
    if location == 'either' and creds:
      location = 'db'
  if not creds:
    if location == 'file' or location == 'either':
      creds = getFromDynamo()
        if location == 'either' and creds:
          location = 'file'
  token_changed = False

  # If there are no (valid) credentials available, let the user log in.
  if not creds or not creds.valid:
      if creds and creds.expired:
          print("Token has expired.  Attempting to refresh token")
      if creds and creds.expired and creds.refresh_token:
          creds.refresh(Request())
          if not creds.valid:
            print("Failed to refresh token")
          else:
            token_changed = True
      else:
          if not creds:
            print("No token found.  Generating one.")
          elif not creds.refresh_token:
            print("Expired token cannot be refreshed.  Generating new one.")
          else:
            print("Invalid token.  Generating new one.")
          # flow = InstalledAppFlow.from_client_secrets_file(
          #     'credentials.json', SCOPES)
          # creds = flow.run_local_server()
          # token_changed = True

  if not creds or not creds.valid:
    raise ValueError("Could not create valid creedentials")

  if token_changed:
    if location == 'file':
      storeInFile(creds)
    else:
      storeInDynamo(creds)

  return creds

def getFromFile():
  """
  Load the token from the file token.pickle in the current directory
  """
  creds = None
  if os.path.exists('token.pickle'):
    with open('token.pickle', 'rb') as token:
        creds = pickle.load(token)
  else:
    print("token.pickle does not exist")
  return creds

def storeInFile(creds):
  with open('token.pickle', 'wb') as token:
      pickle.dump(creds, token)

def getFromDynamo():
        key={
          'PK': 'NotificationCreds',
          'SK': 'GmailTokens'
        }
        dynamodb = boto3.resource('dynamodb', region_name='us-east-2')
        table = dynamodb.Table('global')
        try:
          response = table.get_item(Key=key)
        except:
          print("get_item raised exception")
          raise

        if (
            'Item' in response and 
            response['Item'] and 
            response['Item']['Token'] 
        ):
          creds = pickle.loads(response['Item']['Token'].value)
          return creds
        else:
          print("Could not find token in dynamo")
          return None

def storeInDynamo(creds):
  print("Storing new token in dynamo")
  dynamodb = boto3.resource('dynamodb', region_name='us-east-2')
  table = dynamodb.Table('global')
  pickled_creds = pickle.dumps(creds)
  print("Pickled a " + str(type(creds)) + " into a " + str(type(pickled_creds)))
  item = {
    'PK': 'NotificationCreds',
    'SK': 'GmailToken',
    'Token': pickled_creds
  }
  response = table.put_item(Item=item)
  print("put_item response:" + json.dumps(response))


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
  message = MIMEText(message_text)
  message['to'] = to
  message['from'] = sender
  message['subject'] = subject
  return {'raw': base64.urlsafe_b64encode(message.as_string())}

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


# If modifying these scopes, need to regenerate the token
SCOPES = ['https://www.googleapis.com/auth/gmail.readonly',
          'https://www.googleapis.com/auth/gmail.compose']



def printLabels(service):
  """
  Queries Gmail and prints labels
  Used just to test that the creds are good

  :param service: a Gmail service 
  :type service: ??? 
  """

  print('Calling Gmail API to list labels')
  results = service.users().labels().list(userId='me').execute()
  labels = results.get('labels', [])

  if not labels:
      print('No labels found.')
  else:
      print('Labels:')
      for label in labels:
          print(label['name'])

if __name__ == '__main__':
    if len(sys.argv)<=1:
      print("emailUtil.py token file|db - to regenerate token")
      print(" or ")
      print("emailUtil.py test FROM TO - to send an email")
      exit(1)
    if (sys.argv[1] == "test"):
      if (len(sys.argv)<4):
        print("You need to specify FROM and TO addresses on command line")
        exit(1)
      fromAddr = sys.argv[2]
      toAddr = sys.argv[3]
      creds = getCreds("either")
      service = build('gmail', 'v1', credentials=creds)
      message = create_message(fromAddr, toAddr, "Testing H2HAdventure Notify", "This works!")
      response = send_message(service, 'me', message)
      print("Sent message.  Response = " + json.dumps(response))

    elif  (sys.argv[1] == "token"):
      if len(sys.argv)<3 or sys.argv[2] not in ["file", "db"]:
        print('You need to specify either "file" or "db" on command line')
        exit(1)
      creds = getCreds(sys.argv[2])
      service = build('gmail', 'v1', credentials=creds)
      # Do a query of labels to test
      printLabels(service)

