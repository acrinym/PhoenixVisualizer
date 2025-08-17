// The code used by BASS_WA was originally written by Saïd Bougribate, all BASS related
// functions and some improvements are written by Peter Hebels.
// This code is released under GNU GPL, which means you have to credit the authors if
// you use it in your applications.
//
// Other contributors: Greg, Silhwan and Svante Boberg
//
///////////////////////////////
// Monday January 24, 2005   //
// Update made by Greg Ellis //
///////////////////////////////
//******************************************************************************
//* 1. Added critical section object to avoid crash when start/stopping plugins
//* 2. Initialized all variables to NULL or 0 so that I could check to ensure that
//*    that certain objects were intialized before using them and also to ensure
//*	   that the variables were initialied before trying to free their memory
//* 3. Changed code to have all plugins loaded for entire duration until the
//*    function BASS_WA_FreeVisInfo() is called
//* 4. Changed code that set the song title so that it no longer uses strdup function
//*    because I believe that was causing crashes due to the malloc calls that occur
//*	   from within the function call itself and also because it was not necessary
//* 5. Added if statements around any critical code to avoid crashes in case any 
//*    of the objects that are trying to be used have been freed or are unavailable
//* 6. Updated vb sample that goes along with this dll to show how to use to now that
//*    that I have updated this dll 
//* 7. Seems to be way more stable, there are still cases where some plugins will crash
//*    but that is usually due to some unhandled IPC messages
//******************************************************************************

//
// If you change and release the source code of this dll, you may not remove this
// message.

#include "stdafx.h"
#include "vis.h"
#include "bass_wa.h"

#define BASSDEF(f) (WINAPI *f)

#include "bassvis.h"

HINSTANCE bass=0;

BASS_CHANNELINFO info;

void LoadBass()
{
	if (!(bass=LoadLibrary("bass.dll"))) {
		ExitProcess(0);
	}

	#define LOADBASSFUNCTION(f) *((void**)&f)=GetProcAddress(bass,#f)
	LOADBASSFUNCTION(BASS_ChannelGetData);
	LOADBASSFUNCTION(BASS_ChannelGetInfo);
	LOADBASSFUNCTION(BASS_ChannelIsActive);
	FreeLibrary(bass);
}

void Create_Winamp_Window()
{
	WNDCLASSEX wndclass;
	BOOL	   registered=FALSE;

	wndclass.cbSize = sizeof(wndclass);
	wndclass.style =  CS_PARENTDC | CS_VREDRAW;
	wndclass.cbClsExtra = 0;
	wndclass.cbWndExtra = 0;
	wndclass.hInstance = inst;
	wndclass.hIcon = 0;
	wndclass.hCursor = (HICON) LoadCursor(NULL, IDC_ARROW);
	wndclass.hbrBackground = (HBRUSH) NULL;
	wndclass.lpszMenuName = NULL;
	wndclass.lpszClassName = "Winamp v1.x";
	wndclass.lpfnWndProc = WinampWndProc;
	wndclass.hIconSm = (HICON) NULL;

	registered = RegisterClassEx(&wndclass);
	if (!registered)
		MessageBox(mainhwnd,"Unable to emulate a winamp window class","Error!", MB_ICONEXCLAMATION | MB_ICONWARNING);

		vis_Window_Emu = CreateWindowEx(0,
						"Winamp v1.x", "Winamp 2.40", 
						0, 
						5, 5, 25, 25, 
						(HWND) mainhwnd, (HMENU) NULL, inst, NULL);
		
		if (!vis_Window_Emu)
			MessageBox(mainhwnd, "Unable to emulate Winamp Window!", "Error!", MB_ICONEXCLAMATION | MB_ICONWARNING);
}

void Destroy_WA_Comp_Window(void)
{
	DestroyWindow(vis_Window_Emu);
	vis_Window_Emu  = 0;
	UnregisterClass("Winamp v1.x", inst);
}

