''' <summary>
''' Implements a plug-in that automatically saves the hidden window list to the registry for automatic recovery in case of a full crash.
''' </summary>
''' <remarks></remarks>
Public Class AutoRecoverPlugin
	Inherits VirtualDesktopPlugin

	Public Sub New(vdm As VirtualDesktopManager)
		MyBase.New(vdm)
	End Sub

	Public Overrides Sub Start()
		AddHandler VirtualDesktopManager.VirtualDesktopSwitched, AddressOf VDM_DesktopSwitched
		If RunningValue Then
			Try
				Dim windows() As String = Microsoft.Win32.Registry.GetValue("HKEY_CURRENT_USER\Software\Finestra\Recover", "RecoverWindows", New String() {})
				For Each win In windows
					Dim hwnd As Int64
					If Int64.TryParse(win, hwnd) Then
						Dim winInf As New WindowInfo(New IntPtr(hwnd))
						If winInf.IsValid Then
							winInf.Visible = True
						End If
					End If
				Next
			Catch ex As Exception
				Debug.Print("Error recovering windows: " & ex.Message)
			End Try
		End If
		RunningValue = True
	End Sub

	Public Overrides Sub [Stop]()
		RemoveHandler VirtualDesktopManager.VirtualDesktopSwitched, AddressOf VDM_DesktopSwitched
		RunningValue = False
	End Sub

	Private Sub VDM_DesktopSwitched(prevDesk As Integer, newDesk As Integer)
		Dim wins As New List(Of String)
		For Each d In VirtualDesktopManager.Desktops
			If Not d.Active Then
				For Each w In d.Windows
					wins.Add(w.Handle.ToInt64.ToString)
				Next
			End If
		Next
		Microsoft.Win32.Registry.SetValue("HKEY_CURRENT_USER\Software\Finestra\Recover", "RecoverWindows", wins.ToArray, Microsoft.Win32.RegistryValueKind.MultiString)
	End Sub

	Private Property RunningValue As Boolean
		Get
			Return CBool(Microsoft.Win32.Registry.GetValue("HKEY_CURRENT_USER\Software\Finestra\Recover", "IsRunning", False))
		End Get
		Set(value As Boolean)
			Microsoft.Win32.Registry.SetValue("HKEY_CURRENT_USER\Software\Finestra\Recover", "IsRunning", value)
		End Set
	End Property

End Class
