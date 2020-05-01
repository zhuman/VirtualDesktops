''' <summary>
''' Implements a toolbar that displays icons for each desktop that can be used to switch.
''' </summary>
''' <remarks></remarks>
Public Class IconToolbar
	Inherits ZPixel.LayeredForm

	WithEvents fadeTim As New Timer
	Friend WithEvents mnuDesktop As System.Windows.Forms.ContextMenuStrip
	Private components As System.ComponentModel.IContainer
	Friend WithEvents SwitchToDesktopToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
	Friend WithEvents ToolStripSeparator1 As System.Windows.Forms.ToolStripSeparator
	Friend WithEvents SendAllWindowsToDesktopToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
	Friend WithEvents SendCurrentWindowsToDesktopToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
	Friend WithEvents SendDesktopWindowsToCurrentDesktopToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
	Dim isVertical As Boolean
	Dim vdm As VirtualDesktopManager

	Public Sub New(vdm As VirtualDesktopManager)
		Me.vdm = vdm
		Me.Image = GenerateImage()
		Me.Opacity = 0
		Me.TopMost = True
		Me.ShowInTaskbar = False
		Me.ControlBox = False
		'Me.FormBorderStyle = Windows.Forms.FormBorderStyle.SizableToolWindow
		Me.Text = "Virtual Desktops"
		fadeTim.Interval = 50
		AddHandler vdm.VirtualDesktopSwitched, AddressOf DesktopSwitched
		InitializeComponent()
	End Sub

	Protected Overrides ReadOnly Property CreateParams() As System.Windows.Forms.CreateParams
		Get
			Dim cp As CreateParams = MyBase.CreateParams
			cp.ExStyle = cp.ExStyle Or &H80
			Return cp
		End Get
	End Property

	Private Function GenerateImage() As Bitmap
		Dim b As Bitmap
		Dim f As New Font(SystemFonts.CaptionFont.FontFamily, 12, GraphicsUnit.Pixel)
		If isVertical Then
			b = New Bitmap(16 + 4 + 4, 4 + 8 + 4 + vdm.Desktops.Count * (16 + 4), Imaging.PixelFormat.Format32bppPArgb)
		Else
			b = New Bitmap(4 + 8 + 4 + vdm.Desktops.Count * (16 + 4), 16 + 4 + 4, Imaging.PixelFormat.Format32bppPArgb)
		End If

		Using gr As Graphics = Graphics.FromImage(b)
			gr.SmoothingMode = Drawing2D.SmoothingMode.HighQuality
			gr.PixelOffsetMode = Drawing2D.PixelOffsetMode.Half
			gr.FillPath(New SolidBrush(Color.FromArgb(200, 0, 0, 0)), ZPixel.GraphicsRenderer.GetRoundedRect(New RectangleF(0, 0, b.Width, b.Height), 4))
			gr.PixelOffsetMode = Drawing2D.PixelOffsetMode.Default
			gr.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAliasGridFit

			For i As Integer = 0 To vdm.Desktops.Count - 1
				Dim r As New Rectangle(0, 0, 16, 16)
				If isVertical Then
					r.Location = New Point(4, 4 + 8 + i * (16 + 4))
				Else
					r.Location = New Point(4 + 8 + i * (16 + 4), 4)
				End If
				If i = vdm.CurrentDesktopIndex Then
					gr.FillEllipse(Brushes.White, r)
				Else
					If Me.RectangleToScreen(r).Contains(Control.MousePosition) Then
						gr.FillEllipse(New SolidBrush(Color.FromArgb(100, 255, 255, 255)), r)
					End If
					gr.DrawEllipse(Pens.White, r)
				End If
				gr.DrawString((i + 1).ToString, f, If(i = vdm.CurrentDesktopIndex, Brushes.Black, Brushes.White), r, New StringFormat(StringFormatFlags.NoWrap Or StringFormatFlags.NoClip) With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center})
				'gr.DrawImage(Main.GetDesktopTrayIcon(i, i = VirtualDesktopManager.CurrentDesktopIndex).ToBitmap, p)
			Next
			For i As Integer = 0 To 2
				Dim r As RectangleF
				If isVertical Then
					r = New RectangleF(5 + i / 3 * 14, 4, 2, 2)
				Else
					r = New RectangleF(4, 5 + i / 3 * 14, 2, 2)
				End If
				gr.FillEllipse(New SolidBrush(Color.FromArgb(220, 220, 220)), r)
			Next
		End Using
		Return b
	End Function

	Private Sub IconToolbar_FormClosing(sender As Object, e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
		My.Settings.IconToolbarPosition = Me.Location
		My.Settings.IconToolbarIsVertical = isVertical
	End Sub

	Dim selectedIndex As Integer

	Private Sub IconToolbar_MouseClick(sender As Object, e As System.Windows.Forms.MouseEventArgs) Handles Me.MouseClick
		If isVertical Then
			selectedIndex = Math.Floor((e.Y - 8 - 4) / (16 + 4))
		Else
			selectedIndex = Math.Floor((e.X - 8 - 4) / (16 + 4))
		End If

		If selectedIndex >= 0 And selectedIndex < vdm.Desktops.Count Then
			If e.Button = Windows.Forms.MouseButtons.Left Then
				vdm.CurrentDesktopIndex = selectedIndex
			ElseIf e.Button = Windows.Forms.MouseButtons.Right Then
				Me.SendCurrentWindowsToDesktopToolStripMenuItem.Enabled = Not (vdm.CurrentDesktopIndex = selectedIndex)
				Me.SendDesktopWindowsToCurrentDesktopToolStripMenuItem.Enabled = Not (vdm.CurrentDesktopIndex = selectedIndex)
				mnuDesktop.Show(Control.MousePosition)
			End If
		End If
		Me.Image = GenerateImage()
	End Sub

	Dim isDragging As Boolean = False
	Dim dragStartPoint As Point

	Private Sub IconToolbar_MouseDoubleClick(sender As Object, e As System.Windows.Forms.MouseEventArgs) Handles Me.MouseDoubleClick
		If e.Button = Windows.Forms.MouseButtons.Left AndAlso ((isVertical And e.Y < 4 + 6) OrElse (e.X < 4 + 6)) Then
			isVertical = Not isVertical
			Me.Image = GenerateImage()
			Me.Location = CorrectPoint(Me.Location, Me.Image.Size)
		End If
	End Sub

	Private Sub IconToolbar_MouseDown(sender As Object, e As System.Windows.Forms.MouseEventArgs) Handles Me.MouseDown
		If e.Button = Windows.Forms.MouseButtons.Left AndAlso ((isVertical And e.Y < 4 + 6) OrElse (e.X < 4 + 6)) Then
			isDragging = True
			dragStartPoint = e.Location
		End If
	End Sub

	Private Sub IconToolbar_MouseLeave(sender As Object, e As System.EventArgs) Handles Me.MouseLeave
		Me.Image = GenerateImage()
	End Sub

	Private Sub IconToolbar_MouseMove(sender As Object, e As System.Windows.Forms.MouseEventArgs) Handles Me.MouseMove
		If isDragging AndAlso e.Button = Windows.Forms.MouseButtons.Left Then
			Me.Location = CorrectPoint(Control.MousePosition - dragStartPoint, Me.Image.Size)
		Else
			Me.Image = GenerateImage()
		End If
	End Sub

	Public Shared Function CorrectPoint(p As Point, size As Size) As Point
		Return CorrectPoint(p, size, Screen.FromPoint(p).WorkingArea)
	End Function

	Public Shared Function CorrectPoint(p As Point, size As Size, workingArea As Rectangle) As Point
		If p.X + size.Width + 5 > workingArea.Right Then p.X = workingArea.Right - size.Width
		If p.X - 5 < workingArea.Left Then p.X = workingArea.Left
		If p.Y + size.Height + 5 > workingArea.Bottom Then p.Y = workingArea.Bottom - size.Height
		If p.Y - 5 < workingArea.Top Then p.Y = workingArea.Top
		Return p
	End Function

	Private Sub IconToolbar_MouseUp(sender As Object, e As System.Windows.Forms.MouseEventArgs) Handles Me.MouseUp
		isDragging = False
	End Sub

	Private Sub IconToolbar_Shown(sender As Object, e As System.EventArgs) Handles Me.Shown
		fadeTim.Enabled = True
		Me.TopMost = True
		If My.Settings.IconToolbarPosition = New Point(-3000, -3000) Then
			My.Settings.IconToolbarPosition = New Point(Screen.PrimaryScreen.WorkingArea.Right - Me.Image.Width, Screen.PrimaryScreen.WorkingArea.Bottom - Me.Image.Height)
		End If
		isVertical = My.Settings.IconToolbarIsVertical
		Me.Image = GenerateImage()
		Me.Location = CorrectPoint(My.Settings.IconToolbarPosition, Me.Image.Size)
	End Sub

	Private Sub fadeTim_Tick(sender As Object, e As System.EventArgs) Handles fadeTim.Tick
		If Me.Opacity < 240 Then
			Me.Opacity += 10
		Else
			Me.Opacity = 255
			fadeTim.Enabled = False
		End If
	End Sub

	Private Sub InitializeComponent()
		Me.components = New System.ComponentModel.Container
		Me.mnuDesktop = New System.Windows.Forms.ContextMenuStrip(Me.components)
		Me.SendAllWindowsToDesktopToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
		Me.SendCurrentWindowsToDesktopToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
		Me.SendDesktopWindowsToCurrentDesktopToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
		Me.SwitchToDesktopToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
		Me.ToolStripSeparator1 = New System.Windows.Forms.ToolStripSeparator
		Me.mnuDesktop.SuspendLayout()
		Me.SuspendLayout()
		'
		'mnuDesktop
		'
		Me.mnuDesktop.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.SwitchToDesktopToolStripMenuItem, Me.ToolStripSeparator1, Me.SendAllWindowsToDesktopToolStripMenuItem, Me.SendCurrentWindowsToDesktopToolStripMenuItem, Me.SendDesktopWindowsToCurrentDesktopToolStripMenuItem})
		Me.mnuDesktop.Name = "ContextMenuStrip1"
		Me.mnuDesktop.Size = New System.Drawing.Size(302, 120)
		'
		'SendAllWindowsToDesktopToolStripMenuItem
		'
		Me.SendAllWindowsToDesktopToolStripMenuItem.Name = "SendAllWindowsToDesktopToolStripMenuItem"
		Me.SendAllWindowsToDesktopToolStripMenuItem.Size = New System.Drawing.Size(301, 22)
		Me.SendAllWindowsToDesktopToolStripMenuItem.Text = "Send &All Windows to Desktop"
		'
		'SendCurrentWindowsToDesktopToolStripMenuItem
		'
		Me.SendCurrentWindowsToDesktopToolStripMenuItem.Name = "SendCurrentWindowsToDesktopToolStripMenuItem"
		Me.SendCurrentWindowsToDesktopToolStripMenuItem.Size = New System.Drawing.Size(301, 22)
		Me.SendCurrentWindowsToDesktopToolStripMenuItem.Text = "Send &Current Windows to Desktop"
		'
		'SendDesktopWindowsToCurrentDesktopToolStripMenuItem
		'
		Me.SendDesktopWindowsToCurrentDesktopToolStripMenuItem.Name = "SendDesktopWindowsToCurrentDesktopToolStripMenuItem"
		Me.SendDesktopWindowsToCurrentDesktopToolStripMenuItem.Size = New System.Drawing.Size(301, 22)
		Me.SendDesktopWindowsToCurrentDesktopToolStripMenuItem.Text = "Send &Desktop Windows to Current Desktop"
		'
		'SwitchToDesktopToolStripMenuItem
		'
		Me.SwitchToDesktopToolStripMenuItem.Name = "SwitchToDesktopToolStripMenuItem"
		Me.SwitchToDesktopToolStripMenuItem.Size = New System.Drawing.Size(301, 22)
		Me.SwitchToDesktopToolStripMenuItem.Text = "&Switch to Desktop"
		'
		'ToolStripSeparator1
		'
		Me.ToolStripSeparator1.Name = "ToolStripSeparator1"
		Me.ToolStripSeparator1.Size = New System.Drawing.Size(298, 6)
		'
		'IconToolbar
		'
		Me.ClientSize = New System.Drawing.Size(300, 300)
		Me.Name = "IconToolbar"
		Me.mnuDesktop.ResumeLayout(False)
		Me.ResumeLayout(False)

	End Sub

	Private Sub SendAllWindowsToDesktopToolStripMenuItem_Click(sender As Object, e As System.EventArgs) Handles SendAllWindowsToDesktopToolStripMenuItem.Click
		For Each d As VirtualDesktop In vdm.Desktops
			If d IsNot vdm.Desktops(selectedIndex) Then
				If vdm.Desktops(selectedIndex) Is vdm.CurrentDesktop Then
					d.ShowWindows(False)
					d.Windows.Clear()
				ElseIf d Is vdm.CurrentDesktop Then
					d.HideWindows()
					Dim windows As List(Of WindowInfo) = d.Windows
					SyncLock windows
						For Each w As WindowInfo In windows
							vdm.Desktops(selectedIndex).Windows.Add(w)
						Next
					End SyncLock
					d.Windows.Clear()
				Else
					Dim windows As List(Of WindowInfo) = d.Windows
					SyncLock windows
						For Each w As WindowInfo In windows
							vdm.Desktops(selectedIndex).Windows.Add(w)
						Next
					End SyncLock
					d.Windows.Clear()
				End If
			End If
		Next
	End Sub

	Private Sub SendCurrentWindowsToDesktopToolStripMenuItem_Click(sender As Object, e As System.EventArgs) Handles SendCurrentWindowsToDesktopToolStripMenuItem.Click
		Dim windows As List(Of WindowInfo) = vdm.CurrentDesktop.Windows
		SyncLock windows
			For Each w As WindowInfo In windows
				vdm.Desktops(selectedIndex).Windows.Add(w)
				Try
					w.State = WindowInfo.WindowState.Hide
				Catch ex As Exception

				End Try
			Next
		End SyncLock
	End Sub

	Private Sub SendDesktopWindowsToCurrentDesktopToolStripMenuItem_Click(sender As Object, e As System.EventArgs) Handles SendDesktopWindowsToCurrentDesktopToolStripMenuItem.Click
		Dim windows As List(Of WindowInfo) = vdm.Desktops(selectedIndex).Windows
		SyncLock windows
			For Each w As WindowInfo In windows
				Try
					w.State = WindowInfo.WindowState.Show
				Catch ex As Exception

				End Try
			Next
			vdm.Desktops(selectedIndex).Windows.Clear()
		End SyncLock
	End Sub

	Private Sub SwitchToDesktopToolStripMenuItem_Click(sender As Object, e As System.EventArgs) Handles SwitchToDesktopToolStripMenuItem.Click
		vdm.CurrentDesktopIndex = selectedIndex
		Me.Image = GenerateImage()
	End Sub

	Private Delegate Sub EmptyDelegate()

	Private Sub DesktopSwitched()
		If Not Me.IsDisposed Then
			If Me.InvokeRequired Then
				Me.Invoke(New EmptyDelegate(AddressOf DesktopSwitched))
			Else
				Me.Image = GenerateImage()
			End If
		End If
	End Sub

End Class

''' <summary>
''' Implements the icon toolbar plug-in.
''' </summary>
''' <remarks></remarks>
Public Class IconToolbarPlugin
	Inherits VirtualDesktopPlugin

	Dim appContext As ApplicationContext

	Public Sub New(vdm As VirtualDesktopManager)
		MyBase.New(vdm)
	End Sub

	Public Overrides Sub Start()
		Dim t As New Threading.Thread(AddressOf IconToolbarThread)
		t.Start()
	End Sub

	Public Overrides Sub [Stop]()
		If appContext IsNot Nothing Then
			appContext.ExitThread()
		End If
	End Sub

	Private Sub IconToolbarThread()
		If My.Settings.UseIconToolbar Then
			appContext = New ApplicationContext(New IconToolbar(VirtualDesktopManager))
			Windows.Forms.Application.Run(appContext)
		End If
	End Sub

End Class