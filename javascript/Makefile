all: adventure.html

ENGINE_OBJECTS = engObj/Room.o \
  engObj/ActionQueue.o \
  engObj/Adventure.o \
  engObj/Ball.o \
  engObj/Bat.o \
  engObj/Board.o \
  engObj/Dragon.o \
  engObj/GameObject.o \
  engObj/Logger.o \
  engObj/Map.o \
  engObj/Portcullis.o \
  engObj/RemoteAction.o \
  engObj/Room.o \
  engObj/Sync.o \
  engObj/Transport.o \
  engObj/YTransport.o

adventure.html: AdventureView.o JsTransport.o $(ENGINE_OBJECTS)
	emcc $^ -o adventure.html

AdventureView.o: AdventureView.cpp
	emcc -c $^ -o $@

JsTransport.o: JsTransport.cpp
	emcc -c $^ -o $@

engObj/%.o:   ../engine/%.cpp
	emcc -c $^ -o $@

clean:
	rm -f adventure.html adventure.js *.o engObj/*.o 

