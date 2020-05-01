Imports System.IO
Imports System.Runtime.InteropServices
Imports Microsoft.Win32

''' <summary>
''' Switches desktop folders with the virtual desktop switches. Also save desktop icon positions.
''' </summary>
''' <remarks>Currently, this plugin does not correctly save desktop icons.</remarks>
Public Class DesktopIconsPlugin
	Inherits VirtualDesktopPlugin

	Dim hasInited As Boolean = False
	Dim folderPaths As New List(Of String)

	Public Sub New(vdm As VirtualDesktopManager)
		MyBase.New(vdm)

		For i As Integer = 0 To 3
			Dim newPath = If(i = 0, Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Finestra\Desktop" & i))
			folderPaths.Add(newPath)
			Directory.CreateDirectory(newPath)
		Next
	End Sub

	Public Overrides Sub Start()
		If My.Settings.DeskSwitchEnable Then
			AddHandler VirtualDesktopManager.VirtualDesktopSwitched, AddressOf VDM_DesktopSwitched
			hasInited = True
		End If
	End Sub

	Public Overrides Sub [Stop]()
		If hasInited Then
			RemoveHandler VirtualDesktopManager.VirtualDesktopSwitched, AddressOf VDM_DesktopSwitched
		End If
	End Sub

	Private Sub VDM_DesktopSwitched(oldIndex As Integer, newIndex As Integer)
		SaveDesktopIcons(oldIndex)
		RestoreDesktopIcons(newIndex)
		SetDesktopPath(folderPaths(newIndex))
		RefreshDesktop()
	End Sub

	Public Sub SaveDesktopIcons(oldDesktop As Integer)
		Try
			Registry.CurrentUser.DeleteSubKeyTree("Software\Finestra\DesktopIcons\" & oldDesktop)
		Catch ex As ArgumentException
			'The key didn't exist
		End Try

		Using oldKey = Registry.CurrentUser.OpenSubKey("Software\Microsoft\Windows\Shell\Bags\1\Desktop"),
			newKey = Registry.CurrentUser.CreateSubKey("Software\Finestra\DesktopIcons\" & oldDesktop)
			RegistryUtils.CopyKey(oldKey, newKey)
		End Using
	End Sub

	Public Sub RestoreDesktopIcons(newDesktop As Integer)
		Using oldKey = Registry.CurrentUser.OpenSubKey("Software\Finestra\DesktopIcons\" & newDesktop)
			If oldKey IsNot Nothing Then
				Registry.CurrentUser.DeleteSubKeyTree("Software\Microsoft\Windows\Shell\Bags\1\Desktop")
				Using newKey = Registry.CurrentUser.CreateSubKey("Software\Microsoft\Windows\Shell\Bags\1\Desktop")
					RegistryUtils.CopyKey(oldKey, newKey)
				End Using
			End If
		End Using
	End Sub

	Public Sub SetDesktopPath(newDesktop As String)
		If Environment.OSVersion.Version.Major < 6 Then
			Registry.SetValue("HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders", "Desktop", newDesktop)
			Registry.SetValue("HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders", "Desktop", newDesktop)

			Dim iAttribute As UInteger, oldPidl As IntPtr, newPidl As IntPtr
			SHParseDisplayName(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), IntPtr.Zero, oldPidl, 0, iAttribute)
			SHParseDisplayName(newDesktop, IntPtr.Zero, newPidl, 0, iAttribute)

			SHChangeNotify(SHChangeNotifyEvent.SHCNE_RENAMEFOLDER, &H1000, oldPidl, newPidl)
		Else
			SHSetKnownFolderPath(KnownFolders.Desktop, 0, IntPtr.Zero, newDesktop)
		End If
	End Sub

	Public Sub RefreshDesktop()
		SHChangeNotify(SHChangeNotifyEvent.SHCNE_ASSOCCHANGED, SHChangeNotifyFlags.SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero)

		Dim iAttribute As UInteger, pidl As IntPtr
		SHParseDisplayName(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), IntPtr.Zero, pidl, 0, iAttribute)
		SHChangeNotify(SHChangeNotifyEvent.SHCNE_ATTRIBUTES, SHChangeNotifyFlags.SHCNF_FLUSH, pidl, IntPtr.Zero)
	End Sub

	Private Enum SHChangeNotifyEvent As UInt32
		SHCNE_ALLEVENTS = &H7FFFFFFFL
		SHCNE_ASSOCCHANGED = &H8000000L
		SHCNE_ATTRIBUTES = &H800L
		SHCNE_CREATE = &H2L
		SHCNE_DELETE = &H4L
		SHCNE_DISKEVENTS = &H2381FL
		SHCNE_DRIVEADD = &H100L
		SHCNE_DRIVEADDGUI = &H10000L
		SHCNE_DRIVEREMOVED = &H80L
		SHCNE_EXTENDED_EVENT = &H4000000L
		SHCNE_FREESPACE = &H40000L
		SHCNE_GLOBALEVENTS = &HC0581E0L
		SHCNE_INTERRUPT = &H80000000L
		SHCNE_MEDIAINSERTED = &H20L
		SHCNE_MEDIAREMOVED = &H40L
		SHCNE_MKDIR = &H8L
		SHCNE_NETSHARE = &H200L
		SHCNE_NETUNSHARE = &H400L
		SHCNE_RENAMEFOLDER = &H20000L
		SHCNE_RENAMEITEM = &H1L
		SHCNE_RMDIR = &H10L
		SHCNE_SERVERDISCONNECT = &H4000L
		SHCNE_UPDATEDIR = &H1000L
		SHCNE_UPDATEIMAGE = &H8000L
		SHCNE_UPDATEITEM = &H2000L
	End Enum

	<Flags>
	Private Enum SHChangeNotifyFlags As UInt32
		SHCNF_DWORD = 3
		SHCNF_FLUSH = &H1000
		SHCNF_FLUSHNOWAIT = &H2000
		SHCNF_IDLIST = 0
		SHCNF_PATH = 5
		SHCNF_PATHA = 1
		SHCNF_PATHW = 5
		SHCNF_PRINTER = 6
		SHCNF_PRINTERA = 2
		SHCNF_PRINTERW = 6
		SHCNF_TYPE = &HFF
	End Enum

	Private Class KnownFolders
		Public Shared ReadOnly Desktop As New Guid("B4BFCC3A-DB2C-424C-B029-7FE99A87C641")
	End Class

	Declare Auto Function SHParseDisplayName Lib "shell32.dll" (<MarshalAs(UnmanagedType.LPWStr)> pszName As String, pbc As IntPtr, ByRef ppidl As IntPtr, sfgaoIn As UInteger, ByRef psfgaoOut As UInteger) As Integer
	Declare Auto Function SHChangeNotify Lib "shell32.dll" (eventId As Integer, flags As Integer, item1 As IntPtr, item2 As IntPtr) As Integer
	Declare Auto Function SHSetKnownFolderPath Lib "shell32.dll" (ByRef folderId As Guid, flags As UInteger, token As IntPtr, <MarshalAs(UnmanagedType.LPWStr)> path As String) As Integer

End Class
