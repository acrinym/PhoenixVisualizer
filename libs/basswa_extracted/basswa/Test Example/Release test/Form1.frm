VERSION 5.00
Object = "{F9043C88-F6F2-101A-A3C9-08002B2F49FB}#1.2#0"; "COMDLG32.OCX"
Begin VB.Form Form1 
   BorderStyle     =   3  'Fixed Dialog
   Caption         =   "BASS WA Test"
   ClientHeight    =   2775
   ClientLeft      =   45
   ClientTop       =   435
   ClientWidth     =   5775
   LinkTopic       =   "Form1"
   MaxButton       =   0   'False
   MinButton       =   0   'False
   ScaleHeight     =   2775
   ScaleWidth      =   5775
   ShowInTaskbar   =   0   'False
   StartUpPosition =   3  'Windows Default
   Begin VB.Frame Frame1 
      Caption         =   "Visualisation plugins"
      Height          =   2055
      Left            =   120
      TabIndex        =   1
      Top             =   600
      Width           =   5535
      Begin VB.CommandButton Command4 
         Caption         =   "Start"
         Height          =   345
         Left            =   120
         TabIndex        =   6
         Top             =   1560
         Width           =   1485
      End
      Begin VB.CommandButton Command5 
         Caption         =   "Stop"
         Height          =   345
         Left            =   2040
         TabIndex        =   5
         Top             =   1560
         Width           =   1485
      End
      Begin VB.CommandButton Command6 
         Caption         =   "Config"
         Height          =   345
         Left            =   3960
         TabIndex        =   4
         Top             =   1560
         Width           =   1485
      End
      Begin VB.ComboBox Combo1 
         Height          =   315
         Left            =   120
         Style           =   2  'Dropdown List
         TabIndex        =   3
         Top             =   1080
         Width           =   5315
      End
      Begin VB.ListBox List3 
         Height          =   840
         Left            =   120
         TabIndex        =   2
         Top             =   240
         Width           =   5295
      End
   End
   Begin MSComDlg.CommonDialog cmd 
      Left            =   7080
      Top             =   4680
      _ExtentX        =   847
      _ExtentY        =   847
      _Version        =   393216
   End
   Begin VB.CommandButton cmdOpen 
      Caption         =   "click here to open a file..."
      Height          =   375
      Left            =   120
      TabIndex        =   0
      Top             =   120
      Width           =   5535
   End
End
Attribute VB_Name = "Form1"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Option Explicit

Dim chan As Long
Dim module_index As Long
Dim started As Boolean

Sub Error_(ByVal Message As String)
    Call MsgBox(Message & vbCrLf & vbCrLf & "Error Code : " & BASS_ErrorGetCode, vbExclamation, "Error")
End Sub

Private Sub cmdOpen_Click()
    On Error Resume Next
    
    cmd.CancelError = True
    cmd.flags = cdlOFNExplorer Or cdlOFNFileMustExist Or cdlOFNHideReadOnly
    cmd.DialogTitle = "Open"
    cmd.Filter = "Music files|*.mp3;*.mp2;*.mp1;*.ogg;*.wav|All files|*.*"
    cmd.ShowOpen
    
    If Err.Number = 32755 Then Exit Sub

    Call BASS_MusicFree(chan)
    Call BASS_StreamFree(chan)
    
    chan = BASS_StreamCreateFile(BASSFALSE, cmd.FileName, 0, 0, 0)
    If (chan = 0) Then chan = BASS_MusicLoad(BASSFALSE, cmd.FileName, 0, 0, BASS_MUSIC_LOOP Or BASS_MUSIC_RAMP, 0)
    If (chan = 0) Then
        cmdOpen.Caption = "click here to open a file..."
        Call Error_("Can't play the file")
        Exit Sub
    End If
    
    Dim ChanInfo As BASS_CHANNELINFO
    Call BASS_ChannelGetInfo(chan, ChanInfo)
    If ChanInfo.chans <> 2 Then
        cmdOpen.Caption = "click here to open a file..."
        Call Error_("only stereo sources are supported")
        Exit Sub
    End If
    
    Call BASS_WA_SetChannel(chan)
    
    Call BASS_MusicPlay(chan)
    Call BASS_StreamPlay(chan, 0, BASS_SAMPLE_LOOP)
