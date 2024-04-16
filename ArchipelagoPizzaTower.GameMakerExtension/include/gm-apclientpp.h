// https://github.com/nkrapivin/libLassebq/blob/master/libLassebq/libLassebq.h

#define exterR extern "C" __declspec(dllexport) double       __cdecl 
#define exterS extern "C" __declspec(dllexport) const char* __cdecl 


exterR ap_connect(const char* uri);
exterR ap_connect_slot(const char* name, const char* password, double deathlink);
exterR ap_poll();
exterR ap_get_state();
exterR ap_wants_deathlink();