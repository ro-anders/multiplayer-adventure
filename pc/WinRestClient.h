#pragma once

#ifndef WinRestClient_hpp
#define WinRestClient_hpp

#include <stdio.h>
#include "..\engine\RestClient.hpp"


class WinRestClient : public RestClient {
protected:

	int request(const char* path, const char* message, char* responseBuffer, int bufferLength);

};

#endif /* WinRestClient_hpp */
