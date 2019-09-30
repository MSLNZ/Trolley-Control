/* Include this file into your project to make an easy use of E1735A.dll */

/* Initialize_E1735A_DLL must be the first function to be called before any other function. */
/* Finalize_E1735A_DLL must be called to finilize the library after all. */

#include <windows.h>

#define EC_NOERROR 0
#define EC_UNKNOWNERROR 1
#define EC_ACCESSDENIED 2
#define EC_BADPARAMETER 3
#define EC_EMPTYBUFFER 11
#define EC_BUFFERFULL 12
#define EC_SAMPLELOST 13
#define EC_TIMERSTILLON 14
#define EC_TIMERISOFF 15
#define EC_TIMEERROR 16
#define EC_MEMORYFULL 17
#define EC_NOSAMPLE 18
#define EC_LASEROFF 21
#define EC_NORETURN 22
#define EC_REFSIGLOST 23
#define EC_MEASIGLOST 24
#define EC_BADREFSIG 25
#define EC_BADMEASIG 26
#define EC_IGNOREDTRIG 27
#define EC_OUTOFRANGE 28

#define TB_TIMER 0x01
#define TB_SOFTWARE 0x02
#define TB_REMOTE 0x04
#define TB_AQB 0x08
#define TB_EXTERNAL 0x10

#define SC_DISABLEAQB 0
#define SC_ENABLEAQB 1
#define SC_DISABLEUPDN 2
#define SC_ENABLEUPDN 3

#define BC_NONE 0
#define BC_RECORD 1
#define BC_RESET 2
#define BC_BOTH 3

typedef struct {
	double LaserPos;
	__int64 TimeStamp;
	int LaserTrigger;
	int LaserError;
} TLaserSample;

typedef     int (_stdcall * FuncType_E1735A_ReadDeviceCount)();
typedef    bool (_stdcall * FuncType_E1735A_SelectDevice)(int);
typedef    bool (_stdcall * FuncType_E1735A_GetAllRevisions)(int*, int*, int*, int*, int*);
typedef    bool (_stdcall * FuncType_E1735A_BlinkLED)();
typedef    bool (_stdcall * FuncType_E1735A_ResetDevice)();
typedef     int (_stdcall * FuncType_E1735A_ReadLastError)();
typedef     int (_stdcall * FuncType_E1735A_ReadSampleCount)();
typedef  double (_stdcall * FuncType_E1735A_ReadSample)();
typedef     int (_stdcall * FuncType_E1735A_ReadLastTrigger)();
typedef __int64 (_stdcall * FuncType_E1735A_ReadLastTimeStamp)();
typedef     int (_stdcall * FuncType_E1735A_ReadAllSamples)(TLaserSample*, int);
typedef    bool (_stdcall * FuncType_E1735A_SetSampleTriggers)(int);
typedef     int (_stdcall * FuncType_E1735A_GetSampleTriggers)();
typedef    bool (_stdcall * FuncType_E1735A_SetupTimer)(double);
typedef    bool (_stdcall * FuncType_E1735A_StartTimer)();
typedef    bool (_stdcall * FuncType_E1735A_StopTimer)();
typedef double* (_stdcall * FuncType_E1735A_ReadTimerSamples)(int);
typedef    bool (_stdcall * FuncType_E1735A_SetupAQB)(int, int, int);
typedef     int (_stdcall * FuncType_E1735A_ReadAQB)();
typedef  double (_stdcall * FuncType_E1735A_ReadSampleAndAQB)(int*);
typedef    bool (_stdcall * FuncType_E1735A_StartExternalSampling)();
typedef    bool (_stdcall * FuncType_E1735A_StopExternalSampling)();
typedef     int (_stdcall * FuncType_E1735A_ReadButtonClicked)();
typedef  double (_stdcall * FuncType_E1735A_ReadBeamStrength)();
typedef    bool (_stdcall * FuncType_E1735A_SetOptics)(int);
typedef     int (_stdcall * FuncType_E1735A_GetOptics)();
typedef    bool (_stdcall * FuncType_E1735A_SetParameter)(int, double);
typedef  double (_stdcall * FuncType_E1735A_GetParameter)(int);

HINSTANCE Handle_E1735A_DLL = NULL;