LRESULT CALLBACK WinampWndProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
	if (msg==WM_WA_IPC)
	{
		switch(lParam)
		{
			case WA_USER_GETPLAYLISTTITLE: 
			{
				if (Vis_Enable_Rendering && SongTitle != NULL )
				{
					memcpy((LPTSTR)lParam,SongTitle, strlen(SongTitle)+1);
					return TRUE;
					break;
				}
			}
			case WA_USER_GETTIME:
			{
				if (wParam == 0) return Elapsed;
				if (wParam == 1) return Length;
				break;
			}
			case WA_USER_GETINFO:
			{
				if (wParam == 0) return info.freq;
				if (wParam == 2) return info.chans;
				break;
			}
			case WA_USER_GETLISTLENGTH:
			{
				return 1;				
				break;
			}
			case WA_USER_GETLISTPOS:
			{
				return 1;				
				break;
			}
			case WA_USER_GETVERSION:
			{
				return 0x2040;
				break;
			}
			case WA_USER_STARTPLAY:
			{
				break;
			}
			case WA_USER_ISPLAYING:
			{
				if (BASS_ChannelIsActive(hchannel) == BASS_ACTIVE_PLAYING) return 1;
				if (BASS_ChannelIsActive(hchannel) == BASS_ACTIVE_PAUSED) return 3;
				
				break;
			}
		}
	}
	return DefWindowProc(hwnd, msg, wParam, lParam);
}

void CALLBACK vis_time_event(UINT uId, UINT uMsg, DWORD dwUser, DWORD dw1, DWORD dw2)
{
	int			rendered=0;			
	short		fft[1152]={0};
	float		realfft[4097]={0};
	DWORD		a=0;

	EnterCriticalSection(&cslock);

	if (module < 0) module = 0;

	if (BASS_ChannelIsActive(hchannel) == BASS_ACTIVE_PLAYING) {
		if (Vis_Enable_Rendering==1 && hchannel) {
			if (gs_vWinAmpProps[id].pModule->getModule(module)->waveformNch > 0) {
				
				BASS_ChannelGetData(hchannel,fft,2304);
				
				for (a=0;a<vis_Plugin_Samples;a++)
				{
					pcmBuffer[a] = (signed short)(fft[a]);
				}
				
				if(info.chans==1) {
					Cnv16to8(pcmBuffer,(signed char*) &gs_vWinAmpProps[id].pModule->getModule(module)->waveformData[0][0], vis_Plugin_Samples);
					Cnv16to8(pcmBuffer,(signed char*) &gs_vWinAmpProps[id].pModule->getModule(module)->waveformData[1][0], vis_Plugin_Samples);
				} else {
					Cnv16to8(pcmBuffer,(signed char*) &gs_vWinAmpProps[id].pModule->getModule(module)->waveformData[0][0], vis_Plugin_Samples);
					Cnv16to8(pcmBuffer+1,(signed char*) &gs_vWinAmpProps[id].pModule->getModule(module)->waveformData[1][0], vis_Plugin_Samples);
				}
			} else if (gs_vWinAmpProps[id].pModule->getModule(module)->spectrumNch > 0) {
				
				if(info.chans==1) {
					BASS_ChannelGetData(hchannel,realfft,BASS_DATA_FFT2048);
			
					for (a=0;a<575;a++)
					{
						fftBuffer[a] = (signed short)(96000*realfft[a]);
					}
					
					Cnv16to8(fftBuffer,(signed char*) &gs_vWinAmpProps[id].pModule->getModule(module)->spectrumData[0][0], 575);
					Cnv16to8(fftBuffer,(signed char*) &gs_vWinAmpProps[id].pModule->getModule(module)->spectrumData[1][0], 575);
				} else {
					BASS_ChannelGetData(hchannel,realfft,BASS_DATA_FFT2048 | BASS_DATA_FFT_INDIVIDUAL);
					
					for (a=0;a<575;a++)
					{
						fftBuffer[a*2] = (signed short)(96000*realfft[a*2]);
					}
					
					Cnv16to8(fftBuffer,(signed char*) &gs_vWinAmpProps[id].pModule->getModule(module)->spectrumData[0][0], 575);
					Cnv16to8(fftBuffer+1,(signed char*) &gs_vWinAmpProps[id].pModule->getModule(module)->spectrumData[1][0], 575);
				}
			}

			if(gs_vWinAmpProps[id].pModule && Vis_Enable_Rendering==1)
			rendered = gs_vWinAmpProps[id].pModule->getModule(module)->Render(gs_vWinAmpProps[id].pModule->getModule(module));
		}
	}

	LeaveCriticalSection(&cslock);
}

