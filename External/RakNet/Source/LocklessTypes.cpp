#include "LocklessTypes.h"

using namespace RakNet;

LocklessUint32_t::LocklessUint32_t()
{
	value=0;
}
LocklessUint32_t::LocklessUint32_t(uint32_t initial)
{
	value=initial;
}
uint32_t LocklessUint32_t::Increment(void)
{
#ifdef _WIN32
	return (uint32_t) InterlockedIncrement(&value);
#else
	return __sync_fetch_and_add (&value, (uint32_t) 1);
#endif
}
uint32_t LocklessUint32_t::Decrement(void)
{
#ifdef _WIN32
	return (uint32_t) InterlockedDecrement(&value);
#else
	return __sync_fetch_and_add (&value, (uint32_t) -1);
#endif
}