FARPROC FuncAddr_E1735A_ReadDeviceCount = NULL;
FARPROC FuncAddr_E1735A_SelectDevice = NULL;
FARPROC FuncAddr_E1735A_GetAllRevisions = NULL;
FARPROC FuncAddr_E1735A_BlinkLED = NULL;
FARPROC FuncAddr_E1735A_ResetDevice = NULL;
FARPROC FuncAddr_E1735A_ReadLastError = NULL;
FARPROC FuncAddr_E1735A_ReadSampleCount = NULL;
FARPROC FuncAddr_E1735A_ReadSample = NULL;
FARPROC FuncAddr_E1735A_ReadLastTrigger = NULL;
FARPROC FuncAddr_E1735A_ReadLastTimeStamp = NULL;
FARPROC FuncAddr_E1735A_ReadAllSamples = NULL;
FARPROC FuncAddr_E1735A_SetSampleTriggers = NULL;
FARPROC FuncAddr_E1735A_GetSampleTriggers = NULL;
FARPROC FuncAddr_E1735A_SetupTimer = NULL;
FARPROC FuncAddr_E1735A_StartTimer = NULL;
FARPROC FuncAddr_E1735A_StopTimer = NULL;
FARPROC FuncAddr_E1735A_ReadTimerSamples = NULL;
FARPROC FuncAddr_E1735A_SetupAQB = NULL;
FARPROC FuncAddr_E1735A_ReadAQB = NULL;
FARPROC FuncAddr_E1735A_ReadSampleAndAQB = NULL;
FARPROC FuncAddr_E1735A_StartExternalSampling = NULL;
FARPROC FuncAddr_E1735A_StopExternalSampling = NULL;
FARPROC FuncAddr_E1735A_ReadButtonClicked = NULL;
FARPROC FuncAddr_E1735A_ReadBeamStrength = NULL;
FARPROC FuncAddr_E1735A_SetOptics = NULL;
FARPROC FuncAddr_E1735A_GetOptics = NULL;
FARPROC FuncAddr_E1735A_SetParameter = NULL;
FARPROC FuncAddr_E1735A_GetParameter = NULL;

int Initialize_E1735A_DLL()
{
  if (Handle_E1735A_DLL == NULL)
  {
    Handle_E1735A_DLL = LoadLibrary("E1735A.dll");
    if (Handle_E1735A_DLL != NULL)
    {
      FuncAddr_E1735A_ReadDeviceCount = GetProcAddress(Handle_E1735A_DLL, "E1735A_ReadDeviceCount");
      FuncAddr_E1735A_SelectDevice = GetProcAddress(Handle_E1735A_DLL, "E1735A_SelectDevice");
      FuncAddr_E1735A_GetAllRevisions = GetProcAddress(Handle_E1735A_DLL, "E1735A_GetAllRevisions");
      FuncAddr_E1735A_BlinkLED = GetProcAddress(Handle_E1735A_DLL, "E1735A_BlinkLED");
      FuncAddr_E1735A_ResetDevice = GetProcAddress(Handle_E1735A_DLL, "E1735A_ResetDevice");
      FuncAddr_E1735A_ReadLastError = GetProcAddress(Handle_E1735A_DLL, "E1735A_ReadLastError");
      FuncAddr_E1735A_ReadSampleCount = GetProcAddress(Handle_E1735A_DLL, "E1735A_ReadSampleCount");
      FuncAddr_E1735A_ReadSample = GetProcAddress(Handle_E1735A_DLL, "E1735A_ReadSample");
      FuncAddr_E1735A_ReadAllSamples = GetProcAddress(Handle_E1735A_DLL, "E1735A_ReadAllSamples");
      FuncAddr_E1735A_ReadLastTrigger = GetProcAddress(Handle_E1735A_DLL, "E1735A_ReadLastTrigger");
      FuncAddr_E1735A_ReadLastTimeStamp = GetProcAddress(Handle_E1735A_DLL, "E1735A_ReadLastTimeStamp");
      FuncAddr_E1735A_SetSampleTriggers = GetProcAddress(Handle_E1735A_DLL, "E1735A_SetSampleTriggers");
      FuncAddr_E1735A_GetSampleTriggers = GetProcAddress(Handle_E1735A_DLL, "E1735A_GetSampleTriggers");
      FuncAddr_E1735A_SetupTimer = GetProcAddress(Handle_E1735A_DLL, "E1735A_SetupTimer");
      FuncAddr_E1735A_StartTimer = GetProcAddress(Handle_E1735A_DLL, "E1735A_StartTimer");
      FuncAddr_E1735A_StopTimer = GetProcAddress(Handle_E1735A_DLL, "E1735A_StopTimer");
      FuncAddr_E1735A_ReadTimerSamples = GetProcAddress(Handle_E1735A_DLL, "E1735A_ReadTimerSamples");
      FuncAddr_E1735A_SetupAQB = GetProcAddress(Handle_E1735A_DLL, "E1735A_SetupAQB");
      FuncAddr_E1735A_ReadAQB = GetProcAddress(Handle_E1735A_DLL, "E1735A_ReadAQB");
      FuncAddr_E1735A_ReadSampleAndAQB = GetProcAddress(Handle_E1735A_DLL, "E1735A_ReadSampleAndAQB");
      FuncAddr_E1735A_StartExternalSampling = GetProcAddress(Handle_E1735A_DLL, "E1735A_StartExternalSampling");
      FuncAddr_E1735A_StopExternalSampling = GetProcAddress(Handle_E1735A_DLL, "E1735A_StopExternalSampling");
      FuncAddr_E1735A_ReadButtonClicked = GetProcAddress(Handle_E1735A_DLL, "E1735A_ReadButtonClicked");
      FuncAddr_E1735A_ReadBeamStrength = GetProcAddress(Handle_E1735A_DLL, "E1735A_ReadBeamStrength");
      FuncAddr_E1735A_SetOptics = GetProcAddress(Handle_E1735A_DLL, "E1735A_SetOptics");
      FuncAddr_E1735A_GetOptics = GetProcAddress(Handle_E1735A_DLL, "E1735A_GetOptics");
      FuncAddr_E1735A_SetParameter = GetProcAddress(Handle_E1735A_DLL, "E1735A_SetParameter");
      FuncAddr_E1735A_GetParameter = GetProcAddress(Handle_E1735A_DLL, "E1735A_GetParameter");
      return 0;
    }
    else
    {
      return -1;
    }
  }
  else
  {
    return 1;
  }
}

