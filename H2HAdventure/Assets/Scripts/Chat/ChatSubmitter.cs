using System;

public interface ChatSubmitter
{
    void PostChat(string text);

    void AnnounceVoiceEnabledByHost();

    ChatPanelController GetChatPanelController();
}
