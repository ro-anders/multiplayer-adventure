using System;
namespace GameEngine
{
    public interface Transport
    {
        void send(RemoteAction action);

        RemoteAction get();
    }
}