HANDLE Vis_New_Thread_Init(int i)
{
	Vis_hThread = CreateThread(0, 0, BASS_WA_New_Thread, (void*)i, 0, &dwVis_hThreadId);
	if (!Vis_hThread)
	{
		MessageBox(mainhwnd,"Unable to create thread!","Error !!", MB_ICONEXCLAMATION | MB_ICONWARNING);
		return 0;
	}

	CloseHandle(Vis_hThread);
	return Vis_hThread;
}

DWORD WINAPI BASS_WA_New_Thread(LPVOID lpParam)
{
	MSG  message;
	UINT  minimum=0;					
	INT	 vis_timer=0;				
	BOOL msg_return_value=FALSE;			

	if (BASS_ChannelIsActive(hchannel) == BASS_ACTIVE_PLAYING) {
		id = (int) lpParam;

		if (id!=-1)
		{
			SetThreadPriority(GetCurrentThread(), THREAD_PRIORITY_NORMAL);
				
			Create_Winamp_Window();

			if(gs_vWinAmpProps[id].pModule)
			{
			gs_vWinAmpProps[id].pModule->getModule(module)->hwndParent  = vis_Window_Emu;
			gs_vWinAmpProps[id].pModule->getModule(module)->Init(gs_vWinAmpProps[id].pModule->getModule(module));
		

			minimum = gs_vWinAmpProps[id].pModule->getModule(module)->delayMs;	
			}

			if (minimum < 25) minimum = 25;

			Vis_Enable_Rendering = 1;
			timeBeginPeriod(250);
			
			vis_timer = timeSetEvent(minimum, 250, vis_time_event, (DWORD) NULL, TIME_PERIODIC);
			if ( !vis_timer)
				MessageBox(mainhwnd,"vis_timer Error !","Error !!", MB_ICONEXCLAMATION | MB_ICONWARNING);

			do {
				msg_return_value = GetMessage(&message, NULL, 0, 0);
				if ((message.message == WM_QUIT) || (message.message == WM_CLOSE)) Vis_Enable_Rendering = 0;

    			TranslateMessage(&message);
      			DispatchMessage(&message);

  			}	while (msg_return_value);

			Vis_Enable_Rendering = 0;

			timeKillEvent(vis_timer);
			timeEndPeriod(250);

			EnterCriticalSection(&cslock);

			if(gs_vWinAmpProps[id].pModule)
				gs_vWinAmpProps[id].pModule->getModule(module)->Quit(gs_vWinAmpProps[id].pModule->getModule(module));


			Destroy_WA_Comp_Window();

			theThread		=	0;
			Vis_hThread		=	0;
			dwVis_hThreadId =	0;
			LeaveCriticalSection(&cslock);
			
			DeleteCriticalSection(&cslock);

		}

		Destroy_WA_Comp_Window();

			theThread		=	0;
			Vis_hThread		=	0;
			dwVis_hThreadId =	0;

			DeleteCriticalSection(&cslock);

		return 0;
	}
	Destroy_WA_Comp_Window();

			theThread		=	0;
			Vis_hThread		=	0;
			dwVis_hThreadId =	0;

			DeleteCriticalSection(&cslock);
	return 0;
}

void WINEXPORT BASS_WA_SetHwnd(HWND hwnd)
{

	mainhwnd = hwnd;

	tmpPropVis.hDll = NULL;
	tmpPropVis.NumberOfModules = 0;
	tmpPropVis.pModule = NULL;
	tmpPropVis.strExt = "";
	tmpPropVis.strFileName = "";
	
	for(int n=0; n<512; n++)
	{
		gs_vWinAmpProps[n] = tmpPropVis;
	}
}

HWND WINEXPORT BASS_WA_GetVisHwnd(void)
{
	HWND visHwnd;
	visHwnd = vis_Window_Emu;
	return visHwnd;
}

