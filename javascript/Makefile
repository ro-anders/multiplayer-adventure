all: adventure.html

adventure.html: AdventureView.o
	emcc $^ -o adventure.html

AdventureView.o: AdventureView.cpp
	emcc -c $^ -o $@

