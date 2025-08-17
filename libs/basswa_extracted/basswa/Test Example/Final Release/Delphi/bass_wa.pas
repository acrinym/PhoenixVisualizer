unit bass_wa;

interface

uses windows;

var
  BASS_WA_LoadVisPlugin : function(path : string) : boolean; stdcall;
  BASS_WA_LoadAllVis : procedure; stdcall;
  BASS_WA_LoadVis : procedure(index : Longint); stdcall;
  BASS_WA_GetWinampPluginCount : function : Longint; stdcall;
  BASS_WA_GetModuleCount : function(index : Longint) : Longint; stdcall;
  BASS_WA_GetModuleInfo : function(plugin : Longint; the_module : Longint) : pchar; stdcall;
  BASS_WA_FreeVisInfo : procedure; stdcall;
  BASS_WA_FreeVis : procedure(index : Longint); stdcall;
  BASS_WA_GetWinampPluginInfo : function(index : Longint) : pchar; stdcall;
  BASS_WA_Start_Vis : procedure(i : Longint; hchan : Longint); stdcall;
  BASS_WA_Stop_Vis : procedure(i : Longint); stdcall;
//  BASS_WA_StopModule : procedure(plugin : Longint; the_module : Longint); stdcall;
  BASS_WA_Config_Vis : procedure(i : Longint; module_index : Longint); stdcall;
  BASS_WA_SetModule : procedure(the_module : Longint); stdcall;
  BASS_WA_SetSongTitle : procedure(the_title : string); stdcall;
  BASS_WA_SetElapsed : procedure(elapsed : Longint); stdcall;
  BASS_WA_SetLength : procedure(elapsed : Longint); stdcall;
  BASS_WA_IsPlaying : procedure(playing : Longint); stdcall;
  BASS_WA_SetHwnd : procedure(the_hwnd : Longint); stdcall;
  BASS_WA_SetChannel : procedure(hchan : Longint); stdcall;

var BASS_WA_Handle : Thandle = 0;

function Load_BASS_WA_DLL(const dllfilename : string) : boolean;
procedure Unload_BASS_WA_DLL;


implementation

function Load_BASS_WA_DLL(const dllfilename : string) : boolean;
var
   oldmode : integer;
begin
   if BASS_WA_Handle <> 0 then // is it already there ?
      result := true
   else begin {go & load the dll}
     oldmode := SetErrorMode($8001);
     BASS_WA_Handle := LoadLibrary(pchar(dllfilename));  // obtain the handle we want
     SetErrorMode(oldmode);
     if BASS_WA_Handle <> 0 then
     begin
     // now we tie the functions to the VARs from above
       @BASS_WA_LoadVisPlugin := GetProcAddress(BASS_WA_Handle, 'BASS_WA_LoadVisPlugin');
       @BASS_WA_LoadAllVis := GetProcAddress(BASS_WA_Handle, 'BASS_WA_LoadAllVis');
       @BASS_WA_LoadVis := GetProcAddress(BASS_WA_Handle, 'BASS_WA_LoadVis');
       @BASS_WA_GetWinampPluginCount := GetProcAddress(BASS_WA_Handle, 'BASS_WA_GetWinampPluginCount');
       @BASS_WA_GetModuleCount := GetProcAddress(BASS_WA_Handle, 'BASS_WA_GetModuleCount');
       @BASS_WA_GetModuleInfo := GetProcAddress(BASS_WA_Handle, 'BASS_WA_GetModuleInfo');
       @BASS_WA_FreeVisInfo := GetProcAddress(BASS_WA_Handle, 'BASS_WA_FreeVisInfo');
       @BASS_WA_FreeVis := GetProcAddress(BASS_WA_Handle, 'BASS_WA_FreeVis');
       @BASS_WA_GetWinampPluginInfo := GetProcAddress(BASS_WA_Handle, 'BASS_WA_GetWinampPluginInfo');
       @BASS_WA_Start_Vis := GetProcAddress(BASS_WA_Handle, 'BASS_WA_Start_Vis');
       @BASS_WA_Stop_Vis := GetProcAddress(BASS_WA_Handle, 'BASS_WA_Stop_Vis');
    //   @BASS_WA_StopModule := GetProcAddress(BASS_WA_Handle, 'BASS_WA_StopModule');
       @BASS_WA_Config_Vis := GetProcAddress(BASS_WA_Handle, 'BASS_WA_Config_Vis');
       @BASS_WA_SetModule := GetProcAddress(BASS_WA_Handle, 'BASS_WA_SetModule');
       @BASS_WA_SetSongTitle := GetProcAddress(BASS_WA_Handle, 'BASS_WA_SetSongTitle');
       @BASS_WA_SetElapsed := GetProcAddress(BASS_WA_Handle, 'BASS_WA_SetElapsed');
       @BASS_WA_SetLength := GetProcAddress(BASS_WA_Handle, 'BASS_WA_SetLength');
       @BASS_WA_IsPlaying := GetProcAddress(BASS_WA_Handle, 'BASS_WA_IsPlaying');
       @BASS_WA_SetHwnd := GetProcAddress(BASS_WA_Handle, 'BASS_WA_SetHwnd');
       @BASS_WA_SetChannel := GetProcAddress(BASS_WA_Handle, 'BASS_WA_SetChannel');

     // check if everything is linked in correctly
       if (@BASS_WA_LoadVisPlugin = nil) or
          (@BASS_WA_LoadAllVis = nil) or
          (@BASS_WA_LoadVis = nil) or
          (@BASS_WA_GetWinampPluginCount = nil) or
          (@BASS_WA_GetModuleCount = nil) or
          (@BASS_WA_GetModuleInfo = nil) or
          (@BASS_WA_FreeVisInfo = nil) or
          (@BASS_WA_FreeVis = nil) or
          (@BASS_WA_GetWinampPluginInfo = nil) or
          (@BASS_WA_Start_Vis = nil) or
          (@BASS_WA_Stop_Vis = nil) or
      //    (@BASS_WA_StopModule = nil) or
          (@BASS_WA_Config_Vis = nil) or
          (@BASS_WA_SetModule = nil) or
          (@BASS_WA_SetSongTitle = nil) or
          (@BASS_WA_SetElapsed = nil) or
          (@BASS_WA_SetLength = nil) or
          (@BASS_WA_IsPlaying = nil) or
          (@BASS_WA_SetHwnd = nil) or
          (@BASS_WA_SetChannel = nil) then
         begin
          FreeLibrary(BASS_WA_Handle);
          BASS_WA_Handle := 0;
         end;
     end;

     result := (BASS_WA_Handle <> 0);
   end;
end;

procedure Unload_BASS_WA_DLL;
begin
   if BASS_WA_Handle <> 0 then
      FreeLibrary(BASS_WA_Handle);

   BASS_WA_Handle := 0;
end;

end.