End Sub

Private Sub Form_Load()
Dim WinampInfo As String
Dim lptstrString As Long

Dim lptstrStringDsp As Long

Dim ModuleInfo As String
Dim lpModuleInfo As Long
Dim cntModule As Long
Dim NumOfModules As Long
Dim i As Long
Dim lpStr As String
    
started = False
    
    ChDrive App.path
    ChDir App.path
 
    If FileExists(RPP(App.path) & "bass.dll") = False Then
        Call Error_("BASS.DLL does not exists")
        End
    End If
 
    If BASS_GetVersion <> MakeLong(2, 0) Then
        Call Error_("BASS version 2.0 was not loaded")
        End
    End If

    If (BASS_Init(1, 44100, 0, Me.hWnd, 0) = 0) Then
        Call Error_("Can't initialize device")
        Unload Me
    End If

    Call BASS_WA_SetHwnd(Form1.hWnd)

    Call BASS_WA_LoadVisPlugin(App.path & "\Plugins\")
    For i = 0 To BASS_WA_GetWinampPluginCount - 1
        lptstrString = BASS_WA_GetWinampPluginInfo(i)
        WinampInfo = GetStringFromPointer(lptstrString)
        List3.AddItem WinampInfo
    Next i
          
    If BASS_WA_GetWinampPluginCount > 0 Then List3.ListIndex = 0
    CurrentPlugin = -1
End Sub

Private Sub Form_Unload(Cancel As Integer)
    Call Stop_VisPlg
    Call BASS_Free
    Call BASS_WA_FreeVisInfo
    Unload Me
End Sub

Public Function FileExists(ByVal FileName As String) As Boolean
  On Local Error Resume Next
  FileExists = (Dir$(FileName) <> "")
End Function

Function RPP(ByVal fp As String) As String
    RPP = IIf(Mid(fp, Len(fp), 1) <> "\", fp & "\", fp)
End Function

Private Sub List3_Click()
Dim index As Long
Dim ModuleInfo As String
Dim lpModuleInfo As Long
Dim cntModule As Long
Dim NumOfModules As Long

    Combo1.Clear
    index = List3.ListIndex

    NumOfModules = BASS_WA_GetModuleCount(index)

    For cntModule = 0 To NumOfModules - 1
        lpModuleInfo = BASS_WA_GetModuleInfo(index, cntModule)
        ModuleInfo = GetStringFromPointer(lpModuleInfo)
        Combo1.AddItem ModuleInfo
    Next cntModule
        
    Combo1.ListIndex = 0
End Sub

Private Sub Command9_Click()
    Unload Me
End Sub

Private Sub Command4_Click()
    Stop_VisPlg
    Start_VisPlg
End Sub

Private Sub Start_VisPlg()
Dim index As Long


    

If (started = False) Then
    index = List3.ListIndex
    module_index = Combo1.ListIndex

    Call BASS_WA_SetModule(module_index)
    
    Call BASS_WA_Start_Vis(index, chan)
    Call BASS_WA_IsPlaying(1)
    started = True
Else
    Call BASS_WA_IsPlaying(0)
    index = List3.ListIndex

    Call BASS_WA_Stop_Vis(index)
    started = False
End If
End Sub

Private Sub Command5_Click()
    Call Stop_VisPlg
End Sub
 
Private Sub Stop_VisPlg()
Dim hindex As Long
    
    If (started = True) Then
    Call BASS_WA_IsPlaying(0)
    hindex = List3.ListIndex

    Call BASS_WA_Stop_Vis(hindex)
    started = False
    End If
    
End Sub

Private Sub Command6_Click()
Dim index As Long

    index = List3.ListIndex
    Call BASS_WA_Config_Vis(index, Combo1.ListIndex)
End Sub

Private Sub List3_DblClick()
    Command6_Click
End Sub


