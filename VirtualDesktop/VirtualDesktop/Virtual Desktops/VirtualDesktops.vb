Imports System.Linq
Imports System.Threading

Public Enum Direction
	Up
	Down
	Left
	Right
End Enum

''' <summary>
''' Represents a single virtual desktop.
''' </summary>
''' <remarks></remarks>
Public Class VirtualDesktop

	Dim _vdm As VirtualDesktopManager
	Dim _windows As New List(Of WindowInfo)
	Dim _active As Boolean
	Dim _name As String
	Dim _windowsToRestore As New List(Of WindowInfo)

	Public Sub New(vdm As VirtualDesktopManager)
		_vdm = vdm
	End Sub

	''' <summary>
	''' The user-specified or automatic user-visible name of the desktop.
	''' </summary>
	''' <value></value>
	''' <returns></returns>
	''' <remarks></remarks>
	Public Property Name() As String
		Get
			Return _name
		End Get
		Set(value As String)
			_name = value
		End Set
	End Property

	''' <summary>
	''' Gets whether this desktop is the current, visible desktop. Only one VirtualDesktop object should have this set.
	''' </summary>
	''' <value></value>
	''' <returns></returns>
	''' <remarks></remarks>
	Public ReadOnly Property Active() As Boolean
		Get
			Return _active
		End Get
	End Property

	''' <summary>
	''' Determines whether a window is valid to show.
	''' </summary>
	''' <param name="w"></param>
	''' <returns></returns>
	''' <remarks></remarks>
	Protected Function ShowWindowValid(w As WindowInfo) As Boolean
		Return _vdm.IsWindowValid(w, False, True, False)
	End Function

	''' <summary>
	''' Determines whether a window is valid to hide.
	''' </summary>
	''' <param name="w"></param>
	''' <returns></returns>
	''' <remarks></remarks>
	Protected Function HideWindowValid(w As WindowInfo) As Boolean
		Return _vdm.IsWindowValid(w, False, False, True)
	End Function

	''' <summary>
	''' Gets either the list of windows in this desktop, creating it on the fly when the current desktop is visible.
	''' </summary>
	''' <value></value>
	''' <returns></returns>
	''' <remarks></remarks>
	Public ReadOnly Property Windows() As List(Of WindowInfo)
		Get
			If Active Then
				Return _vdm.GetCurrentWindows
			Else
				SyncLock _windows
					_windows.RemoveAll(Function(window) Not window.IsValid)
				End SyncLock
				Return _windows
			End If
		End Get
	End Property

	''' <summary>
	''' Shows all windows from a desktop that has been previously hidden.
	''' </summary>
	''' <param name="switchBackground"></param>
	''' <remarks></remarks>
	Public Sub ShowWindows(switchBackground As Boolean)
		SyncLock _windows
			If Me.Active Then Exit Sub
			Dim winPosInfo = WindowInfo.BeginDeferredPositions(_windows.Count)
			For Each w As WindowInfo In _windows.Where(Function(window) ShowWindowValid(window))
				winPosInfo = w.SetDeferredPosition(winPosInfo, Rectangle.Empty, IntPtr.Zero, WindowInfo.SetPositionFlags.ShowWindow Or WindowInfo.SetPositionFlags.NoMove Or WindowInfo.SetPositionFlags.NoSize Or WindowInfo.SetPositionFlags.NoZOrder Or WindowInfo.SetPositionFlags.NoOwnerZOrder Or WindowInfo.SetPositionFlags.NoActivate)
				If winPosInfo = IntPtr.Zero Then Exit For
			Next
			If winPosInfo = IntPtr.Zero Then
				For Each w As WindowInfo In _windows.Where(Function(window) ShowWindowValid(window))
					Try
						w.State = WindowInfo.WindowState.Show
					Catch ex As Exception
						Debug.Print(ex.Message)
					End Try
				Next
			Else
				WindowInfo.EndDeferredPositions(winPosInfo)
			End If

			'Restore minimized windows
			For Each w As WindowInfo In _windows.Where(Function(window) ShowWindowValid(window))
				Try
					If _windowsToRestore.Contains(w) Then w.State = WindowInfo.WindowState.Restore
					w.Refresh()
				Catch ex As Exception
					Debug.Print(ex.Message)
				End Try
			Next

			Try
				If Windows.Count > 0 Then Windows(Windows.Count - 1).BringToFront()
			Catch ex As Exception

			End Try
			_windows.Clear()
			_windowsToRestore.Clear()
			If switchBackground Then SetDesktopBackground(_wallPaper, Me._wallPaperStyle1, Me._wallPaperStyle2)
			_active = True
		End SyncLock
	End Sub

	''' <summary>
	''' Removes all equivalent instances of a given window from the saved window list.
	''' </summary>
	''' <param name="rw"></param>
	''' <remarks></remarks>
	Public Sub RemoveAllInstances(rw As WindowInfo)
		SyncLock _windows
			_windows.RemoveAll(Function(window) window = rw)
		End SyncLock
	End Sub

	''' <summary>
	''' Hides all window in the current desktop, assuming that this desktop is still active, and switches it into an inactive state.
	''' </summary>
	''' <remarks></remarks>
	Public Sub HideWindows()
		SyncLock _windows
			Dim cacheThumbs As Boolean = My.Settings.CacheWindowThumbnails

			_windows.Clear()
			Dim currWindows = WindowInfo.GetWindows(AddressOf HideWindowValid)

			'Cache thumbnails of all open windows, but timeout if it takes too long
			Dim finishedCachingThumbnails As Boolean = New Action(Sub()
																	  System.Threading.Thread.CurrentThread.Name = "Finestra: Thumbnail Capturing"
																	  System.Threading.Tasks.Parallel.ForEach(currWindows, Sub(w)
																															   Try
																																   If cacheThumbs AndAlso w.Visible AndAlso (Not w.Minimized OrElse _vdm.GetThumbnail(w) Is Nothing) Then
																																	   _vdm.AddThumbnail(w)
																																   End If
																															   Catch ex As Exception

																															   End Try
																														   End Sub)
																  End Sub).BeginInvoke(Nothing, Nothing).AsyncWaitHandle.WaitOne(200)

			WindowInfo.SortWindowsByZOrder(currWindows)
			For Each w As WindowInfo In currWindows
				_windows.Add(w)
				For Each v As VirtualDesktop In _vdm.Desktops
					If v IsNot Me Then
						v.RemoveAllInstances(w)
					End If
				Next
				If _vdm.IsProcessMinimizing(_vdm.GetProcessName(w.ProcessId)) Then
					w.State = WindowInfo.WindowState.Minimize
					_windowsToRestore.Add(w)
				End If
			Next

			Dim winPosInfo = WindowInfo.BeginDeferredPositions(_windows.Count)
			For Each w In _windows
				winPosInfo = w.SetDeferredPosition(winPosInfo, Rectangle.Empty, IntPtr.Zero, WindowInfo.SetPositionFlags.HideWindow Or WindowInfo.SetPositionFlags.NoMove Or WindowInfo.SetPositionFlags.NoSize Or WindowInfo.SetPositionFlags.NoOwnerZOrder Or WindowInfo.SetPositionFlags.NoZOrder Or WindowInfo.SetPositionFlags.NoActivate)
				If winPosInfo = IntPtr.Zero Then Exit For
			Next
			If winPosInfo = IntPtr.Zero Then
				For Each w In _windows
					Try
						w.State = WindowInfo.WindowState.Hide
					Catch ex As Exception

					End Try
				Next
			Else
				WindowInfo.EndDeferredPositions(winPosInfo)
			End If

			_wallPaper = Desktop.Wallpaper
			If My.Settings.UseDesktopBackgrounds Then
				If IO.File.Exists(_wallPaper) Then
					Dim newPath As String = My.Computer.FileSystem.SpecialDirectories.CurrentUserApplicationData
					If Not IO.Directory.Exists(newPath) Then IO.Directory.CreateDirectory(newPath)
					newPath = IO.Path.Combine(newPath, _vdm.Desktops.IndexOf(Me).ToString & IO.Path.GetExtension(_wallPaper))
					If _wallPaper <> newPath Then
						If IO.File.Exists(newPath) Then IO.File.Delete(newPath)
						IO.File.Copy(_wallPaper, newPath)
						_wallPaper = newPath
					End If
				End If
			End If
			_wallPaperStyle1 = Desktop.WallpaperStyleData1
			_wallPaperStyle2 = Desktop.WallpaperStyleData2
			_active = False
		End SyncLock
	End Sub

	''' <summary>
	''' Gets the list of windows that have to restored to a non-minimized state upon reappearing.
	''' </summary>
	''' <value></value>
	''' <returns></returns>
	''' <remarks></remarks>
	Public ReadOnly Property WindowsToRestore() As List(Of WindowInfo)
		Get
			Return _windowsToRestore
		End Get
	End Property

	Dim _wallPaper As String
	Dim _wallPaperStyle1 As String
	Dim _wallPaperStyle2 As String

	''' <summary>
	''' Stores the path to this desktop's wallpaper.
	''' </summary>
	''' <value></value>
	''' <returns></returns>
	''' <remarks></remarks>
	Public Property DesktopBackground() As String
		Get
			If _vdm.CurrentDesktop Is Me Then
				Return Desktop.Wallpaper
			Else
				Return _wallPaper
			End If
		End Get
		Set(value As String)
			_wallPaper = value
			If _vdm.CurrentDesktop Is Me Then
				SetDesktopBackground(value, Me.DesktopBackgroundData1, Me.DesktopBackgroundData2)
			End If
		End Set
	End Property

	''' <summary>
	''' Stores the first flag for this desktop's wallpaper. Used for the wallpaper sizing mode.
	''' </summary>
	''' <value></value>
	''' <returns></returns>
	''' <remarks></remarks>
	Public Property DesktopBackgroundData1() As String
		Get
			If _vdm.CurrentDesktop Is Me Then
				Return Desktop.WallpaperStyleData1
			Else
				Return _wallPaperStyle1
			End If
		End Get
		Set(value As String)
			_wallPaperStyle1 = value
			If _vdm.CurrentDesktop Is Me Then
				SetDesktopBackground(DesktopBackground, value, Me.DesktopBackgroundData2)
			End If
		End Set
	End Property

	''' <summary>
	''' Stores the second flag for this desktop's wallpaper. Used for the wallpaper sizing mode.
	''' </summary>
	''' <value></value>
	''' <returns></returns>
	''' <remarks></remarks>
	Public Property DesktopBackgroundData2() As String
		Get
			If _vdm.CurrentDesktop Is Me Then
				Return Desktop.WallpaperStyleData2
			Else
				Return _wallPaperStyle2
			End If
		End Get
		Set(value As String)
			_wallPaperStyle2 = value
			If _vdm.CurrentDesktop Is Me Then
				SetDesktopBackground(DesktopBackground, Me.DesktopBackgroundData1, value)
			End If
		End Set
	End Property

