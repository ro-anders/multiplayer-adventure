using System.Collections;
using System.Collections.Generic;
using GameScene;
using TMPro;
using UnityEngine;
using System.Linq;
using GameEngine;

/// <summary>
/// This class handles chatting in game.
/// It is a Unity Panel that contains a input field to enter a chat message,
/// a button to post the chat message, and a text field to display all 
/// chat messages.
/// </summary>
public class ChatController : MonoBehaviour
{
    // The text field to display all chat messages 
    public TMP_Text message_display;

    // The input field to enter a text message
    public TMP_InputField message_input;

    // The transport between games.
    public WebSocketTransport xport;

    // The number of chats displayed.
    private int numChats = 0;

    // The maximum number of chats to display on the screen.
    const int MAX_CHATS = 100;

    // Put the players name in the color of their castle
    private static string[] PLAYER_COLORS = new string[]{"#FFD84C", "#CF4D0C", "#00A86B"};

    // On startup clear the dummy text from the text chat display
    void Start() {
        message_display.text = "<color=#FFFFFF>Type below to send chat messages</color>";
    }

    // Every frame see if there are new chat messages to display
    void Update()
    {
        // Put any new messages in the chat window
        ChatMessage nextChat = xport.getChat();
        while (nextChat != null) {
            GameEngine.Logger.Debug("Display chat from player #" + nextChat.slot);
            displayChat(nextChat);
            nextChat = xport.getChat();
        }
    }

    // Post a chat message in the display, applying rich text markup.
    // If there are too many chat messages in the display, truncate them.
    private void displayChat(ChatMessage chat) {
        GameEngine.Logger.Debug("Chat from player #" + chat.slot + ": " + chat.message);
        // If there are too many lines, truncate it by 20%
        if (numChats > MAX_CHATS) {
            int lines_to_truncate = (numChats-MAX_CHATS) + MAX_CHATS/5;
            string[] lines = message_display.text.Split(new[] { '\n' });
            message_display.text = string.Join('\n', lines.Skip(lines_to_truncate));
            numChats -= lines_to_truncate;
        }
        string colorCode = PLAYER_COLORS[chat.slot];
        string playerName = xport.GameInfo.player_names[chat.slot];
        string new_text = "\n<color="+colorCode+">"+playerName+":</color> "+chat.message;
        message_display.text += new_text;
        numChats += 1;
        GameEngine.Logger.Debug("Chat window:\n" + message_display.text);
    }

    // Event handler for pressing the post button (or hitting enter inside the
    // input field).  Display the input text in the chat display, send the chat
    // message to everyone else and clear the input field.
    public void HandlePostButtonPressed() {
        if (message_input.text.Trim().Length > 0) {
            ChatMessage chat = new ChatMessage {slot = xport.ThisPlayerSlot, message = message_input.text.Trim()};
            message_input.text = "";
            xport.sendChat(chat.message);
            displayChat(chat);
        }
    }

    // Handle a key pressed in the input field - mostly to look for enter being pressed
    public void OnTextChange()
    {
        if (message_input.text.Contains('\n'))
        {
            message_input.text = message_input.text.Replace("\n", "");
            HandlePostButtonPressed();
        }
    }

}
