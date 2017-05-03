
#ifndef List_hpp
#define List_hpp

#include <stdio.h>
#include <stdexcept>

template <class T> class List {

private:
    T* array;
    int numEntries;
public:
    
    List() :
      array(new T[16]),
      numEntries(0) {}
    
    List(const List<T>& other) :
      array(new T[16]),
      numEntries(other.numEntries)
    { for(int ctr=0; ctr<numEntries; ++ctr) {array[ctr]=other.array[ctr];}}
    
    ~List() {delete[] array;}
    
	T& get(int i) { if (i > numEntries) throw std::runtime_error("Index out of bounds"); return array[i]; }
    
	void add(T t) { if (numEntries == 16) throw std::runtime_error("List full"); array[numEntries] = t; ++numEntries; }
    
    void clear() {numEntries=0;}
    
    void set(int place, T t) { if (place > numEntries) throw std::runtime_error("Index out of bounds"); array[place]=t;}
    
    int size() {return numEntries;}
    
};

#endif /* List_hpp */