#Region "SetDesktopBackground Async"

	Private Shared t As New Threading.Thread(AddressOf SetDesktopBackground)

	''' <summary>
	''' Set the current Windows desktop background.
	''' </summary>
	''' <param name="image"></param>
	''' <param name="data1"></param>
	''' <param name="data2"></param>
	''' <remarks></remarks>
	Public Shared Sub SetDesktopBackground(image As String, data1 As String, data2 As String)
		If My.Settings.UseDesktopBackgrounds Then
			If t.IsAlive Then t.Join()
			Dim dba As New DesktopBackgroundArgs
			dba.ImageFile = image
			dba.Style1 = data1
			dba.Style2 = data2
			t = New Threading.Thread(AddressOf SetDesktopBackground)
			t.Start(dba)
		End If
	End Sub

	Private Class DesktopBackgroundArgs
		Public ImageFile As String
		Public Style1 As String
		Public Style2 As String
	End Class

	''' <summary>
	''' Sets the current Windows desktop background, designed to be called as another thread.
	''' </summary>
	''' <param name="args"></param>
	''' <remarks></remarks>
	Private Shared Sub SetDesktopBackground(args As Object)
		Threading.Thread.CurrentThread.Name = "Finestra: Set Desktop Background"
		Try
			Dim dba As DesktopBackgroundArgs = args
			Desktop.WallpaperStyleData1 = dba.Style1
			Desktop.WallpaperStyleData2 = dba.Style2
			Desktop.Wallpaper = dba.ImageFile
		Catch ex As Exception
			Debug.Print("Error setting background: " & ex.Message)
		End Try
	End Sub

