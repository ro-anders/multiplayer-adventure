using System;

public interface ChatSubmitter
{
    void PostChat(string text);

    ChatPanelController GetChatPanelController();
}
