unit Unit1;

interface

uses
  Windows, Messages, SysUtils, Classes, Graphics, Controls, Forms, Dialogs,
  StdCtrls, Dynamic_BASS, bass_wa;

type
  TForm1 = class(TForm)
    btnOpen: TButton;
    GroupBox1: TGroupBox;
    PluginList: TListBox;
    ComboBox1: TComboBox;
    btnStart: TButton;
    btnStop: TButton;
    btnConfig: TButton;
    OpenDialog1: TOpenDialog;
    procedure FormShow(Sender: TObject);
    procedure btnOpenClick(Sender: TObject);
    procedure FormClose(Sender: TObject; var Action: TCloseAction);
    procedure PluginListClick(Sender: TObject);
    procedure btnStartClick(Sender: TObject);
    procedure btnStopClick(Sender: TObject);
    procedure btnConfigClick(Sender: TObject);
    procedure PluginListDblClick(Sender: TObject);
  private
    { Private declarations }
    procedure Stop_VisPlg;
  public
    { Public declarations }
  end;

var
  Form1: TForm1;

implementation

{$R *.DFM}


var
   PlayerReady : boolean = false;
   chan : Longword = 0;
   ProgDir : string;
   PluginInfo : string;
   PluginCount : integer;
   CurrentPlugin : integer;

procedure TForm1.FormShow(Sender: TObject);
var
   i : integer;
begin
   ProgDir := ExtractFilePath(ParamStr(0));
   if not Load_BASSDLL(ProgDir + 'bass.dll') then
   begin
      Application.MessageBox('BASS.DLL was not loaded !', 'Confirm', MB_OK);
      exit;
   end;

   if (BASS_GetVersion <> MAKELONG(2,0)) then
   begin
     Application.MessageBox('BASS version is not 2.0 !', 'Confirm', MB_OK);
     exit;
   end;

 // setup output - default device, 44100Hz
   if not BASS_Init(1, 44100, 0, Application.Handle, nil) then
   begin
      Application.MessageBox('Can''t initialize device !', 'Confirm', MB_OK);
      exit;
   end;

   if not Load_BASS_WA_DLL(ProgDir + 'bass_wa.dll') then
   begin
      Application.MessageBox('BASS_WA.DLL was not loaded !', 'Confirm', MB_OK);
      exit;
   end;

   BASS_WA_SetHwnd(Form1.Handle);
   BASS_WA_LoadVisPlugin(ProgDir + 'Plugins\');
   PluginCount := BASS_WA_GetWinampPluginCount;
   for i := 0 to PluginCount - 1 do
   begin
      PluginInfo := StrPas(BASS_WA_GetWinampPluginInfo(i));
      PluginList.Items.Add(PluginInfo);
   end;

   BASS_WA_FreeVisInfo;

   if PluginCount > 0 then
      PluginList.ItemIndex := 0;
   CurrentPlugin := -1;

   PlayerReady := true;

   PluginListClick(Sender);
   PluginList.SetFocus;
end;

procedure TForm1.FormClose(Sender: TObject; var Action: TCloseAction);
begin
   Stop_VisPlg;
   
   if PlayerReady then
      BASS_Free;

   Unload_BASSDLL;
   Unload_BASS_WA_DLL;
end;

procedure TForm1.btnOpenClick(Sender: TObject);
var
   ChanInfo : BASS_CHANNELINFO;
begin
   if not PlayerReady then
   begin
     Application.MessageBox('Player is not ready !', 'Confirm', MB_OK);
     exit;
   end;

   OpenDialog1.FileName := '';
   OpenDialog1.Filter := 'Music files|*.mp3;*.mp2;*.mp1;*.ogg;*.wav|All files|*.*';
   if not OpenDialog1.Execute then
      exit;

   if chan <> 0 then
   begin
      BASS_MusicFree(chan);
      BASS_StreamFree(chan);
   end;

   chan := BASS_StreamCreateFile(FALSE, PChar(OpenDialog1.FileName), 0, 0, 0);
   if (chan = 0) then
      chan := BASS_MusicLoad(FALSE, PChar(OpenDialog1.FileName), 0, 0, BASS_MUSIC_LOOP + BASS_MUSIC_RAMP, 0);
   if (chan = 0) then
   begin
      btnOpen.Caption := 'click here to open a file...';
      Application.MessageBox('Can''t play the file', 'Confirm', MB_OK);
      exit;
   end;

   BASS_ChannelGetInfo(chan, ChanInfo);
   if ChanInfo.chans <> 2 Then
   begin
      btnOpen.Caption := 'click here to open a file...';
      Application.MessageBox('only stereo sources are supported', 'Confirm', MB_OK);
      exit;
   end;

   BASS_WA_SetChannel(chan);

   BASS_MusicPlay(chan);
   BASS_StreamPlay(chan, false, BASS_SAMPLE_LOOP);
   btnOpen.Caption := OpenDialog1.FileName;
end;

procedure TForm1.PluginListClick(Sender: TObject);
var
   ItemIndex : integer;
   NumOfModules : integer;
   cntModule : integer;
   ModuleInfo : string;
begin
   ComboBox1.Clear;
   ItemIndex := PluginList.ItemIndex;
   BASS_WA_LoadVis(ItemIndex);

   NumOfModules := BASS_WA_GetModuleCount(ItemIndex);

   for cntModule := 0 to NumOfModules - 1 do
   begin
      ModuleInfo := StrPas(BASS_WA_GetModuleInfo(ItemIndex, cntModule));
      ComboBox1.Items.Add(ModuleInfo);
   end;

    ComboBox1.ItemIndex := 0;
end;

procedure TForm1.Stop_VisPlg;
begin
   if CurrentPlugin = -1 then
      exit;

   BASS_WA_Stop_Vis(CurrentPlugin);
   BASS_WA_FreeVis(CurrentPlugin);
   Sleep(100);

   CurrentPlugin := -1;
end;

procedure TForm1.btnStartClick(Sender: TObject);
var
   ItemIndex : integer;
   module_index : integer;
begin
   Stop_VisPlg;

   ItemIndex := PluginList.ItemIndex;
   module_index := ComboBox1.ItemIndex;

   BASS_WA_SetModule(module_index);
   BASS_WA_IsPlaying(1{True});

   CurrentPlugin := ItemIndex;
   BASS_WA_LoadVis(ItemIndex);
   BASS_WA_Start_Vis(ItemIndex, chan);
end;

procedure TForm1.btnStopClick(Sender: TObject);
begin
   Stop_VisPlg;
end;

procedure TForm1.btnConfigClick(Sender: TObject);
var
   ItemIndex : integer;
begin
   ItemIndex := PluginList.ItemIndex;
   BASS_WA_Config_Vis(ItemIndex, ComboBox1.ItemIndex);
end;

procedure TForm1.PluginListDblClick(Sender: TObject);
begin
   btnConfigClick(Sender);
end;

end.