#End Region

End Class

''' <summary>
''' Represents the global manager of VirtualDesktop objects.
''' </summary>
''' <remarks></remarks>
Public Class VirtualDesktopManager

	Public Event VirtualDesktopSwitched(previousDesk As Integer, newDesk As Integer)
	Public Event VirtualDesktopSwitching(previousDesk As Integer, newDesk As Integer)

	Dim _desktops As New List(Of VirtualDesktop)
	Dim _switchLock As New Object

	''' <summary>
	''' Gets the collection of all desktop objects.
	''' </summary>
	Public ReadOnly Property Desktops() As List(Of VirtualDesktop)
		Get
			Return _desktops
		End Get
	End Property

	''' <summary>
	''' Gets or sets the list of currently loaded plugins.
	''' </summary>
	''' <value></value>
	''' <returns></returns>
	''' <remarks></remarks>
	Public Property LoadedPlugins As List(Of VirtualDesktopLoadedPlugin)

	Dim _currentDesktop As VirtualDesktop

	''' <summary>
	''' Gets or sets the currently active desktop object. Setting it actually switches desktops.
	''' </summary>
	''' <value></value>
	''' <returns></returns>
	''' <remarks></remarks>
	Public Property CurrentDesktop() As VirtualDesktop
		Get
			Return _currentDesktop
		End Get
		Set(value As VirtualDesktop)
			'Be friendly to the UI components while locking
			While Not Monitor.TryEnter(_switchLock)
				Windows.Forms.Application.DoEvents()
				Thread.Sleep(0)
			End While
			Try
				If value IsNot _currentDesktop Then
					Dim prevDesk As Integer = CurrentDesktopIndex

					'Find the currently captured and moving/sizing windows in order to prevent them from being switched
					Dim info = WindowInfo.GetGuiThreadInfo
					currCaptureWindow = info.hwndCapture
					currMoveSizeWindow = info.hwndMoveSize

					RaiseEvent VirtualDesktopSwitching(prevDesk, _desktops.IndexOf(value))

					'Switch the windows
					_currentDesktop.HideWindows()
					_currentDesktop = value
					value.ShowWindows(True)

					RaiseEvent VirtualDesktopSwitched(prevDesk, CurrentDesktopIndex)

					RefreshThumbnails()
				End If
			Finally
				Monitor.Exit(_switchLock)
			End Try
		End Set
	End Property

	''' <summary>
	''' Provides an index-based way to switch desktops.
	''' </summary>
	''' <value></value>
	''' <returns></returns>
	''' <remarks></remarks>
	Public Property CurrentDesktopIndex() As Integer
		Get
			Return Desktops.IndexOf(CurrentDesktop)
		End Get
		Set(value As Integer)
			Me.CurrentDesktop = Me.Desktops(value)
		End Set
	End Property

	Dim useMons() As Integer

	''' <summary>
	''' Gets the indices of the monitors to be used for switching.
	''' </summary>
	''' <value></value>
	''' <returns></returns>
	''' <remarks></remarks>
	Public ReadOnly Property MonitorIndices() As Integer()
		Get
			Return useMons
		End Get
	End Property

	''' <summary>
	''' Initializes the VirtualDesktopManager using the current user-specified settings.
	''' </summary>
	''' <param name="num"></param>
	''' <remarks></remarks>
	Public Sub Start(num As Integer)
		'Initialize settings
		If num < 1 Then num = 1
		If My.Settings.DesktopBackgrounds Is Nothing Then
			My.Settings.DesktopBackgrounds = New Specialized.StringCollection
		End If
		If My.Settings.DesktopBackgroundsData1 Is Nothing Then
			My.Settings.DesktopBackgroundsData1 = New Specialized.StringCollection
		End If
		If My.Settings.DesktopBackgroundsData2 Is Nothing Then
			My.Settings.DesktopBackgroundsData2 = New Specialized.StringCollection
		End If

		If My.Settings.DesktopBackgrounds.Count <> My.Settings.DesktopBackgroundsData1.Count OrElse My.Settings.DesktopBackgrounds.Count <> My.Settings.DesktopBackgroundsData2.Count Then
			My.Settings.DesktopBackgrounds = New Specialized.StringCollection
			My.Settings.DesktopBackgroundsData1 = New Specialized.StringCollection
			My.Settings.DesktopBackgroundsData2 = New Specialized.StringCollection
		End If

		'Only use select monitors
		Dim useMonStrs As New Specialized.StringCollection
		useMonStrs.AddRange(My.Settings.UseMonitors.Split(New Char() {"|"}, StringSplitOptions.RemoveEmptyEntries))
		ReDim useMons(useMonStrs.Count - 1)
		For i As Integer = 0 To useMonStrs.Count - 1
			useMons(i) = Integer.Parse(useMonStrs(i))
		Next

		'Add desktops
		For i As Integer = 0 To num - 1
			_desktops.Add(New VirtualDesktop(Me))
			If My.Settings.DesktopBackgrounds.Count > i Then
				_desktops(i).DesktopBackground = My.Settings.DesktopBackgrounds(i)
				_desktops(i).DesktopBackgroundData1 = My.Settings.DesktopBackgroundsData1(i)
				_desktops(i).DesktopBackgroundData2 = My.Settings.DesktopBackgroundsData2(i)
			Else
				My.Settings.DesktopBackgrounds.Add(Desktop.Wallpaper)
				My.Settings.DesktopBackgroundsData1.Add(Desktop.WallpaperStyleData1)
				My.Settings.DesktopBackgroundsData2.Add(Desktop.WallpaperStyleData2)
				_desktops(i).DesktopBackground = Desktop.Wallpaper
				_desktops(i).DesktopBackgroundData1 = Desktop.WallpaperStyleData1
				_desktops(i).DesktopBackgroundData2 = Desktop.WallpaperStyleData2
			End If
			If My.Settings.DesktopNames.Count > i Then
				_desktops(i).Name = My.Settings.DesktopNames(i)
			Else
				_desktops(i).Name = My.Resources.DesktopWord & " " & (i + 1).ToString
			End If
		Next
		_currentDesktop = _desktops(0)
		_currentDesktop.ShowWindows(False)
	End Sub

	''' <summary>
	''' Shuts down and un-initializes the VirtualDesktopManager.
	''' </summary>
	''' <remarks></remarks>
	Public Sub [Stop]()
		My.Settings.DesktopBackgrounds = New Specialized.StringCollection
		My.Settings.DesktopBackgroundsData1 = New Specialized.StringCollection
		My.Settings.DesktopBackgroundsData2 = New Specialized.StringCollection
		For Each d As VirtualDesktop In Desktops
			Try
				Try
					My.Settings.DesktopBackgrounds.Add(d.DesktopBackground)
					My.Settings.DesktopBackgroundsData1.Add(d.DesktopBackgroundData1)
					My.Settings.DesktopBackgroundsData2.Add(d.DesktopBackgroundData2)

					If My.Settings.DesktopNames.Count <= Desktops.IndexOf(d) Then
						My.Settings.DesktopNames.Add(d.Name)
					Else
						My.Settings.DesktopNames(Desktops.IndexOf(d)) = d.Name
					End If
				Finally
					If Not CurrentDesktop Is d Then d.ShowWindows(False)
				End Try
			Catch ex As Exception

			End Try
		Next
		VirtualDesktop.SetDesktopBackground(Desktops(0).DesktopBackground, Desktops(0).DesktopBackgroundData1, Desktops(0).DesktopBackgroundData2)
		Desktops.Clear()
	End Sub

	''' <summary>
	''' Returns a list of currently-visible windows.
	''' </summary>
	''' <returns></returns>
	''' <remarks></remarks>
	Public Function GetCurrentWindows() As List(Of WindowInfo)
		Return WindowInfo.GetWindows(AddressOf IsWindowValid)
	End Function

	''' <summary>
	''' Gets whether a window is valid to be managed by the VirtualDesktopManager.
	''' </summary>
	''' <param name="w"></param>
	''' <returns></returns>
	''' <remarks></remarks>
	Public Function IsWindowValid(w As WindowInfo) As Boolean
		Return IsWindowValid(w, False)
	End Function

	''' <summary>
	''' Returns whether a process, specified by name, is set to be sticky by the user.
	''' </summary>
	''' <param name="p"></param>
	''' <returns></returns>
	''' <remarks></remarks>
	Public Function IsProcessSticky(p As String) As Boolean
		p = p.ToLower
		If p.EndsWith(".exe") Then
			p = p.Substring(0, p.Length - 4)
		End If

		For Each s As String In My.Settings.StickyPrograms
			If p Like LCase(s) Then
				Return True
			End If
		Next
		Return False
	End Function

	''' <summary>
	''' Returns whether a process, specified by name, is set to be sticky by the user.
	''' </summary>
	''' <param name="p"></param>
	''' <returns></returns>
	''' <remarks></remarks>
	Public Function IsProcessMinimizing(p As String) As Boolean
		p = p.ToLower
		If p.EndsWith(".exe") Then
			p = p.Substring(0, p.Length - 4)
		End If

		For Each s As String In My.Settings.MinimizePrograms
			If p Like LCase(s) Then
				Return True
			End If
		Next
		Return False
	End Function

	Dim _procNameCache As New Dictionary(Of Integer, String)

	''' <summary>
	''' Returns the name of the process matching the specified process ID, using caching.
	''' </summary>
	''' <param name="id"></param>
	''' <returns></returns>
	''' <remarks></remarks>
	Public Function GetProcessName(id As Integer) As String
		If _procNameCache.ContainsKey(id) Then
			Return _procNameCache(id)
		Else
			Dim name = Process.GetProcessById(id).ProcessName
			_procNameCache.Add(id, name)
			Return name
		End If
	End Function

	Dim currCaptureWindow As IntPtr
	Dim currMoveSizeWindow As IntPtr

	''' <summary>
	''' Returns whether a window can be managed by the VirtualDesktopManager, given the specified criteria.
	''' </summary>
	''' <param name="w"></param>
	''' <param name="includeStickies"></param>
	''' <param name="includeHidden"></param>
	''' <param name="includeShadows"></param>
	''' <returns></returns>
	''' <remarks></remarks>
	Public Function IsWindowValid(w As WindowInfo, includeStickies As Boolean, Optional includeHidden As Boolean = False, Optional includeShadows As Boolean = False) As Boolean
		Try
			Dim className As String = w.ClassName
			If className Is Nothing Then Return False
			Dim text As String = w.Text
			If w.IsValid _
			 AndAlso (includeStickies OrElse (Not IsSticky(w))) _
			 AndAlso (text <> "" OrElse (includeShadows AndAlso (className = "SysShadow" OrElse className = "ShadowWindow"))) _
			 AndAlso (includeHidden OrElse w.Visible) _
			 AndAlso (Not w = WindowInfo.FindWindowByClass("Progman")) _
			 AndAlso (Not (UCase(className) = "BUTTON" AndAlso (text = "Start" OrElse text = "Démarrer"))) _
			 AndAlso (Not (Diagnostics.Debugger.IsAttached AndAlso className = "wndclass_desked_gsk")) Then
				Dim procId = w.ProcessId
				Dim procName = GetProcessName(procId)
				If (Not (Diagnostics.Debugger.IsAttached AndAlso procName Like "*devenv*")) AndAlso
				  (My.Settings.AllMonitors OrElse Array.IndexOf(useMons, (Array.IndexOf(Screen.AllScreens, Screen.FromHandle(w.Handle)))) >= 0) AndAlso
				  w.Width > 0 AndAlso
				  w.Height > 0 AndAlso
				  (Not (text = "Start Menu" AndAlso procName Like "*explorer*")) AndAlso
				  (Not (className = "Desktop User Picture" AndAlso procName Like "*explorer*")) AndAlso
				  (Not (className = "ThumbnailClass" AndAlso procName Like "*explorer*")) AndAlso
				  (includeStickies OrElse Not IsProcessSticky(procName)) AndAlso
				  (Not procId = Process.GetCurrentProcess.Id) Then
					If w.Handle <> currCaptureWindow AndAlso w.Handle <> currMoveSizeWindow Then
						Return True
					End If
				End If
			End If
		Catch ex As Exception

		End Try
		Return False
	End Function