int E1735A_ReadDeviceCount()
{
  if (FuncAddr_E1735A_ReadDeviceCount != NULL)
  {
    return (*((FuncType_E1735A_ReadDeviceCount) FuncAddr_E1735A_ReadDeviceCount))();
  }
  else
  {
    return -1;
  }
}

bool E1735A_SelectDevice(int Para0)
{
  if (FuncAddr_E1735A_SelectDevice != NULL)
  {
    return (*((FuncType_E1735A_SelectDevice) FuncAddr_E1735A_SelectDevice))(Para0);
  }
  else
  {
    return false;
  }
}

bool E1735A_GetAllRevisions(int* Para0, int* Para1, int* Para2, int* Para3, int* Para4)
{
  if (FuncAddr_E1735A_GetAllRevisions != NULL)
  {
    return (*((FuncType_E1735A_GetAllRevisions) FuncAddr_E1735A_GetAllRevisions))(Para0, Para1, Para2, Para3, Para4);
  }
  else
  {
    return false;
  }
}

bool E1735A_BlinkLED()
{
  if (FuncAddr_E1735A_BlinkLED != NULL)
  {
    return (*((FuncType_E1735A_BlinkLED) FuncAddr_E1735A_BlinkLED))();
  }
  else
  {
    return false;
  }
}

bool E1735A_ResetDevice()
{
  if (FuncAddr_E1735A_ResetDevice != NULL)
  {
    return (*((FuncType_E1735A_ResetDevice) FuncAddr_E1735A_ResetDevice))();
  }
  else 
  {
    return false;
  }
}

int E1735A_ReadLastError()
{
  if (FuncAddr_E1735A_ReadLastError != NULL)
  {
    return (*((FuncType_E1735A_ReadLastError) FuncAddr_E1735A_ReadLastError))();
  }
  else
  {
    return -1;
  }
}

int E1735A_ReadSampleCount()
{
  if (FuncAddr_E1735A_ReadSampleCount != NULL)
  {
    return (*((FuncType_E1735A_ReadSampleCount) FuncAddr_E1735A_ReadSampleCount))();
  }
  else
  {
    return -1;
  }
}

