Imports System.Runtime.InteropServices

Public Module ShellEvents

	Public Event WindowCreated(NewWindow As WindowInfo)
	Public Event WindowDestroyed(OldWindowHandle As IntPtr)
	Public Event WindowActivated(Window As WindowInfo, Fullscreen As Boolean)
	Public Event WindowReplaced(OldWindow As WindowInfo, NewWindow As WindowInfo)
	Public Event ActivateShellWindow()
	Public Event WindowTitleChange(Window As WindowInfo, Flash As Boolean)
	Public Event OpenTaskManager(ByRef Cancel As Boolean)
	Public Event ApplicationCommand(DestinationWindow As WindowInfo, Command As ApplicationCommandType, ByRef Cancel As Boolean)
	Public Event GetMinimizedRect(Window As WindowInfo, ByRef MinRect As Rectangle)

	Private Const RSH_UNREGISTER As Integer = 0
	Private Const RSH_REGISTER As Integer = 1
	Private Const RSH_REGISTER_PROGMAN As Integer = 2
	Private Const RSH_REGISTER_TASKMAN As Integer = 3

	'Private Declare Auto Function SetWindowsHookEx Lib "user32.dll" (idHook As Integer, lpfn As HookCallBack, hMod As Integer, dwThreadID As Integer) As Integer
	Private Declare Auto Function UnhookWindowsHookEx Lib "user32.dll" (hhk As Integer) As Integer
	Private Declare Auto Function CallNextHookEx Lib "user32.dll" (hhk As Integer, nCode As Integer, wParam As Integer, lParam As Integer) As Integer
	Private Declare Auto Function RegisterShellHookWindow Lib "user32.dll" (hwnd As IntPtr) As Boolean
	Private Declare Auto Function SetTaskmanWindow Lib "user32.dll" (hwnd As IntPtr) As Boolean
	Private Declare Auto Function SetProgmanWindow Lib "user32.dll" (hwnd As IntPtr) As Boolean
	Private Declare Auto Function RegisterShellHook Lib "shell32.dll" Alias "#181" (hwnd As IntPtr, flags As Integer) As Boolean
	Private Declare Auto Function RegisterWindowMessage Lib "user32.dll" (lpString As String) As Integer
	Private Declare Auto Function DeregisterShellHookWindow Lib "user32.dll" (hwnd As IntPtr) As Boolean

	Dim hookId As Integer
	Dim hookWin As NativeWindowEx
	Dim WM_SHELLHOOKMESSAGE As Integer

	Public Sub InitShellEvents()
		Try
			hookWin = New NativeWindowEx()
			hookWin.CreateHandle(New CreateParams)

			'*** We don't need this any more since we use RegisterShellHook
			'*** instead of RegisterShellHookWindow
			'
			'Set the invisible window as a taskman window to allow for RegisterShellHookWindow
			'If SetTaskmanWindow(hookWin.Handle) = False Then
			'Dim ex As New System.ComponentModel.Win32Exception
			'Debug.Print("SetTaskmanWindow: " & ex.Message)
			'End If

			'Register to receive shell-related events
			If RegisterShellHookWindow(hookWin.Handle) = False Then
				Throw New System.ComponentModel.Win32Exception
			Else
				'No error occurred
				WM_SHELLHOOKMESSAGE = RegisterWindowMessage("SHELLHOOK")
				AddHandler hookWin.MessageRecieved, AddressOf ShellWinProc
			End If
		Catch ex As Exception
			Debug.Print(ex.Message)
			Throw ex
		End Try
	End Sub

	Public Sub UninitShellEvents()
		RegisterShellHook(hookWin.Handle, RSH_UNREGISTER)
	End Sub

	Dim replacingWindow As IntPtr

	Private Const WM_COPYDATA As Integer = &H4A

	Private Sub ShellWinProc(ByRef m As Message)
		Try
			If m.Msg = WM_SHELLHOOKMESSAGE Then
				Select Case m.WParam
					Case HSHELL_WINDOWCREATED
						If New WindowInfo(m.LParam).IsValid Then RaiseEvent WindowCreated(New WindowInfo(m.LParam))
					Case HSHELL_WINDOWDESTROYED
						If New WindowInfo(m.LParam).IsValid Then RaiseEvent WindowDestroyed(m.LParam)
					Case HSHELL_WINDOWREPLACING
						replacingWindow = m.LParam
					Case HSHELL_WINDOWREPLACED
						If New WindowInfo(replacingWindow).IsValid AndAlso New WindowInfo(m.LParam).IsValid Then RaiseEvent WindowReplaced(New WindowInfo(replacingWindow), New WindowInfo(m.LParam))
					Case HSHELL_WINDOWACTIVATED
						If New WindowInfo(m.LParam).IsValid Then RaiseEvent WindowActivated(New WindowInfo(m.LParam), False)
					Case HSHELL_FLASH
						If New WindowInfo(m.LParam).IsValid Then RaiseEvent WindowTitleChange(New WindowInfo(m.LParam), True)
					Case HSHELL_RUDEAPPACTIVATED
						If New WindowInfo(m.LParam).IsValid Then RaiseEvent WindowActivated(New WindowInfo(m.LParam), False)
					Case HSHELL_REDRAW
						If New WindowInfo(m.LParam).IsValid Then RaiseEvent WindowTitleChange(New WindowInfo(m.LParam), False)
					Case HSHELL_APPCOMMAND
						Dim cancel As Boolean
						RaiseEvent ApplicationCommand(Nothing, CType(CInt(m.LParam) >> 8, ApplicationCommandType), cancel)
						Debug.Print(CType(CInt(m.LParam) >> 8, ApplicationCommandType).ToString)
					Case HSHELL_ACTIVATESHELLWINDOW
						RaiseEvent ActivateShellWindow()
					Case HSHELL_TASKMAN
						Dim cancel As Boolean
						RaiseEvent OpenTaskManager(cancel)
					Case HSHELL_GETMINRECT
						Dim winHandle As SHELLHOOKINFO = Runtime.InteropServices.Marshal.PtrToStructure(m.LParam, GetType(SHELLHOOKINFO))
						'Dim ptr As IntPtr = Runtime.InteropServices.Marshal.AllocHGlobal(Marshal.SizeOf(GetType(RECT)))
						'Marshal.StructureToPtr(winHandle.rc, ptr, True)
						'm.Result = ShellProc(HSHELL_GETMINRECT, winHandle.hwnd, ptr)
						'winHandle.rc = Marshal.PtrToStructure(ptr, GetType(RECT))
						'winHandle.rc = New RECT
						Dim wi As New WindowInfo(winHandle.hwnd)
						Debug.Print(wi.Text)
						winHandle.rc.top = 0
						winHandle.rc.left = 0
						winHandle.rc.right = 100
						winHandle.rc.bottom = 100
						Debug.Print(Rectangle.FromLTRB(winHandle.rc.left, winHandle.rc.top, winHandle.rc.right, winHandle.rc.bottom).ToString)
						Marshal.StructureToPtr(winHandle, m.LParam, True)
						m.Result = winHandle.hwnd
						'Marshal.FreeHGlobal(ptr)
				End Select
			End If
		Catch ex As Exception
			Debug.Print("An error occured in ShellWinProc: " & ex.Message)
		End Try
	End Sub

	<Runtime.InteropServices.StructLayout(Runtime.InteropServices.LayoutKind.Sequential)> _
	Private Structure RECT
		Public left As Integer
		Public top As Integer
		Public right As Integer
		Public bottom As Integer
	End Structure

	<Runtime.InteropServices.StructLayout(Runtime.InteropServices.LayoutKind.Sequential)> _
	Private Structure SHELLHOOKINFO
		Public hwnd As IntPtr
		Public rc As RECT
	End Structure

	Private Const WH_SHELL As Integer = 10

	Private Const HSHELL_WINDOWCREATED As Integer = 1
	Private Const HSHELL_WINDOWDESTROYED As Integer = 2
	Private Const HSHELL_ACTIVATESHELLWINDOW As Integer = 3

	'Windows NT
	Private Const HSHELL_WINDOWACTIVATED As Integer = 4
	Private Const HSHELL_GETMINRECT As Integer = 5
	Private Const HSHELL_REDRAW As Integer = 6
	Private Const HSHELL_TASKMAN As Integer = 7
	Private Const HSHELL_LANGUAGE As Integer = 8
	Private Const HSHELL_SYSMENU As Integer = 9
	Private Const HSHELL_ENDTASK As Integer = 10
	'Windows 2000
	Private Const HSHELL_ACCESSIBILITYSTATE As Integer = 11
	Private Const HSHELL_APPCOMMAND As Integer = 12

	'Windows XP
	Private Const HSHELL_WINDOWREPLACED As Integer = 13
	Private Const HSHELL_WINDOWREPLACING As Integer = 14

	Private Const HSHELL_HIGHBIT As Integer = &H8000
	Private Const HSHELL_FLASH As Integer = (HSHELL_REDRAW Or HSHELL_HIGHBIT)
	Private Const HSHELL_RUDEAPPACTIVATED As Integer = (HSHELL_WINDOWACTIVATED Or HSHELL_HIGHBIT)

	Public Enum ApplicationCommandType As Integer
		'Windows 2000
		BrowserBackward = 1
		BrowserForward = 2
		BrowserRefresh = 3
		BrowserStop = 4
		BrowserSearch = 5
		BrowserFavorites = 6
		BrowserHome = 7
		VolumeMute = 8
		VolumeDown = 9
		VolumeUp = 10
		MediaNextTrack = 11
		MediaPreviousTrack = 12
		MediaStop = 13
		MediaPlayPause = 14
		LaunchMail = 15
		LaunchMediaSelect = 16
		LaunchApp1 = 17
		LaunchApp2 = 18
		BassDown = 19
		BassBoost = 20
		BassUp = 21
		TrebleDown = 22
		TrebleUp = 23
		'Windows XP
		MicrophoneVolumeMute = 24
		MicrophoneVolumeDown = 25
		MicrophoneVolumeUp = 26
		Help = 27
		Find = 28
		[New] = 29
		Open = 30
		Close = 31
		Save = 32
		Print = 33
		Undo = 34
		Redo = 35
		Copy = 36
		Cut = 37
		Paste = 38
		ReplyToMail = 39
		ForwardMail = 40
		SendMail = 41
		SpellCheck = 42
		DictateOrCommandControlToggle = 43
		MicOnOffToggle = 44
		CorrectionList = 45
		MediaPlay = 46
		MediaPause = 47
		MediaRecord = 48
		MediaFastForward = 49
		MediaRewind = 50
		MediaChannelUp = 51
		MediaChannelDown = 52

		'Windows Vista
		Delete = 53
		DWMFlip3D = 54
	End Enum

End Module