#Region "Sticky Windows"

	Dim _stickyWindows As New List(Of WindowInfo)

	''' <summary>
	''' Contains windows that should not be attached to any particular virtual desktop.
	''' </summary>
	''' <value></value>
	''' <returns></returns>
	''' <remarks></remarks>
	Private ReadOnly Property StickyWindows() As List(Of WindowInfo)
		Get
			For i As Integer = 0 To _stickyWindows.Count - 1
				If Not _stickyWindows(i).IsValid Then
					_stickyWindows.RemoveAt(i)
					i -= 1
				End If
			Next
			Return _stickyWindows
		End Get
	End Property

	Public Function IsSticky(w As WindowInfo) As Boolean
		For Each s As WindowInfo In StickyWindows
			If s = w Then Return True
		Next
		Return False
	End Function

	Public Sub StickWindow(w As WindowInfo)
		If Not IsSticky(w) Then StickyWindows.Add(w)
	End Sub

	Public Sub UnstickWindow(w As WindowInfo)
		If IsSticky(w) Then
			Dim i = 0
			For Each s As WindowInfo In StickyWindows
				If s = w Then
					StickyWindows.RemoveAt(i)
					Exit Sub
				End If
				i += 1
			Next
		End If
	End Sub

#End Region

#Region "Thumbnail Caching"

	Dim _winThumbs As New Dictionary(Of IntPtr, Bitmap)

	Public Sub AddThumbnail(w As WindowInfo)
		If w.Visible AndAlso Not w.Minimized Then
			Dim thumb = w.CaptureBitmap
			SyncLock _winThumbs
				If _winThumbs.ContainsKey(w.Handle) Then
					_winThumbs(w.Handle).Dispose()
					_winThumbs.Remove(w.Handle)
				End If
				_winThumbs.Add(w.Handle, thumb)
			End SyncLock
		End If
	End Sub

	Public Sub RefreshThumbnails()
		SyncLock _winThumbs
			For Each h As IntPtr In _winThumbs.Keys.Where(Function(win) Not New WindowInfo(win).IsValid).ToList
				_winThumbs(h).Dispose()
				_winThumbs.Remove(h)
			Next
		End SyncLock
	End Sub

	Public Sub RemoveThumbnail(w As WindowInfo)
		SyncLock _winThumbs
			_winThumbs(w.Handle).Dispose()
			_winThumbs.Remove(w.Handle)
		End SyncLock
	End Sub

	Public Function GetThumbnail(w As WindowInfo) As Bitmap
		Dim b As Bitmap = Nothing
		If _winThumbs.TryGetValue(w.Handle, b) Then
			Return b
		Else
			Return Nothing
		End If
	End Function