void WINEXPORT BASS_WA_SetSongTitle(LPTSTR TheTitle)
{
	SongTitle = TheTitle;
	SetWindowText(vis_Window_Emu, SongTitle);
}

void WINEXPORT BASS_WA_SetElapsed(int elapsed)
{
	Elapsed = elapsed;
}

void WINEXPORT BASS_WA_SetLength(int length)
{
	Length = length;
}

void WINEXPORT BASS_WA_IsPlaying(int playing)
{
	Vis_Enable_Rendering = playing;
}

void WINEXPORT BASS_WA_SetModule(int the_module)
{
	module = the_module;
}

LPSTR WINEXPORT BASS_WA_GetWinampPluginInfo(int i)
{	
	LPSTR strRet="";

	if(gs_vWinAmpProps[i].pModule)
	{
		if (gs_vWinAmpProps[i].pModule->getModule(0)->description != NULL)
		{
			strRet = gs_vWinAmpProps[i].pModule->description;
		}
	}
	return strRet;
}

UINT WINEXPORT BASS_WA_GetWinampPluginCount()
{
	int i = 0;
	while  (gs_vWinAmpProps[i].pModule)
		i++; 

	return i;
}

UINT WINEXPORT BASS_WA_GetModuleCount(int i)
{
	if(gs_vWinAmpProps[i].pModule)
		return gs_vWinAmpProps[i].NumberOfModules;
	else
		return 0;
}

LPSTR WINEXPORT BASS_WA_GetModuleInfo(int plugin, int the_module)
{
	LPTSTR ModuleInfo="";
	
	if(gs_vWinAmpProps[plugin].pModule)
		ModuleInfo = gs_vWinAmpProps[plugin].pModule->getModule(the_module)->description;

	return ModuleInfo;
}

void WINEXPORT BASS_WA_Config_Vis(int i, int module_index)
{
	if (i>=0) {
		gs_vWinAmpProps[i].pModule->getModule(module_index)->Config(gs_vWinAmpProps[i].pModule->getModule(module_index));
		

	}
}

void WINEXPORT BASS_WA_Start_Vis(int i, int hchan)
{
	if(theThread != 0 || Vis_hThread != 0 || dwVis_hThreadId != 0)
			return; 

		InitializeCriticalSection(&cslock);

	if (i>=0) {
		vis_Plugin_Samples = 1152;

		/*if (bass == 0)*/ LoadBass();

		hchannel=hchan;

		theThread = Vis_New_Thread_Init(i);
		if (!theThread)
		{
			MessageBox(mainhwnd,"Cannot initialize thread!","Error!", MB_ICONEXCLAMATION | MB_ICONWARNING);
		} 
	}
}

void WINEXPORT BASS_WA_Stop_Vis(int i)
{
	if (i>=0) {
		if (dwVis_hThreadId) {
			PostThreadMessage(dwVis_hThreadId, WM_QUIT, 0, 0);
		}
	}
}

void Release_Vis_Plugin(int cnt)
{
	if(gs_vWinAmpProps[cnt].pModule)
	{
		if (gs_vWinAmpProps[cnt].pModule->getModule(0)->hDllInstance!=0)
		{
			FreeLibrary(gs_vWinAmpProps[cnt].pModule->getModule(0)->hDllInstance);
		}
	}
}