double E1735A_ReadSample()
{
  if (FuncAddr_E1735A_ReadSample != NULL)
  {
    return (*((FuncType_E1735A_ReadSample) FuncAddr_E1735A_ReadSample))();
  }
  else
  {
	double x=0;
    return 1/x;
  }
}

int E1735A_ReadLastTrigger()
{
  if (FuncAddr_E1735A_ReadLastTrigger != NULL)
  {
    return (*((FuncType_E1735A_ReadLastTrigger) FuncAddr_E1735A_ReadLastTrigger))();
  }
  else
  {
    return -1;
  }
}

__int64 E1735A_ReadLastTimeStamp()
{
  if (FuncAddr_E1735A_ReadLastTimeStamp != NULL)
  {
    return (*((FuncType_E1735A_ReadLastTimeStamp) FuncAddr_E1735A_ReadLastTimeStamp))();
  }
  else
  {
	  return 0;
  }
}

int E1735A_ReadAllSamples(TLaserSample* Para0, int Para1)
{
  if (FuncAddr_E1735A_ReadSampleAndAQB != NULL)
  {
    return (*((FuncType_E1735A_ReadAllSamples) FuncAddr_E1735A_ReadAllSamples))(Para0, Para1);
  }
  else
  {
    return 0;
  }
}

bool E1735A_SetSampleTriggers(int Para0)
{
  if (FuncAddr_E1735A_SetSampleTriggers != NULL)
  {
    return (*((FuncType_E1735A_SetSampleTriggers) FuncAddr_E1735A_SetSampleTriggers))(Para0);
  }
  else
  {
    return false;
  }
}

int E1735A_GetSampleTriggers()
{
  if (FuncAddr_E1735A_GetSampleTriggers != NULL)
  {
    return (*((FuncType_E1735A_GetSampleTriggers) FuncAddr_E1735A_GetSampleTriggers))();
  }
  else
  {
    return -1;
  }
}

bool E1735A_SetupTimer(double Para0)
{
  if (FuncAddr_E1735A_SetupTimer != NULL)
  {
    return (*((FuncType_E1735A_SetupTimer) FuncAddr_E1735A_SetupTimer))(Para0);
  }
  else
  {
    return false;
  }
}

bool E1735A_StartTimer()
{
  if (FuncAddr_E1735A_StartTimer != NULL)
  {
    return (*((FuncType_E1735A_StartTimer) FuncAddr_E1735A_StartTimer))();
  }
  else
  {
    return false;
  }
}

bool E1735A_StopTimer()
{
  if (FuncAddr_E1735A_StopTimer != NULL)
  {
    return (*((FuncType_E1735A_StopTimer) FuncAddr_E1735A_StopTimer))();
  }
  else
  {
    return false;
  }
}

double* E1735A_ReadTimerSamples(int Para0)
{
  if (FuncAddr_E1735A_ReadTimerSamples != NULL)
  {
    return (*((FuncType_E1735A_ReadTimerSamples) FuncAddr_E1735A_ReadTimerSamples))(Para0);
  }
  else
  {
    return NULL;
  }
}

bool E1735A_SetupAQB(int Para0, int Para1, int Para2)
{
  if (FuncAddr_E1735A_SetupAQB != NULL)
  {
    return (*((FuncType_E1735A_SetupAQB) FuncAddr_E1735A_SetupAQB))(Para0, Para1, Para2);
  }
  else
  {
    return false;
  }
}

int E1735A_ReadAQB()
{
  if (FuncAddr_E1735A_ReadAQB != NULL)
  {
    return (*((FuncType_E1735A_ReadAQB) FuncAddr_E1735A_ReadAQB))();
  }
  else
  {
    return -1;
  }
}

double E1735A_ReadSampleAndAQB(int* Para0)
{
  if (FuncAddr_E1735A_ReadSampleAndAQB != NULL)
  {
    return (*((FuncType_E1735A_ReadSampleAndAQB) FuncAddr_E1735A_ReadSampleAndAQB))(Para0);
  }
  else
  {
    return 0;
  }
}

bool E1735A_StartExternalSampling()
{
  if (FuncAddr_E1735A_StartExternalSampling != NULL)
  {
    return (*((FuncType_E1735A_StartExternalSampling) FuncAddr_E1735A_StartExternalSampling))();
  }
  else
  {
    return false;
  }
}

