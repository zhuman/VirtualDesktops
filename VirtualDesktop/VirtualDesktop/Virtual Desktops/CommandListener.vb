''' <summary>
''' Listens for commands from secondary instances of Finestra.
''' </summary>
''' <remarks></remarks>
Public Class CommandListener
	Inherits VirtualDesktopPlugin

	WithEvents _win As NativeWindowEx

	Private Declare Auto Function RegisterWindowMessage Lib "user32.dll" (lpString As String) As Integer

	Dim _cmdMsg As Integer
	Dim _cmdMsgProcess As Integer

	Public Sub New(vdm As VirtualDesktopManager)
		MyBase.New(vdm)
	End Sub

	Public Overrides Sub Start()
		_cmdMsg = RegisterWindowMessage("FinestraCommand")
		_cmdMsgProcess = RegisterWindowMessage("FinestraCommandAssignProcess")
		_win = New NativeWindowEx
		Dim cp As New CreateParams
		cp.Caption = "FinestraCommandListener"
		_win.CreateHandle(cp)
	End Sub

	Public Overrides Sub [Stop]()
		_win.DestroyHandle()
	End Sub

	Public Enum CommandCode
		SwitchDesktop
		AssignProcess
		ShowSwitcher
		ShowOptions
	End Enum

	''' <summary>
	''' Receives command line arguments from newly opened processes.
	''' </summary>
	''' <param name="m"></param>
	''' <remarks></remarks>
	Private Sub _win_MessageRecieved(ByRef m As System.Windows.Forms.Message) Handles _win.MessageRecieved
		Try
			Select Case m.Msg
				Case _cmdMsg
					Select Case CType(m.WParam, CommandCode)
						Case CommandCode.SwitchDesktop
							VirtualDesktopManager.SendGlobalCommand("SwitchDesktop#" & m.LParam.ToInt32)
						Case CommandCode.ShowSwitcher
							VirtualDesktopManager.SendGlobalCommand("ShowSwitcher")
						Case CommandCode.ShowOptions
							VirtualDesktopManager.SendGlobalCommand("ShowOptions")
					End Select
				Case _cmdMsgProcess
					VirtualDesktopManager.SendGlobalCommand("TempPinPid#" & m.LParam.ToInt64 & "#" & m.WParam.ToInt64)
			End Select
		Catch ex As Exception

		End Try
	End Sub

	''' <summary>
	''' Parses the command line arguments and sends commands to the main Finestra process.
	''' </summary>
	''' <remarks></remarks>
	Public Shared Sub SendCommands()
		Dim w As WindowInfo = WindowInfo.FindWindowByText("FinestraCommandListener")
		If Not w.IsValid Then
			MessageBox.Show("A running instance of Finestra Virtual Desktops could not be found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
			Exit Sub
		End If

		Dim args() As String = Environment.GetCommandLineArgs
		For Each s As String In args
			If s = args(0) Then Continue For
			s = s.Trim("""", " ")
			If s.StartsWith("-") OrElse s.StartsWith("/") Then
				If LCase(s.Substring(1)) = "switcher" Then
					w.SendMessage(RegisterWindowMessage("FinestraCommand"), CommandCode.ShowSwitcher, 0)
				ElseIf LCase(s.Substring(1)).StartsWith("desk:") Then
					Dim desk As Integer
					Dim str As String = s.Substring("-desk:".Length)
					If Not Integer.TryParse(str, desk) Then
						For Each d As String In My.Settings.DesktopNames
							If LCase(d) = LCase(str) Then
								desk = My.Settings.DesktopNames.IndexOf(d)
								Exit For
							End If
						Next
					Else
						desk -= 1
					End If
					w.SendMessage(RegisterWindowMessage("FinestraCommand"), CommandCode.SwitchDesktop, desk)
				ElseIf LCase(s.Substring(1)) = "options" Then
					w.SendMessage(RegisterWindowMessage("FinestraCommand"), CommandCode.ShowOptions, 0)
				End If
			ElseIf s.Contains(":") Then
				Dim pieces() As String = s.Split(":")
				If pieces.Length = 2 Then
					Dim desk As Integer
					If Not Integer.TryParse(pieces(0), desk) Then
						For Each d As String In My.Settings.DesktopNames
							If LCase(d) = LCase(pieces(0)) Then
								desk = My.Settings.DesktopNames.IndexOf(d)
								Exit For
							End If
						Next
					Else
						desk -= 1
					End If
					Dim pId As Integer
					If Not Integer.TryParse(pieces(1), pId) Then
						pId = Process.Start(pieces(1)).Id
					End If
					w.SendMessage(RegisterWindowMessage("FinestraCommandAssignProcess"), pId, desk)
				End If
			Else
				MessageBox.Show("Unknown command: " & s, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
			End If
		Next
	End Sub

End Class
