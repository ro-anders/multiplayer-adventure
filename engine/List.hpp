
#ifndef List_hpp
#define List_hpp

#include <stdio.h>

template <class T> class List {

private:
    T* array;
    int numEntries;

public:
    
    List() :
      array(new T[10]),
      numEntries(0) {}
    
    List(const List<T>& other) :
      array(new T[10]),
      numEntries(other.numEntries)
    { for(int ctr=0; ctr<numEntries; ++ctr) {array[ctr]=other.array[ctr];}}
    
    ~List() {delete[] array;}
    
    T& get(int i) {return array[i];}
    
    void add(T t) {array[numEntries]=t; ++numEntries;}
    
    void set(int place, T t) {array[place]=t;}
    
    int size() {return numEntries;}
    
};

#endif /* List_hpp */