bool WINEXPORT BASS_WA_LoadVisPlugin(LPCTSTR path)
{
	BASS_WA_FreeVisInfo();

	static int				currplug=0;
	WIN32_FIND_DATA			sFF = {0};
	HANDLE					hFind = NULL;
	char					m_strDir[255];
	char					m_strPath[255];
	char					s_strPath[255];
	LPTSTR				    dir = "Plugins\\";

	memset(&pVis,0,sizeof(pVis));
	strcpy(m_strDir,path);
	strcat(m_strDir,"\\vis_*.dll");
	hFind = FindFirstFile(m_strDir,&sFF); 	

	if (hFind == INVALID_HANDLE_VALUE)
	{
		return FALSE;
	} else
	{
		do {
			m_strPath[0] = '\0';
			strcat(m_strPath, path);
			strcat(m_strPath,sFF.cFileName);

			tmpPropVis.hDll = NULL;				
 			tmpPropVis.hDll = LoadLibrary(m_strPath);
			
			if (tmpPropVis.hDll)
				{
					char s[256];
					int cnt = 0;
					
					strcpy(s_strPath,dir);
					strcat(s_strPath,sFF.cFileName);

					WINAMPGETVISMODULE pGetMod = (WINAMPGETVISMODULE)
					GetProcAddress(tmpPropVis.hDll,
									"winampVisGetHeader");

    				tmpPropVis.pModule = pGetMod();
					
					tmpPropVis.strFileName = s_strPath;
					pVis = tmpPropVis.pModule->getModule(0);
					pVis->hDllInstance = tmpPropVis.hDll;
					pVis->hwndParent = mainhwnd;
					pVis->sRate = 44100;
					pVis->nCh = 2;

					while (tmpPropVis.pModule->getModule(cnt) > 0)
						cnt++;

					strcpy(s,tmpPropVis.pModule->description);

					gs_vWinAmpProps[currplug] = tmpPropVis;
					gs_vWinAmpProps[currplug].NumberOfModules = cnt;
					currplug++;

			} else return FALSE;

		} while (FindNextFile(hFind, &sFF));
		FindClose(hFind);
	}
	return TRUE;
}

void WINEXPORT BASS_WA_FreeVisInfo()
{
	int cnt=0;
	
	for ( cnt=0; cnt < (signed)BASS_WA_GetWinampPluginCount(); cnt++)
	{
		if(gs_vWinAmpProps[cnt].hDll)
		FreeLibrary(gs_vWinAmpProps[cnt].hDll);
	}

				
	tmpPropVis.hDll = NULL;
	tmpPropVis.NumberOfModules = 0;
	tmpPropVis.pModule = NULL;
	tmpPropVis.strExt = "";
	tmpPropVis.strFileName = "";
	
	for(int n=0; n<512; n++)
	{
		gs_vWinAmpProps[n] = tmpPropVis;
	}
			
}

void WINEXPORT BASS_WA_FreeVis(int i)
{
	if(gs_vWinAmpProps[i].hDll)
	FreeLibrary(gs_vWinAmpProps[i].hDll);
}

void WINEXPORT BASS_WA_LoadVis(int i)
{
	BASS_WA_FreeVisInfo();
	LoadWinampPlugin(gs_vWinAmpProps[i].strFileName, i);
}

void WINEXPORT BASS_WA_SetChannel(int hchan)
{
	hchannel=hchan;
	if (bass == 0) LoadBass();
	BASS_ChannelGetInfo(hchannel,&info);
}

void  WINEXPORT BASS_WA_LoadAllVis()
{
	int cnt=0;
	
	BASS_WA_FreeVisInfo();
	for (cnt=0; cnt < (signed)BASS_WA_GetWinampPluginCount(); cnt++)
	{
		
		LoadWinampPlugin(gs_vWinAmpProps[cnt].strFileName, cnt);
	}
}

void  LoadWinampPlugin(LPCSTR path, int currplug)
{
	int cnt = 0;

	pVis = NULL;
	tmpPropVis.hDll = LoadLibrary(path);
	
	if (tmpPropVis.hDll)
		{
		WINAMPGETVISMODULE pGetMod = (WINAMPGETVISMODULE)
			GetProcAddress(tmpPropVis.hDll,
							"winampVisGetHeader");
			
			tmpPropVis.pModule = pGetMod();
			tmpPropVis.strFileName = path;
			pVis = tmpPropVis.pModule->getModule(0);
			pVis->hDllInstance = tmpPropVis.hDll;					
			pVis->hwndParent = mainhwnd;								
			pVis->sRate = 44100;
			pVis->nCh = 2;

			while (tmpPropVis.pModule->getModule(cnt) > 0)
				cnt++;

			gs_vWinAmpProps[currplug] = tmpPropVis;
			gs_vWinAmpProps[currplug].NumberOfModules = cnt;
	}
}

long Cnv16to8 (signed short *source,signed char *dest,unsigned long samples)
{
	unsigned long i;

	for (i=0;i<samples;i=i+2)
	{
		dest[i>>1]=source[i]>>8;
	}
	return i;
}

