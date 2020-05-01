Public Class HotKey

	Shared hotkeyHandles As New Dictionary(Of String, Integer)
	Shared hotkeyIds As New Dictionary(Of Integer, String)
	Shared hotkeyNextInt As Integer = 0
	Shared hotkeyIdLock As New Object

	WithEvents hotKeyWin As NativeWindowEx

	Public Sub New()
		hotKeyWin = New NativeWindowEx
		hotKeyWin.CreateHandle(New CreateParams)
	End Sub

	Private Sub hotKeyWin_MessageRecieved(ByRef m As System.Windows.Forms.Message) Handles hotKeyWin.MessageRecieved
		If m.Msg = WM_HOTKEY Then
			SyncLock hotkeyIdLock
				If hotkeyIds.ContainsKey(m.WParam) Then
					RaiseEvent HotKeyPressed(hotkeyIds(m.WParam))
				End If
			End SyncLock
		End If
	End Sub

	Public Event HotKeyPressed(id As String)

	Const WM_HOTKEY As Integer = &H312

	Private Declare Function RegisterHotKeyAPI Lib "user32.dll" Alias "RegisterHotKey" (hwnd As IntPtr, id As Integer, acc As UInteger, keys As UInteger) As Boolean
	Private Declare Function UnregisterHotKeyAPI Lib "user32.dll" Alias "UnregisterHotKey" (hwnd As IntPtr, id As Integer) As Boolean

	Public Sub RegisterHotKey(id As String, modifiers As ModifierKeys, keyCode As Keys)
		SyncLock hotkeyIdLock
			If RegisterHotKeyAPI(hotKeyWin.Handle, hotkeyNextInt, CInt(modifiers), CInt(keyCode)) = False Then
				Throw New System.ComponentModel.Win32Exception()
			End If
			hotkeyHandles(id) = hotkeyNextInt
			hotkeyIds(hotkeyNextInt) = id
			hotkeyNextInt += 1
		End SyncLock
	End Sub

	Public Function TryRegisterHotKey(id As String, modifiers As ModifierKeys, keyCode As Keys) As Boolean
		SyncLock hotkeyIdLock
			Dim ret As Boolean = RegisterHotKeyAPI(hotKeyWin.Handle, hotkeyNextInt, CInt(modifiers), CInt(keyCode))
			hotkeyHandles(id) = hotkeyNextInt
			hotkeyIds(hotkeyNextInt) = id
			hotkeyNextInt += 1
			Return ret
		End SyncLock
	End Function

	Public Sub UnregisterHotKey(id As String)
		SyncLock hotkeyIdLock
			If UnregisterHotKeyAPI(hotKeyWin.Handle, hotkeyHandles(id)) = False Then
				Throw New System.ComponentModel.Win32Exception()
			End If
			hotkeyIds.Remove(hotkeyHandles(id))
			hotkeyHandles.Remove(id)
		End SyncLock
	End Sub

	Public Function TryUnregisterHotKey(id As String) As Boolean
		Return UnregisterHotKeyAPI(hotKeyWin.Handle, id)
	End Function

	<Flags()> _
	Public Enum ModifierKeys
		None = 0
		Alt = 1
		Control = 2
		Shift = 4
		Windows = 8
	End Enum

End Class
