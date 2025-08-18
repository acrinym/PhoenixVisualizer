#ifdef __cplusplus
extern "C" {
#endif

winampVisModule* pVis;

CRITICAL_SECTION cslock;

HWND			hWnd;
#define			WINEXPORT WINAPI 
#define			MAX_PLUGINS 512

typedef winampVisHeader*	(*WINAMPGETVISMODULE)(void);
WINAMPPLUGINPROPVIS			gs_vWinAmpProps[MAX_PLUGINS];
WINAMPPLUGINPROPVIS			tmpPropVis;

enum
{
	OSCILLOSCOPE,
	SPECTRUM

}VIS_MODE;
int		id=-1;							
int		module=0;		

static HANDLE	Vis_hThread=0;				
HINSTANCE		hinstance=0;					
HANDLE			theThread=0;			 
DWORD			dwVis_hThreadId=0;
HWND			vis_Window_Emu = NULL;	
UINT			Vis_Enable_Rendering = 0;	
	
LPTSTR			SongTitle="No title";
int				Elapsed,Length;

static			HINSTANCE inst;				
HWND			mainhwnd=0;

int				hchannel=0;

#define WA_USER_GETTIME			105
#define WA_USER_ISPLAYING		104
#define WA_USER_GETVERSION		0
#define WA_USER_STARTPLAY		102
#define WA_USER_GETINFO			126
#define WA_USER_GETLISTLENGTH	124
#define WA_USER_GETLISTPOS		125
#define WA_USER_GETPLAYLISTFILE	211
#define WA_USER_GETPLAYLISTTITLE	212

#define WM_WA_IPC WM_USER


LRESULT CALLBACK WinampWndProc(HWND , UINT , WPARAM , LPARAM);
static void CALLBACK vis_time_event(UINT uID, UINT uMsg, DWORD dwUser, DWORD dw1, DWORD dw2);

HANDLE	Vis_New_Thread_Init(int);
DWORD	WINAPI BASS_WA_New_Thread(LPVOID);
void	Create_Winamp_Window(void);
void	LoadBass(void);
void	Destroy_WA_Comp_Window(void);
void	Release_Vis_Plugin(int);
bool	WINEXPORT BASS_WA_LoadVisPlugin(LPCTSTR path);
void	LoadWinampPlugin(LPCSTR path,int currplug);
void	WINEXPORT BASS_WA_Start_Vis(int,int);
void	WINEXPORT BASS_WA_Stop_Vis(int);
void	WINEXPORT BASS_WA_Config_Vis(int,int);
void	WINEXPORT BASS_WA_LoadAllVis(void);
void	WINEXPORT BASS_WA_LoadVis(int);
void	WINEXPORT BASS_WA_FreeVis(int);
void	WINEXPORT BASS_WA_FreeVisInfo(void);
UINT	WINEXPORT BASS_WA_GetModuleCount(int);
LPSTR	WINEXPORT BASS_WA_GetModuleInfo(int,int);
UINT	WINEXPORT BASS_WA_GetWinampPluginCount(void);

void	WINEXPORT BASS_WA_SetHwnd(HWND);
HWND	WINEXPORT BASS_WA_GetVisHwnd(void);
void	WINEXPORT BASS_WA_SetSongTitle(LPTSTR);
void	WINEXPORT BASS_WA_SetElapsed(int);
void	WINEXPORT BASS_WA_SetLength(int);
void	WINEXPORT BASS_WA_IsPlaying(int);
void	WINEXPORT BASS_WA_SetModule(int);
LPSTR	WINEXPORT BASS_WA_GetWinampPluginInfo(int);
void	WINEXPORT BASS_WA_SetChannel(int);

#define BUFFERSIZE			(44100 * 25) / 1000
#define SINGLE_BUFFER_SIZE  BUFFERSIZE << 2

UINT					vis_Plugin_Samples=1152;

static signed short		pcmBuffer[1152];
static signed short		fftBuffer[2048];

long Cnv16to8 (signed short *,signed char *,unsigned long);

#ifdef __cplusplus
}
#endif
