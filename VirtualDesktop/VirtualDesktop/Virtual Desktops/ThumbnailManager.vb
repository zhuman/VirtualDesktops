''' <summary>
''' Manages the switcher and the thumbnail windows used.
''' </summary>
''' <remarks></remarks>
Public Class ThumbnailManager

	WithEvents timFade As New Timer
	Public WithEvents backgroundWins As New List(Of DoubleBufferedForm)
	Public thumbWins As New List(Of SwitcherThumbnailWindow)
	Dim vdm As VirtualDesktopManager

	Dim _switcherVisible As Boolean
	Dim _exposeMode As Boolean

	Public ReadOnly Property SwitcherVisible() As Boolean
		Get
			Return _switcherVisible
		End Get
	End Property

	Public ReadOnly Property ExposeMode() As Boolean
		Get
			Return _exposeMode
		End Get
	End Property

	Dim exposeWindows As List(Of List(Of WindowInfo))
	Dim exposeDeskSize As Size

	Public Sub New(vdm As VirtualDesktopManager)
		Me.vdm = vdm
	End Sub

	''' <summary>
	''' Displays the full-screen switcher on every monitor set in the settings.
	''' </summary>
	''' <param name="expose"></param>
	''' <remarks></remarks>
	Public Sub ShowSwitcher(Optional expose As Boolean = False)
		If Not _switcherVisible Then

			isHiding = False
			_switcherVisible = True
			_exposeMode = expose
			If ExposeMode Then
				timFade.Interval = 10
			Else
				timFade.Interval = 10
			End If

			exposeWindows = Nothing
			exposeDeskSize = New Size(0, 0)
			selectedItem = vdm.CurrentDesktopIndex
			backgroundWins.Clear()

			'Set up the black form
			For Each s As Screen In Screen.AllScreens
				If My.Settings.AllMonitors OrElse Array.IndexOf(vdm.MonitorIndices, Array.IndexOf(Screen.AllScreens, s)) >= 0 Then
					Dim b As New DoubleBufferedForm
					backgroundWins.Add(b)
					If ExposeMode Then
						'AddHandler b.Paint, AddressOf switcherExpose_Paint
					Else
						AddHandler b.Paint, AddressOf switcher_Paint
					End If
					AddHandler b.KeyDown, AddressOf switcher_KeyDown
					AddHandler b.MouseDoubleClick, AddressOf switcher_MouseDoubleClick
					AddHandler b.MouseWheel, AddressOf switcher_MouseWheel
					AddHandler b.MouseClick, AddressOf switcher_MouseClick

					b.BackColor = My.Settings.PreviewBackground
					b.FormBorderStyle = Windows.Forms.FormBorderStyle.None
					b.Size = s.Bounds.Size
					b.Opacity = 0
					b.ShowInTaskbar = False
					b.TopMost = True
					b.Show()
					b.Activate()
					b.Location = s.Bounds.Location
					b.Refresh()
				End If
			Next

			'Create the thumbnails
			If Not ExposeMode Then
				For Each d As VirtualDesktop In vdm.Desktops
					Dim wins As List(Of WindowInfo) = d.Windows
					If d.Active Then
						WindowInfo.SortWindowsByZOrder(wins)
					End If
					SyncLock wins
						For Each w As WindowInfo In wins
							Dim thumbwin As New SwitcherThumbnailWindow(Me, w, d, vdm, ExposeMode, New Rectangle)
							AddHandler thumbwin.KeyDown, AddressOf switcher_KeyDown
							AddHandler thumbwin.MouseWheel, AddressOf switcher_MouseWheel
							AddHandler thumbwin.MouseDown, AddressOf thumb_MouseDown
							thumbWins.Add(thumbwin)
							For Each b As DoubleBufferedForm In backgroundWins
								If Screen.FromControl(b).Equals(Screen.FromHandle(w.Handle)) Then
									thumbwin.Show(b)
									Exit For
								End If
							Next
						Next
					End SyncLock
				Next
			Else 'Expose mode
				exposeWindows = New List(Of List(Of WindowInfo))
				For Each s As Screen In Screen.AllScreens
					exposeWindows.Add(New List(Of WindowInfo))
				Next
				For Each d As VirtualDesktop In vdm.Desktops
					Dim wins As List(Of WindowInfo) = d.Windows
					SyncLock wins
						For Each w As WindowInfo In wins
							Dim mon As Integer = GetMonitorIndex(Screen.FromHandle(w.Handle))
							exposeWindows(mon).Add(w)
						Next
					End SyncLock
				Next
				For i As Integer = 0 To exposeWindows.Count - 1
					If exposeWindows(i).Count <= 0 Then Continue For
					Dim rows As Integer = Math.Round(Math.Sqrt(exposeWindows(i).Count))
					Dim cols As Integer = Math.Ceiling(CDbl(exposeWindows(i).Count) / CDbl(rows))
					exposeDeskSize = New Size(vdm.PreviewWinSize(i).Width / cols, vdm.PreviewWinSize(i).Height / cols)
					For winInd = 0 To exposeWindows(i).Count - 1
						Dim w = exposeWindows(i)(winInd)
						Dim thumbWin As New SwitcherThumbnailWindow(Me, w, vdm.CurrentDesktop, vdm, True, New Rectangle(ExposeWindowOrigin(winInd, exposeWindows(i).Count, i), exposeDeskSize))
						AddHandler thumbWin.KeyDown, AddressOf switcher_KeyDown
						AddHandler thumbWin.MouseWheel, AddressOf switcher_MouseWheel
						thumbWins.Add(thumbWin)
						For Each b As DoubleBufferedForm In backgroundWins
							If Screen.FromControl(b).Equals(Screen.FromHandle(w.Handle)) Then
								thumbWin.Show(b)
								'thumbWin.Opacity = 255
								Exit For
							End If
						Next
					Next
				Next
			End If

			timFade.Enabled = True

			'Make sure the blackform still has focus
			For Each b As DoubleBufferedForm In backgroundWins
				b.Activate()
			Next

			'Lock the Z-order of the thumbnail windows
			For Each thumb In thumbWins
				thumb.IsZLocked = True
			Next
		End If
	End Sub

	Private Function GetMonitorIndex(scr As Screen) As Integer
		For Each s As Screen In Screen.AllScreens
			If scr.Equals(s) Then Return Array.IndexOf(Screen.AllScreens, s)
		Next
		Return 0
	End Function

	Private Function ExposeWindowOrigin(windowInd As Integer, wincount As Integer, monitor As Integer) As Point
		Dim rows As Integer = Math.Round(Math.Sqrt(wincount))
		Dim cols As Integer = Math.Ceiling(CDbl(wincount) / CDbl(rows))
		Dim deskSize As New Size(vdm.PreviewWinSize(monitor).Width / cols, vdm.PreviewWinSize(monitor).Height / cols)

		Dim offsetY As Integer = (vdm.PreviewWinSize(monitor).Height - rows * deskSize.Height) / 2
		Dim loopDesk As Integer = 0
		For i As Integer = 1 To rows
			If i = rows Then
				Dim offsetX As Integer = (vdm.PreviewWinSize(monitor).Width - ((wincount - ((i - 1) * cols)) * deskSize.Width)) / 2
				For j As Integer = 1 To wincount - ((i - 1) * cols)
					If loopDesk = windowInd Then Return New Point((j - 1) * deskSize.Width + offsetX, (i - 1) * deskSize.Height + offsetY) + Screen.AllScreens(monitor).Bounds.Location
					loopDesk += 1
				Next
			Else
				For j As Integer = 1 To cols
					If loopDesk = windowInd Then Return New Point((j - 1) * deskSize.Width, (i - 1) * deskSize.Height + offsetY) + Screen.AllScreens(monitor).Bounds.Location
					loopDesk += 1
				Next
			End If
		Next
	End Function

	Public Sub HideSwitcher()
		If _switcherVisible Then
			'Fade everything away
			isHiding = True
			timFade.Enabled = True
		End If
	End Sub

	Public Sub SelectNextDesktop()
		'Move in a loop between desktops 1 to 2, ..., 4 to 1
		If selectedItem + 1 = vdm.Desktops.Count Then
			selectedItem = 0
		Else
			selectedItem += 1
		End If

		'Reposition the selection rectangle to show which desktop is selected
		For Each b As DoubleBufferedForm In backgroundWins
			b.Refresh()
		Next
	End Sub

	Public Sub SelectPrevDesktop()
		'Move in a loop between desktops 4 to 3, ..., 1 to 4
		If selectedItem - 1 = -1 Then
			selectedItem = vdm.Desktops.Count - 1
		Else
			selectedItem -= 1
		End If

		'Reposition the selection rectangle to show which desktop is selected
		For Each b As DoubleBufferedForm In backgroundWins
			b.Refresh()
		Next
	End Sub

	Public Sub SelectUpDesktop()
		Dim rows As Integer = Math.Round(Math.Sqrt(vdm.Desktops.Count))
		Dim cols As Integer = Math.Ceiling(CDbl(vdm.Desktops.Count) / CDbl(rows))
		Dim nextdesk As Integer = Math.Floor(selectedItem / rows)
		nextdesk = (selectedItem - nextdesk * cols) + (nextdesk - 1) * cols
		If nextdesk < 0 Then
			nextdesk = Math.Min(selectedItem + (rows - 1) * cols, vdm.Desktops.Count - 1)
		End If
		selectedItem = nextdesk

		'Reposition the selection rectangle to show which desktop is selected
		For Each b As DoubleBufferedForm In backgroundWins
			b.Refresh()
		Next
	End Sub

	Public Sub SelectDownDesktop()
		Dim rows As Integer = Math.Round(Math.Sqrt(vdm.Desktops.Count))
		Dim cols As Integer = Math.Ceiling(CDbl(vdm.Desktops.Count) / CDbl(rows))
		Dim nextdesk As Integer = Math.Floor(selectedItem / rows)
		nextdesk = (selectedItem - nextdesk * cols) + (nextdesk + 1) * cols
		If nextdesk >= vdm.Desktops.Count Then
			nextdesk = selectedItem - Math.Floor(selectedItem / rows) * cols
		End If
		selectedItem = nextdesk

		'Reposition the selection rectangle to show which desktop is selected
		For Each b As DoubleBufferedForm In backgroundWins
			b.Refresh()
		Next
	End Sub

	Public Sub SwitchToSelection()
		If Not ExposeMode Then
			'When the user decides to switch desktops, switch and hide the switcher
			vdm.CurrentDesktop = vdm.Desktops(selectedItem)
			HideSwitcher()
		End If
	End Sub

	Dim isHiding As Boolean = False
	Dim selectedItem As Integer = 0

	Dim _opacity As Double = 0

	Private Property BackgroundWinOpacity() As Double
		Get
			Return _opacity
		End Get
		Set(value As Double)
			For Each b As DoubleBufferedForm In backgroundWins
				b.Opacity = value
			Next
			_opacity = value
		End Set
	End Property

	Private Sub timFade_Tick(sender As System.Object, e As System.EventArgs) Handles timFade.Tick
		If isHiding Then
			If BackgroundWinOpacity <= 0 Then 'If the black form has faded away
				timFade.Enabled = False	'Stop animating
				For Each w As SwitcherThumbnailWindow In thumbWins 'Close the thumbnail windows
					w.Close()
				Next
				For Each b As DoubleBufferedForm In backgroundWins 'Close all the switcher windows
					b.Close()
				Next
				backgroundWins.Clear() 'Delete all of the switcher windows
				_switcherVisible = False 'Allow the switcher to fade back in
				thumbWins.Clear() 'Delete all of the thumbnail windows
			Else
				BackgroundWinOpacity -= My.Settings.FadeSpeed	'Continue the animation
				For Each w As SwitcherThumbnailWindow In thumbWins
					w.Opacity = BackgroundWinOpacity / 0.8 * 255 'Fade the thumbnails from 1 to 0 opacity
					'w.Refresh()
				Next
			End If
		Else
			If BackgroundWinOpacity >= 0.8 OrElse My.Settings.FadeSpeed >= 0.8 Then	'If the black form has faded in
				timFade.Enabled = False	'Stop animating
				For Each w As SwitcherThumbnailWindow In thumbWins 'Make sure the thumbnails are at full opacity
					w.Refresh()
					w.Opacity = 255
				Next
				For Each b As DoubleBufferedForm In backgroundWins
					b.Opacity = 0.8
					b.Focus() 'Focus the black form just in case
				Next
			Else
				BackgroundWinOpacity += My.Settings.FadeSpeed	'Continue the animation
				For Each w As SwitcherThumbnailWindow In thumbWins 'Fade the thumbnails from 0 to 1 opacity
					w.Opacity = BackgroundWinOpacity / 0.8 * 255
				Next
			End If
		End If

	End Sub

	Private Sub switcher_KeyDown(sender As Object, e As System.Windows.Forms.KeyEventArgs)
		If e.KeyData = Keys.Escape Then
			HideSwitcher()
		ElseIf Not ExposeMode Then
			If e.KeyData = Keys.Right Then
				SelectNextDesktop()
			ElseIf e.KeyData = Keys.Down Then
				SelectDownDesktop()
			ElseIf e.KeyData = Keys.Left Then
				SelectPrevDesktop()
			ElseIf e.KeyData = Keys.Up Then
				SelectUpDesktop()
			ElseIf e.KeyData = Keys.Enter Then
				SwitchToSelection()
			End If
		End If
	End Sub

	Private Sub switcher_MouseDoubleClick(sender As Object, e As MouseEventArgs)
		Dim desk As Integer = DesktopFromPoint(e.Location, backgroundWins.IndexOf(sender))
		If desk <> -1 Then
			selectedItem = desk
			SwitchToSelection()
		End If
	End Sub

	Private Sub switcher_MouseClick(sender As Object, e As MouseEventArgs)
		If e.Button = MouseButtons.Right Then
			'TODO: Possibly allow a desktop rename here
			'Dim desk As Integer = DesktopFromPoint(e.Location, backgroundWins.IndexOf(sender))
			'If desk <> -1 Then
			'    selectedItem = desk
			'    Dim newName As String = InputBox("Enter a new name for this desktop:", "Desktop Name", VirtualDesktopManager.Desktops(desk).Name)
			'    If newName <> "" Then
			'        VirtualDesktopManager.Desktops(desk).Name = newName
			'        For Each b As Form In backgroundWins
			'            b.Refresh()
			'        Next
			'    End If
			'End If
		End If
	End Sub

	Private Sub thumb_MouseDown(sender As Object, e As MouseEventArgs)
		If e.Button = MouseButtons.Right Then
			Dim win As SwitcherThumbnailWindow = sender
			ShowWindowMenu(win.Window, win)
		End If
	End Sub

	Private Sub switcher_Paint(sender As Object, e As System.Windows.Forms.PaintEventArgs)
		Dim monBounds = Screen.FromControl(sender).Bounds
		monBounds.X = 0
		monBounds.Y = 0

		Dim selectionRect = vdm.GetDesktopPreviewBounds(selectedItem, monBounds)

		'Draw the desktop selection rectangle
		e.Graphics.FillRectangle(Brushes.LightGray, selectionRect)

		e.Graphics.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAliasGridFit

		'Draw the desktop borders
		For i As Integer = 0 To vdm.Desktops.Count - 1
			Dim r = vdm.GetDesktopPreviewBounds(i, monBounds)
			e.Graphics.DrawRectangle(New Pen(Color.White, 5), r)
			Dim sf As New StringFormat
			sf.Alignment = StringAlignment.Near
			sf.LineAlignment = StringAlignment.Far
			Dim f As New Font(SystemFonts.CaptionFont.FontFamily, 24, FontStyle.Regular, GraphicsUnit.Pixel)
			e.Graphics.DrawString(vdm.Desktops(i).Name, f, If(selectedItem = i, Brushes.Black, Brushes.White), r, sf)
			'TextRenderer.DrawText(e.Graphics, vdm.Desktops(i).Name, f, r, If(selectedItem = i, Color.Black, Color.White), Color.Transparent, TextFormatFlags.Left Or TextFormatFlags.Bottom)
		Next

		'Draw the help text
		e.Graphics.DrawString("Press enter to switch to the selected desktop.", SystemFonts.SmallCaptionFont, If(selectedItem = 0, Brushes.Black, Brushes.White), 10, 10)

	End Sub

	Private Sub switcher_MouseWheel(sender As Object, e As MouseEventArgs)
		If e.Delta > 0 Then
			selectedItem += 1
		ElseIf e.Delta < 0 Then
			selectedItem -= 1
		End If
		If selectedItem < 0 Then
			selectedItem = vdm.Desktops.Count - 1
		ElseIf selectedItem >= vdm.Desktops.Count Then
			selectedItem = 0
		End If

		For Each b As Form In backgroundWins
			b.Refresh()
		Next
	End Sub

	''' <summary>
	''' Returns the desktop index at the specified point on the selection black form.
	''' </summary>
	''' <param name="p"></param>
	''' <returns></returns>
	''' <remarks></remarks>
	Public Function DesktopFromPoint(p As Point, monitor As Integer) As Integer
		Dim monBounds = Screen.AllScreens(monitor).Bounds
		monBounds.X = 0
		monBounds.Y = 0
		For i As Integer = 0 To vdm.Desktops.Count - 1
			If vdm.GetDesktopPreviewBounds(i, monBounds).Contains(p) Then Return i
		Next
		Return -1
	End Function

	Dim _activeWindow As WindowInfo

	''' <summary>
	''' Displays a context menu at the cursor's location for the foreground window.
	''' </summary>
	''' <remarks></remarks>
	Public Sub ShowWindowMenu()
		Dim w As WindowInfo = WindowInfo.GetForegroundWindow
		ShowWindowMenu(w, Nothing)
	End Sub

	Public Sub ShowWindowMenu(w As WindowInfo, parent As Control)
		If vdm.IsWindowValid(w, True) Then
			_activeWindow = w
			Dim isSticky As Boolean = vdm.IsSticky(w)
			Dim mnuWin As New ContextMenuStrip
			AddHandler mnuWin.ItemClicked, AddressOf WindowMenuItem_Click
			Dim mnuWinName As New ToolStripMenuItem(w.Text)
			mnuWinName.Enabled = False
			mnuWinName.Font = New Font(mnuWinName.Font, FontStyle.Bold)
			mnuWin.Items.Add(mnuWinName)
			If Thumbnail.IsDWMEnabled Then mnuWin.Items.Add(New ToolStripMenuItem("Show &Thumbnail...", Nothing, Nothing, "thumbnail"))
			mnuWin.Items.Add(New ToolStripSeparator)
			If isSticky Then
				Dim mnuUnstick As New ToolStripMenuItem("Unstick this Window")
				mnuUnstick.Name = "unstick"
				mnuUnstick.ToolTipText = "Causes this window to only appear on the current desktop."
				mnuWin.Items.Add(mnuUnstick)
			Else
				Dim mnuStick As New ToolStripMenuItem("Make this Window Sticky")
				mnuStick.Name = "stick"
				mnuStick.ToolTipText = "Causes this window to appear on all desktops."
				mnuWin.Items.Add(mnuStick)

				mnuWin.Items.Add(New ToolStripSeparator)
				For i As Integer = 1 To vdm.Desktops.Count
					Dim mnuDesk As New ToolStripMenuItem("Send Window to Desktop " & i.ToString)
					mnuDesk.Name = (i - 1).ToString
					If i - 1 = vdm.CurrentDesktopIndex Then mnuDesk.Enabled = False
					mnuWin.Items.Add(mnuDesk)
				Next

				Dim pid = w.ProcessId
				mnuWin.Items.Add(New ToolStripSeparator)
				For i As Integer = 0 To vdm.Desktops.Count
					Dim mnuDesk As New ToolStripMenuItem(If(i = 0, "Unpin process", "Pin process to desktop " & i.ToString))
					mnuDesk.Name = "TempPinPid#" & pid & "#" & (i - 1)
					mnuWin.Items.Add(mnuDesk)
				Next

			End If
			mnuWin.ShowItemToolTips = True
			If parent Is Nothing Then
				mnuWin.Show(Control.MousePosition, ToolStripDropDownDirection.Default)
			Else
				mnuWin.Show(parent, parent.PointToClient(Control.MousePosition), ToolStripDropDownDirection.Default)
			End If
			Dim mnuw As New WindowInfo(mnuWin.Handle)
			mnuw.BringToFront()
		End If
	End Sub

	Private Sub WindowMenuItem_Click(sender As Object, e As ToolStripItemClickedEventArgs)
		If _activeWindow.IsValid Then
			Select Case e.ClickedItem.Name
				Case "unstick"
					vdm.UnstickWindow(_activeWindow)
				Case "stick"
					vdm.StickWindow(_activeWindow)
				Case "thumbnail"
					Dim tform As New ThumbnailToolForm(_activeWindow)
					tform.Show()
				Case Else
					If e.ClickedItem.Name.StartsWith("TempPinPid") Then
						vdm.SendGlobalCommand(e.ClickedItem.Name)
					Else
						Dim desktopNum As Integer
						If Integer.TryParse(e.ClickedItem.Name, desktopNum) Then
							Try
								'Move the window to the selected desktop
								vdm.SendWindowToDesktop(_activeWindow, vdm.CurrentDesktopIndex, desktopNum)
							Catch ex As Exception

							End Try
						End If
					End If
			End Select
		End If
	End Sub

End Class