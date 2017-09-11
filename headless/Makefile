all: h2hadventure

ENGINE_OBJECTS = engObj/ActionQueue.o \
  engObj/Adventure.o \
  engObj/Ball.o \
  engObj/Bat.o \
  engObj/Board.o \
  engObj/Dragon.o \
  engObj/GameObject.o \
  engObj/GameSetup.o \
  engObj/Logger.o \
  engObj/Map.o \
  engObj/Portcullis.o \
  engObj/RemoteAction.o \
  engObj/RestClient.o \
  engObj/Room.o \
  engObj/ScriptedSync.o \
  engObj/Sync.o \
  engObj/Sys.o \
  engObj/Transport.o \
  engObj/UdpSocket.o \
  engObj/UdpTransport.o \
  engObj/jsoncpp.o 

h2hadventure: AdventureView.o $(ENGINE_OBJECTS)
	g++ -g $^ -o h2hadventure

AdventureView.o: AdventureView.cpp
	g++ -g -c $^ -o $@

engObj/%.o:   ../engine/%.cpp
	g++ -g -c $^ -o $@

clean:
	rm -f h2hadventure *.o engObj/*.o

