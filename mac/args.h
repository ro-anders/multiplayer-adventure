

#ifndef args_h
#define args_h

#if !defined(__cplusplus)
#define MONExternC extern
#else
#define MONExternC extern "C"
#endif


MONExternC void Args_SetArgs(int inArgc, char* inArgv[]);


MONExternC void Args_GetArgs(int* outArgc, char*** outArgv);


#endif /* args_h */
