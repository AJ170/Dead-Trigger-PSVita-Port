#include <kernel.h>
#include <moduleinfo.h>
#include <rtc.h>

#define PRX_EXPORT extern "C" __declspec (dllexport)

SCE_MODULE_INFO( VitaClock, SCE_MODULE_ATTR_NONE, 1, 1);

extern "C" int module_start(SceSize sz, const void* arg)
{
	return SCE_KERNEL_START_SUCCESS;
}

// Packed local date as yyyymmdd, or -1 if the clock could not be read.
PRX_EXPORT int VitaClock_GetDate()
{
	SceDateTime t;
	if (sceRtcGetCurrentClockLocalTime(&t) < 0)
		return -1;
	return t.year * 10000 + t.month * 100 + t.day;
}

// Packed local time as hhmm, or -1 if the clock could not be read.
PRX_EXPORT int VitaClock_GetTime()
{
	SceDateTime t;
	if (sceRtcGetCurrentClockLocalTime(&t) < 0)
		return -1;
	return t.hour * 100 + t.minute;
}