#End Region

#Region "Utilities"

	Public Function GetDesktopDirectional(dir As Direction, fromDesktop As Integer) As Integer
		Select Case dir
			Case Direction.Up
				Dim rows As Integer = Math.Round(Math.Sqrt(Desktops.Count))
				Dim cols As Integer = Math.Ceiling(CDbl(Desktops.Count) / CDbl(rows))
				Dim nextdesk As Integer = Math.Floor(fromDesktop / rows)
				nextdesk = (fromDesktop - nextdesk * cols) + (nextdesk - 1) * cols
				If nextdesk < 0 Then
					nextdesk = Math.Min(fromDesktop + (rows - 1) * cols, Desktops.Count - 1)
				End If
				Return nextdesk
			Case Direction.Down
				Dim rows As Integer = Math.Round(Math.Sqrt(Desktops.Count))
				Dim cols As Integer = Math.Ceiling(CDbl(Desktops.Count) / CDbl(rows))
				Dim nextdesk As Integer = Math.Floor(fromDesktop / rows)
				nextdesk = (fromDesktop - nextdesk * cols) + (nextdesk + 1) * cols
				If nextdesk >= Desktops.Count Then
					nextdesk = fromDesktop - Math.Floor(fromDesktop / rows) * cols
				End If
				Return nextdesk
			Case Direction.Left
				If fromDesktop <= 0 Then
					Return Desktops.Count - 1
				Else
					Return fromDesktop - 1
				End If
			Case Else 'Direction.Right
				If fromDesktop >= Desktops.Count - 1 Then
					Return 0
				Else
					Return fromDesktop + 1
				End If
		End Select
	End Function

	Public Sub SendWindowToDesktop(w As WindowInfo, newDesk As Integer, oldDesk As Integer)
		If oldDesk = newDesk Then Exit Sub
		If oldDesk = CurrentDesktopIndex Then
			Try
				Desktops(newDesk).Windows.Add(w)
				If Not w.Minimized AndAlso IsProcessMinimizing(GetProcessName(w.ProcessId)) Then
					w.State = WindowInfo.WindowState.Minimize
					Desktops(newDesk).WindowsToRestore.Add(w)
				End If
				w.State = WindowInfo.WindowState.Hide
			Catch ex As Exception

			End Try
		ElseIf newDesk = CurrentDesktopIndex Then
			Try
				w.State = WindowInfo.WindowState.ShowNA
				If Desktops(oldDesk).WindowsToRestore.Contains(w) Then w.State = WindowInfo.WindowState.Restore
				Desktops(oldDesk).RemoveAllInstances(w)
			Catch ex As Exception

			End Try
		Else
			Desktops(newDesk).Windows.Add(w)
			If Desktops(oldDesk).WindowsToRestore.Contains(w) Then
				Desktops(newDesk).WindowsToRestore.Add(w)
			End If
			Desktops(oldDesk).RemoveAllInstances(w)
		End If
	End Sub

	''' <summary>
	''' Gets the size of a single desktop's preview area as seen in the switcher for a given monitor.
	''' </summary>
	''' <param name="monitor"></param>
	''' <returns></returns>
	''' <remarks></remarks>
	Public Function GetDesktopSize(monitor As Integer) As Size
		Dim rows As Integer = Math.Round(Math.Sqrt(Desktops.Count))
		Dim cols As Integer = Math.Ceiling(CDbl(Desktops.Count) / CDbl(rows))
		Dim deskSize As New Size(PreviewWinSize(monitor).Width / cols, PreviewWinSize(monitor).Height / cols)
		Return deskSize
	End Function

	''' <summary>
	''' Gets the size of the entire switcher window for a given monitor.
	''' </summary>
	''' <param name="monitor"></param>
	''' <value></value>
	''' <returns></returns>
	''' <remarks></remarks>
	Public ReadOnly Property PreviewWinSize(monitor As Integer) As Size
		Get
			Return Screen.AllScreens(monitor).Bounds.Size
		End Get
	End Property

	''' <summary>
	''' Returns the (0,0) point for a given desktop index on the switcher black form.
	''' </summary>
	''' <param name="d"></param>
	''' <returns></returns>
	''' <remarks></remarks>
	Public Function GetDesktopPreviewBounds(d As Integer, monitor As Integer) As Rectangle
		Return GetDesktopPreviewBounds(d, Screen.AllScreens(monitor).Bounds)
	End Function

	Public Function GetDesktopPreviewBounds(d As Integer, monBounds As Rectangle) As Rectangle
		Dim rows As Integer = Math.Round(Math.Sqrt(Desktops.Count))
		Dim cols As Integer = Math.Ceiling(CDbl(Desktops.Count) / CDbl(rows))

		Dim deskSize As New Size(monBounds.Width / cols, monBounds.Height / cols)

		Dim offsetY As Integer = (monBounds.Height - rows * deskSize.Height) / 2
		Dim deskRow = d \ cols
		Dim deskCol = d Mod cols

		If deskRow = rows - 1 Then
			Dim offsetX As Integer = (monBounds.Width - ((Desktops.Count - (deskRow * cols)) * deskSize.Width)) / 2
			Return New Rectangle(New Point(deskCol * deskSize.Width + offsetX, deskRow * deskSize.Height + offsetY) + monBounds.Location, deskSize)
		Else
			Return New Rectangle(New Point(deskCol * deskSize.Width, deskRow * deskSize.Height + offsetY) + monBounds.Location, deskSize)
		End If
	End Function

	Public Event GlobalMessage(message As String, icon As ToolTipIcon, callback As Action)

	Public Sub SendGlobalMessage(message As String, icon As ToolTipIcon, callback As Action)
		RaiseEvent GlobalMessage(message, icon, callback)
	End Sub

	Public Event GlobalCommand(command As String)

	Public Sub SendGlobalCommand(command As String)
		RaiseEvent GlobalCommand(command)
	End Sub

#End Region

End Class
