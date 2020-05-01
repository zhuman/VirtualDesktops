Imports System.Runtime.InteropServices

''' <summary>
''' Monitors for shell events on a new thread. Handles when new 
''' windows are opened and moves them to their proper desktop if 
''' configured in the ruleset.
''' </summary>
''' <remarks></remarks>
Public Class WindowWatcher
	Inherits VirtualDesktopPlugin

	ReadOnly _tempProcs As New Dictionary(Of Integer, Integer)
	Dim hookHandle As IntPtr

	Public Sub New(vdm As VirtualDesktopManager)
		MyBase.New(vdm)
	End Sub

	Public Overrides Sub Start()
		If My.Settings.EnableWindowWatcher Then
			AddHandler VirtualDesktopManager.GlobalCommand, AddressOf VDM_GlobalCommand

			Dim t As New Threading.Thread(AddressOf WindowWatcherProc)
			t.SetApartmentState(Threading.ApartmentState.STA)
			t.Start()
		End If
	End Sub

	Public Overrides Sub [Stop]()
		If appContext IsNot Nothing Then
			appContext.ExitThread()
		End If
	End Sub

	Private Sub VDM_GlobalCommand(command As String)
		If command.StartsWith("TempPinPid") Then
			Dim parts = command.Split("#")
			Dim pid, desk As Integer
			If parts.Length = 3 AndAlso Integer.TryParse(parts(1), pid) AndAlso Integer.TryParse(parts(2), desk) Then
				If _tempProcs.ContainsKey(pid) Then _tempProcs.Remove(pid)
				If desk >= 0 AndAlso desk < VirtualDesktopManager.Desktops.Count Then
					_tempProcs.Add(pid, desk)
					For Each d As VirtualDesktop In VirtualDesktopManager.Desktops
						If Not d.Active Then
							Dim wins As List(Of WindowInfo) = d.Windows
							Dim remWins As New ObjectModel.Collection(Of WindowInfo)
							SyncLock wins
								For Each w As WindowInfo In wins
									Try
										If w.ProcessId = pid Then
											w.State = WindowInfo.WindowState.Show
											remWins.Add(w)
										End If
									Catch ex As Exception

									End Try
								Next

								For Each w As WindowInfo In remWins
									d.Windows.Remove(w)
								Next
							End SyncLock
							remWins.Clear()
						End If
					Next
				End If
			End If
		End If
	End Sub

	Dim appContext As ApplicationContext

	Private Sub WindowWatcherProc()
		Threading.Thread.CurrentThread.Name = "Finestra: Window Watcher"

		Try
			appContext = New ApplicationContext
			Dim handler As WinEventCallback = AddressOf WinEventHandler
			hookHandle = SetWinEventHook(WinEventType.EVENT_OBJECT_SHOW, WinEventType.EVENT_OBJECT_FOCUS, IntPtr.Zero, handler, IntPtr.Zero, IntPtr.Zero, WinEventHookFlags.WINEVENT_OUTOFCONTEXT Or WinEventHookFlags.WINEVENT_SKIPOWNPROCESS)
			Windows.Forms.Application.Run(appContext)
			UnhookWinEvent(hookHandle)
			GC.KeepAlive(handler)
		Catch ex As Exception
			Debug.Print("Window watcher error: " & ex.Message)
		End Try
	End Sub

	Private Sub InternalHandleWindow(w As WindowInfo, desk As Integer, pname As String)
		If VirtualDesktopManager.CurrentDesktopIndex <> desk Then
			SyncLock VirtualDesktopManager.Desktops(desk).Windows
				VirtualDesktopManager.SendWindowToDesktop(w, desk, VirtualDesktopManager.CurrentDesktopIndex)
			End SyncLock
			VirtualDesktopManager.SendGlobalMessage("A window has been moved to " & VirtualDesktopManager.Desktops(desk).Name & ". Click this balloon to switch to it.",
			  ToolTipIcon.Info,
			  Sub()
		 VirtualDesktopManager.CurrentDesktopIndex = desk
	 End Sub)
		End If
	End Sub

	Private Sub HandleWindow(w As WindowInfo)
		Try
			Dim pName As String = VirtualDesktopManager.GetProcessName(w.ProcessId)
			Dim pId As Integer = w.ProcessId
			If VirtualDesktopManager.IsWindowValid(w, False, False, True) Then
				If _tempProcs.ContainsKey(pId) Then
					InternalHandleWindow(w, _tempProcs(pId), pName)
					Exit Sub
				End If

				For Each s As String In My.Settings.ProgramDesktops.Keys
					If s Like ("*" & pName & "*") OrElse pName Like ("*" & s & "*") Then
						Dim desk As Integer = CInt(My.Settings.ProgramDesktops(s))
						InternalHandleWindow(w, desk, pName)
						Exit Sub
					End If
				Next
			End If
		Catch ex As Exception
			Debug.Print("Error handling new window: " & ex.Message)
		End Try
	End Sub

	''' <summary>
	''' Receives window events from the accessibility API.
	''' </summary>
	''' <remarks></remarks>
	Private Sub WinEventHandler(handle As IntPtr, winEvent As Integer, hwnd As IntPtr, idObject As Integer, idChild As Integer, idEventThread As Integer, dwmsEventTime As Integer)
		If winEvent = WinEventType.EVENT_OBJECT_FOCUS OrElse winEvent = WinEventType.EVENT_OBJECT_SHOW Then
			Try
				'Debug.Print("Handling WinEvent: hwnd=" & hwnd.ToString & " event=" & winEvent.ToString)
				HandleWindow(hwnd)
			Catch ex As Exception
				Debug.Print("Window Watcher WinEventHandler exception: " & ex.Message)
			End Try
		End If
	End Sub

	Public ReadOnly Property TemporaryProcs() As Dictionary(Of Integer, Integer)
		Get
			Return _tempProcs
		End Get
	End Property

	Private Enum WinEventHookFlags As Integer
		WINEVENT_OUTOFCONTEXT = &H0	 'Events are ASYNC
		WINEVENT_SKIPOWNTHREAD = &H1  'Don't call back for events on installer's thread
		WINEVENT_SKIPOWNPROCESS = &H2  'Don't call back for events on installer's process
		WINEVENT_INCONTEXT = &H4  'Events are SYNC, this causes your dll to be injected into every process
	End Enum

	Public Enum WinEventType As Integer
		EVENT_OBJECT_SHOW = &H8002	'hwnd + ID + idChild is shown item
		EVENT_OBJECT_FOCUS = &H8005	 'hwnd + ID + idChild is focused item
	End Enum

	Private Delegate Sub WinEventCallback(handle As IntPtr, winEvent As Integer, hwnd As IntPtr, idObject As Integer, idChild As Integer, idEventThread As Integer, dwmsEventTime As Integer)
	Private Declare Auto Function SetWinEventHook Lib "user32.dll" (eventMin As Integer, eventMax As Integer, hmod As IntPtr, callback As WinEventCallback, processId As Integer, threadId As Integer, flags As Integer) As IntPtr
	Private Declare Auto Function UnhookWinEvent Lib "user32.dll" (handle As IntPtr) As Boolean

End Class
