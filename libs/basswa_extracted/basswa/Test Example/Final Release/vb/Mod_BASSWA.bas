Attribute VB_Name = "Mod_BassWA"
Global CurrentPlugin As Long

Public Declare Function BASS_WA_LoadVisPlugin Lib "bass_wa.dll" (ByVal path As String) As Boolean
Public Declare Sub BASS_WA_LoadAllVis Lib "bass_wa.dll" ()
Public Declare Sub BASS_WA_LoadVis Lib "bass_wa.dll" (ByVal index As Long)
Public Declare Function BASS_WA_GetWinampPluginCount Lib "bass_wa.dll" () As Long
Public Declare Function BASS_WA_GetModuleCount Lib "bass_wa.dll" (ByVal index As Long) As Long
Public Declare Function BASS_WA_GetModuleInfo Lib "bass_wa.dll" (ByVal plugin As Long, ByVal the_module As Long) As Long
Public Declare Sub BASS_WA_FreeVisInfo Lib "bass_wa.dll" ()
Public Declare Sub BASS_WA_FreeVis Lib "bass_wa.dll" (ByVal index As Long)
Public Declare Function BASS_WA_GetWinampPluginInfo Lib "bass_wa.dll" (ByVal index As Long) As Long
Public Declare Sub BASS_WA_Start_Vis Lib "bass_wa.dll" (ByVal i As Long, ByVal hchan As Long)
Public Declare Sub BASS_WA_Stop_Vis Lib "bass_wa.dll" (ByVal i As Long)
Public Declare Sub BASS_WA_StopModule Lib "bass_wa.dll" (ByVal plugin As Long, ByVal the_module As Long)
Public Declare Sub BASS_WA_Config_Vis Lib "bass_wa.dll" (ByVal i As Long, ByVal module_index As Long)
Public Declare Sub BASS_WA_SetModule Lib "bass_wa.dll" (ByVal the_module As Long)
Public Declare Sub BASS_WA_SetSongTitle Lib "bass_wa.dll" (ByVal thetitle As String)
Public Declare Sub BASS_WA_SetElapsed Lib "bass_wa.dll" (ByVal elapsed As Long)
Public Declare Sub BASS_WA_SetLength Lib "bass_wa.dll" (ByVal elapsed As Long)
Public Declare Sub BASS_WA_IsPlaying Lib "bass_wa.dll" (ByVal playing As Long)
Public Declare Sub BASS_WA_SetHwnd Lib "bass_wa.dll" (ByVal the_hwnd As Long)
Public Declare Sub BASS_WA_SetChannel Lib "bass_wa.dll" (ByVal hchan As Long)
Public Declare Function BASS_WA_GetVisHwnd Lib "bass_wa.dll" () As Long

Public Declare Sub Sleep Lib "kernel32" (ByVal dwMilliseconds As Long)
Public Declare Function ConvCStringToVBString Lib "kernel32" Alias "lstrcpyA" (ByVal lpsz As String, ByVal pt As Long) As Long

Public Function GetStringFromPointer(ByVal lpString As Long) As String
Dim NullCharPos As Long
Dim szBuffer As String

    szBuffer = String(255, 0)
    ConvCStringToVBString szBuffer, lpString
    NullCharPos = InStr(szBuffer, vbNullChar)
    GetStringFromPointer = Left(szBuffer, NullCharPos - 1)
End Function


