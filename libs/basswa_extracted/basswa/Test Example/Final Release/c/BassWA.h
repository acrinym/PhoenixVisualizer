#ifndef BASSWA_H
#define BASSWA_H

#include <wtypes.h>

#ifdef __cplusplus
extern "C" {
#endif

#ifndef BASSWADEF
#define BASSWADEF(f) WINAPI f
#endif

void BASSWADEF(BASS_WA_Start_Vis)(int x, int hchan);
void BASSWADEF(BASS_WA_Stop_Vis)(int x);
void BASSWADEF(BASS_WA_Config_Vis)(int x, int hindex);
bool BASSWADEF(BASS_WA_LoadVisPlugin)(LPCTSTR hpath);
void BASSWADEF(BASS_WA_LoadAllVis)(void);
void BASSWADEF(BASS_WA_LoadVis)(int hindex);
void BASSWADEF(BASS_WA_FreeVis)(int hindex);
void BASSWADEF(BASS_WA_FreeVisInfo)(void);
UINT BASSWADEF(BASS_WA_GetModuleCount)(int hindex);
LPSTR BASSWADEF(BASS_WA_GetModuleInfo)(int hplugin, int hmodule);
UINT BASSWADEF(BASS_WA_GetWinampPluginCount)(void);
void BASSWADEF(BASS_WA_SetHwnd)(HWND hhwnd);
HWND BASSWADEF(BASS_WA_GetVisHwnd)(void);
void BASSWADEF(BASS_WA_SetSongTitle)(LPTSTR htitle);
void BASSWADEF(BASS_WA_SetElapsed)(int helapsed);
void BASSWADEF(BASS_WA_SetLength)(int hlength);
void BASSWADEF(BASS_WA_IsPlaying)(int hplaying);
void BASSWADEF(BASS_WA_SetModule)(int hmodule);
LPSTR BASSWADEF(BASS_WA_GetWinampPluginInfo)(int hmodule);
void BASSWADEF(BASS_WA_SetChannel)(int hchan);

#ifdef __cplusplus
}
#endif

#endif