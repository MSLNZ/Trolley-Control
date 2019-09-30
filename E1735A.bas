Attribute VB_Name = "E1735A_DLL_Declares"
' Add this unit to your project to make an easy use of E1735A.dll
'                          Beijing Pretech Science Co., Ltd.


Public Const EC_NOERROR = 0
Public Const EC_UNKNOWNERROR = 1
Public Const EC_ACCESSDENIED = 2
Public Const EC_BADPARAMETER = 3
Public Const EC_EMPTYBUFFER = 11
Public Const EC_BUFFERFULL = 12
Public Const EC_SAMPLELOST = 13
Public Const EC_TIMERSTILLON = 14
Public Const EC_TIMERISOFF = 15
Public Const EC_TIMEERROR = 16
Public Const EC_MEMORYFULL = 17
Public Const EC_NOSAMPLE = 18
Public Const EC_LASEROFF = 21
Public Const EC_NORETURN = 22
Public Const EC_REFSIGLOST = 23
Public Const EC_MEASIGLOST = 24
Public Const EC_BADREFSIG = 25
Public Const EC_BADMEASIG = 26
Public Const EC_IGNOREDTRIG = 27
Public Const EC_OUTOFRANGE = 28

Public Const TB_TIMER = 1
Public Const TB_SOFTWARE = 2
Public Const TB_REMOTE = 4
Public Const TB_AQB = 8
Public Const TB_EXTERNAL = 16

Public Const SC_DISABLEAQB = 0
Public Const SC_ENABLEAQB = 1
Public Const SC_DISABLEUPDN = 2
Public Const SC_ENABLEUPDN = 3

Public Const BC_NONE = 0
Public Const BC_RECORD = 1
Public Const BC_RESET = 2
Public Const BC_BOTH = 3

Type TLaserSample
    LaserPos As Double
    TimeStamp As Currency
    LaserTrigger As Long
    LaserError As Long
End Type

Public Declare Function E1735A_ReadDeviceCount Lib "E1735A.dll" () As Long

Public Declare Function E1735A_SelectDevice Lib "E1735A.dll" _
         (ByVal Index As Long) As Boolean

Public Declare Function E1735A_GetAllRevisions Lib "E1735A.dll" _
         (ByRef rHWRevPtr As Long, ByRef rFWRevPtr As Long, ByRef rDrvRevPtr As Long, _
         ByRef rCoreDLLRevPtr As Long, ByRef rDLLRevPtr As Long) As Boolean

Public Declare Function E1735A_BlinkLED Lib "E1735A.dll" () As Boolean

Public Declare Function E1735A_ResetDevice Lib "E1735A.dll" () As Boolean

Public Declare Function E1735A_ReadLastError Lib "E1735A.dll" () As Long

Public Declare Function E1735A_ReadSampleCount Lib "E1735A.dll" () As Long

Public Declare Function E1735A_ReadSample Lib "E1735A.dll" () As Double

Public Declare Function E1735A_ReadLastTrigger Lib "E1735A.dll" () As Long

Public Declare Function E1735A_ReadLastTimeStamp Lib "E1735A.dll" () As Currency

Public Declare Function E1735A_ReadAllSamples Lib "E1735A.dll" _
        (ByRef FirstBufferItem As TLaserSample, ByVal Size As Long) As Long

Public Declare Function E1735A_SetSampleTriggers Lib "E1735A.dll" (ByVal TriggerTypes As Long) As Boolean

Public Declare Function E1735A_GetSampleTriggers Lib "E1735A.dll" () As Long

Public Declare Function E1735A_SetupTimer Lib "E1735A.dll" (ByVal Interval As Double) As Boolean

Public Declare Function E1735A_StartTimer Lib "E1735A.dll" () As Boolean

Public Declare Function E1735A_StopTimer Lib "E1735A.dll" () As Boolean

' This function returns a pointer of Double!
Public Declare Function E1735A_ReadTimerSamples Lib "E1735A.dll" (ByVal Count As Long) As Long

Public Declare Function E1735A_SetupAQB Lib "E1735A.dll" _
         (ByVal Modulo As Long, ByVal Hysteresis As Long, ByVal Settings As Long) As Boolean

Public Declare Function E1735A_ReadAQB Lib "E1735A.dll" () As Long

Public Declare Function E1735A_ReadSampleAndAQB Lib "E1735A.dll" (ByRef rAQBPtr As Long) As Double

Public Declare Function E1735A_StartExternalSampling Lib "E1735A.dll" () As Boolean

Public Declare Function E1735A_StopExternalSampling Lib "E1735A.dll" () As Boolean

Public Declare Function E1735A_ReadButtonClicked Lib "E1735A.dll" () As Long

Public Declare Function E1735A_ReadBeamStrength Lib "E1735A.dll" () As Double

Public Declare Function E1735A_SetOptics Lib "E1735A.dll" (ByVal NewOptics As Long) As Boolean

Public Declare Function E1735A_GetOptics Lib "E1735A.dll" () As Long

Public Declare Function E1735A_SetParameter Lib "E1735A.dll" _
         (ByVal Index As Long, ByVal Value As Double) As Boolean

Public Declare Function E1735A_GetParameter Lib "E1735A.dll" _
         (ByVal Index As Long) As Double

