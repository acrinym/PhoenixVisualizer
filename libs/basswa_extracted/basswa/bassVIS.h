#ifndef BASS_H
#define BASS_H
#ifdef __cplusplus
extern "C" {
#endif

#ifndef BASSDEF
#define BASSDEF(f) WINAPI f
#endif

#define BASS_DATA_AVAILABLE	0			
#define BASS_DATA_FFT512	0x80000000	
#define BASS_DATA_FFT1024	0x80000001	
#define BASS_DATA_FFT2048	0x80000002	
#define BASS_DATA_FFT4096	0x80000003	
#define BASS_DATA_FFT_INDIVIDUAL 0x10	
#define BASS_DATA_FFT_NOWINDOW	0x20
	
#define BASS_ACTIVE_STOPPED	0
#define BASS_ACTIVE_PLAYING	1
#define BASS_ACTIVE_STALLED	2
#define BASS_ACTIVE_PAUSED	3

typedef struct {
	DWORD freq;		
	DWORD chans;	
	DWORD flags;	
	DWORD ctype;	
} BASS_CHANNELINFO;

typedef struct {
	DWORD size;		
	DWORD flags;	
	DWORD hwsize;	
	DWORD hwfree;	
	DWORD freesam;	
	DWORD free3d;	
	DWORD minrate;	
	DWORD maxrate;	
	BOOL eax;	
	DWORD minbuf;
	DWORD dsver;	
	DWORD latency;	
	DWORD initflags;
	DWORD speakers; 
	char *driver;	
} BASS_INFO;

typedef DWORD HDSP;	

typedef void (CALLBACK DSPPROC)(HDSP handle, DWORD channel, void *buffer, DWORD length, DWORD user);

DWORD BASSDEF(BASS_ChannelGetData)(DWORD handle, void *buffer, DWORD length);
DWORD BASSDEF(BASS_ChannelIsActive)(DWORD handle);
BOOL BASSDEF(BASS_ChannelGetInfo)(DWORD handle, BASS_CHANNELINFO *info);

#ifdef __cplusplus
}
#endif

#endif