bool E1735A_StopExternalSampling()
{
  if (FuncAddr_E1735A_StopExternalSampling != NULL)
  {
    return (*((FuncType_E1735A_StopExternalSampling) FuncAddr_E1735A_StopExternalSampling))();
  }
  else
  {
    return false;
  }
}

int E1735A_ReadButtonClicked()
{
  if (FuncAddr_E1735A_ReadButtonClicked != NULL)
  {
    return (*((FuncType_E1735A_ReadButtonClicked) FuncAddr_E1735A_ReadButtonClicked))();
  }
  else
  {
    return -1;
  }
}

double E1735A_ReadBeamStrength()
{
  if (FuncAddr_E1735A_ReadBeamStrength != NULL)
  {
    return (*((FuncType_E1735A_ReadBeamStrength) FuncAddr_E1735A_ReadBeamStrength))();
  }
  else
  {
    return 0;
  }
}

bool E1735A_SetOptics(int Para0)
{
  if (FuncAddr_E1735A_SetOptics != NULL)
  {
    return (*((FuncType_E1735A_SetOptics) FuncAddr_E1735A_SetOptics))(Para0);
  }
  else
  {
    return false;
  }
}

int E1735A_GetOptics()
{
  if (FuncAddr_E1735A_GetOptics != NULL)
  {
    return (*((FuncType_E1735A_GetOptics) FuncAddr_E1735A_GetOptics))();
  }
  else
  {
    return -1;
  }
}

bool E1735A_SetParameter(int Para0, double Para1)
{
  if (FuncAddr_E1735A_SetParameter != NULL)
  {
    return (*((FuncType_E1735A_SetParameter) FuncAddr_E1735A_SetParameter))(Para0, Para1);
  }
  else
  {
    return false;
  }
}

double E1735A_GetParameter(int Para0)
{
  if (FuncAddr_E1735A_GetParameter != NULL)
  {
    return (*((FuncType_E1735A_GetParameter) FuncAddr_E1735A_GetParameter))(Para0);
  }
  else
  {
    return 0;
  }
}

int Finalize_E1735A_DLL()
{
  if (Handle_E1735A_DLL != NULL)
  {
    FreeLibrary(Handle_E1735A_DLL);
    Handle_E1735A_DLL = NULL;
    FuncAddr_E1735A_ReadDeviceCount = NULL;
    FuncAddr_E1735A_SelectDevice = NULL;
    FuncAddr_E1735A_GetAllRevisions = NULL;
    FuncAddr_E1735A_BlinkLED = NULL;
    FuncAddr_E1735A_ResetDevice = NULL;
    FuncAddr_E1735A_ReadLastError = NULL;
    FuncAddr_E1735A_ReadSampleCount = NULL;
    FuncAddr_E1735A_ReadSample = NULL;
    FuncAddr_E1735A_ReadAllSamples = NULL;
    FuncAddr_E1735A_ReadLastTrigger = NULL;
    FuncAddr_E1735A_ReadLastTimeStamp = NULL;
    FuncAddr_E1735A_SetSampleTriggers = NULL;
    FuncAddr_E1735A_GetSampleTriggers = NULL;
    FuncAddr_E1735A_SetupTimer = NULL;
    FuncAddr_E1735A_StartTimer = NULL;
    FuncAddr_E1735A_StopTimer = NULL;
    FuncAddr_E1735A_ReadTimerSamples = NULL;
    FuncAddr_E1735A_SetupAQB = NULL;
    FuncAddr_E1735A_ReadAQB = NULL;
    FuncAddr_E1735A_ReadSampleAndAQB = NULL;
    FuncAddr_E1735A_StartExternalSampling = NULL;
    FuncAddr_E1735A_StopExternalSampling = NULL;
    FuncAddr_E1735A_ReadButtonClicked = NULL;
    FuncAddr_E1735A_ReadBeamStrength = NULL;
    FuncAddr_E1735A_SetOptics = NULL;
    FuncAddr_E1735A_GetOptics = NULL;
    FuncAddr_E1735A_SetParameter = NULL;
    FuncAddr_E1735A_GetParameter = NULL;
    return 0;
  }
  else
  {
    return -1;
  }
}
