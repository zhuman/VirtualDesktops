Imports System.Runtime.InteropServices

''' <summary>
''' Implements an Aero-themed preview window that appears on hover over the main tray icon.
''' </summary>
''' <remarks></remarks>
Public Class AeroMiniPreviewWindow
	Inherits GlassForm

	Const outerBorderNum As Integer = 8
	Const innerBorderNum As Integer = 2
	WithEvents timFade As New Timer()
	Dim monitor As Integer
	WithEvents renderer As New System.ComponentModel.BackgroundWorker
	Dim vdm As VirtualDesktopManager
	Dim ui As MainUiPlugin

	Public Sub New(vdm As VirtualDesktopManager, ui As MainUiPlugin)
		Me.vdm = vdm
		Me.ui = ui

		Me.SetStyle(ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw Or ControlStyles.AllPaintingInWmPaint Or ControlStyles.SupportsTransparentBackColor, True)
		Me.FormBorderStyle = Windows.Forms.FormBorderStyle.Sizable
		Me.ControlBox = False
		Me.ShowIcon = False
		Me.Text = "Desktop Switcher"
		Me.ShowInTaskbar = False
		Me.TopMost = True

		monitor = Array.IndexOf(Screen.AllScreens, Screen.PrimaryScreen)

		'For Each s As Screen In Screen.AllScreens
		'	If Array.IndexOf(VirtualDesktopManager.MonitorIndices, Array.IndexOf(Screen.AllScreens, s)) >= 0 Then
		'		If s.Bounds.Width * s.Bounds.Height > Screen.AllScreens(monitor).Bounds.Width * Screen.AllScreens(monitor).Bounds.Height Then
		'			monitor = Array.IndexOf(Screen.AllScreens, s)
		'		End If
		'	End If
		'Next
		Me.ClientSize = New Size(My.Computer.Screen.Bounds.Width / My.Settings.MiniPrevSize, My.Computer.Screen.Bounds.Height / My.Settings.MiniPrevSize)
		Me.Opacity = 0
	End Sub

	Protected Overrides ReadOnly Property CreateParams As System.Windows.Forms.CreateParams
		Get
			Dim cp As CreateParams = MyBase.CreateParams
			cp.Style = cp.Style And (Not &HC00000) '&H86000000 'cp.Style And (Not &H40000)
			'cp.ExStyle = &H80088
			Return cp
		End Get
	End Property

	Private Const WM_NCHITTEST As Integer = 132
	Private Const WM_NCCALCSIZE As Integer = &H83
	Private Const HTCLIENT As Integer = 1

	Protected Overrides Sub WndProc(ByRef m As System.Windows.Forms.Message)
		If m.Msg = WM_NCHITTEST Then
			m.Result = HTCLIENT
		ElseIf m.Msg = WM_NCCALCSIZE Then
			If m.WParam Then
				m.Result = 0
			Else
				MyBase.WndProc(m)
			End If
		Else
			MyBase.WndProc(m)
		End If
	End Sub

	Private Property PreviewImage As Bitmap

	Private Function RenderDesktops() As Image
		Dim b As New Bitmap(Me.ClientSize.Width, Me.ClientSize.Height, Imaging.PixelFormat.Format32bppPArgb)
		Dim gr As Graphics = Graphics.FromImage(b)
		gr.SmoothingMode = Drawing2D.SmoothingMode.HighQuality

		For i As Integer = 0 To vdm.Desktops.Count - 1
			Try
				Dim rect As Rectangle = CalcPrevRect(i)
				'gr.SetClip(ZPixel.GraphicsRenderer.GetRoundedRect(rect, 5))
				'gr.FillPath(New SolidBrush(Color.FromArgb(If(i = VirtualDesktopManager.CurrentDesktopIndex, 200, 100), 200, 200, 200)), ZPixel.GraphicsRenderer.GetRoundedRect(rect, borderNum))
				Dim wins As List(Of WindowInfo) = vdm.Desktops(i).Windows
				SyncLock wins
					For Each w As WindowInfo In wins
						If vdm.IsWindowValid(w, False, True) Then
							Dim r As Rectangle = CalcWinPrevRect(w, rect)
							r.Width -= 1
							r.Height -= 1
							'gr.FillRectangle(New SolidBrush(Color.FromArgb(200, 100, 100, 100)), r)
							gr.FillPath(New SolidBrush(Color.FromArgb(200, 100, 100, 100)), ZPixel.GraphicsRenderer.GetRoundedRect(r, 5))
							Try
								Dim ico As Icon = w.Icon(WindowInfo.WindowIconSize.Small2)
								gr.DrawIcon(ico, New Rectangle(r.Location + New Point(r.Width / 2 - 8, r.Height / 2 - 8), New Size(16, 16)))
								ico.Dispose()
							Catch ex As Exception

							End Try
						End If
					Next
				End SyncLock

				'Dim sf As New StringFormat
				'sf.Alignment = StringAlignment.Near
				'sf.LineAlignment = StringAlignment.Far
				'gr.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAliasGridFit
				'Dim f As New Font(SystemFonts.CaptionFont.FontFamily, Math.Max(10, rect.Height / 7), FontStyle.Regular, GraphicsUnit.Pixel)
				'gr.DrawString(VirtualDesktopManager.Desktops(i).Name, f, If(VirtualDesktopManager.CurrentDesktopIndex = i, Brushes.Black, Brushes.White), rect, sf)
				'GlassForm.DrawGlassText(gr, VirtualDesktopManager.Desktops(i).Name, f, rect, TextFormatFlags.Left Or TextFormatFlags.Bottom Or TextFormatFlags.SingleLine, 5)
			Catch ex As Exception

			End Try
		Next

		gr.Dispose()

		Return b
	End Function

	Private Function CalcPrevRect(desktop As Integer) As Rectangle
		Dim monBounds As New Rectangle(New Point(outerBorderNum, outerBorderNum), Me.ClientSize - New Size(2 * outerBorderNum - innerBorderNum, 2 * outerBorderNum - innerBorderNum))
		Dim rect = vdm.GetDesktopPreviewBounds(desktop, monBounds)
		rect.Width -= innerBorderNum
		rect.Height -= innerBorderNum
		Return rect
	End Function

	Private Function CalcWinPrevRect(w As WindowInfo, deskRect As Rectangle) As Rectangle
		Dim pos As Point
		Dim s As Size
		Dim scr As Rectangle = Screen.FromHandle(w.Handle).Bounds
		Dim normRect As Rectangle = w.NormalRectangle
		If w.Maximized Then
			Return deskRect
		ElseIf w.Minimized Then
			pos = New Point((normRect.Left - scr.X) / scr.Width * deskRect.Width + deskRect.X, (normRect.Top - scr.Y) / scr.Height * deskRect.Height + deskRect.Y)
			s = New Size(normRect.Width / scr.Width * deskRect.Width, normRect.Height / scr.Height * deskRect.Height)
		Else
			pos = New Point((w.Left - scr.X) / scr.Width * deskRect.Width + deskRect.X, (w.Top - scr.Y) / scr.Height * deskRect.Height + deskRect.Y)
			s = New Size(w.Width / scr.Width * deskRect.Width, w.Height / scr.Height * deskRect.Height)
		End If
		Return New Rectangle(pos, s)
	End Function

	Dim _atMouse As Boolean
	Dim _lastShowTime As DateTime
	Dim _firstShowTime As DateTime

	Public Sub ShowPreview(Optional atMouse As Boolean = False)
		_lastShowTime = DateTime.Now
		If Not Me.Visible Then
			_firstShowTime = _lastShowTime
			isHiding = False
			PositionMe(atMouse)
			Me.targetY = Me.Top
			If Not renderer.IsBusy Then renderer.RunWorkerAsync()
			Me.Opacity = 0
			Me.Refresh()
			Me.Show()
			Me.Activate()
			PositionMe(atMouse)
			'Dim w As New WindowInfo(Me.Handle)
			'w.BringToFront()

			Try
				timFade.Interval = 5
				timFade.Enabled = True
			Catch ex As Exception

			End Try
		End If
	End Sub

	Private Sub PositionMe(atMouse As Boolean)
		Me.CreateControl()
		_atMouse = atMouse

		Me.Location = Control.MousePosition - New Point(Me.Width / 2, Me.Height / 2)
		Dim workingArea As Rectangle

		If Not atMouse Then
			workingArea = Screen.PrimaryScreen.WorkingArea

			Dim w As WindowInfo = WindowInfo.FindWindowByClass("Shell_TrayWnd")
			If w.IsValid AndAlso Screen.PrimaryScreen.WorkingArea.IntersectsWith(w.Rectangle) Then
				If w.Top > workingArea.Top + workingArea.Height / 2 Then
					workingArea.Height -= w.Height
				ElseIf w.Left > workingArea.Left + workingArea.Width / 2 Then
					workingArea.Width -= w.Width
				ElseIf w.Rectangle.Right > workingArea.Left + workingArea.Width / 2 Then
					workingArea.Y += w.Height
					workingArea.Height -= w.Height
				ElseIf w.Rectangle.Bottom > workingArea.Top + workingArea.Height / 2 Then
					workingArea.X += w.Width
					workingArea.Width -= w.Width
				End If
			End If

			workingArea.Inflate(-10, -10)
		Else
			workingArea = Screen.FromPoint(Me.Location).WorkingArea
		End If

		Me.Location = IconToolbar.CorrectPoint(Me.Location, Me.Size, workingArea)
	End Sub

	Private Function EaseInterpolate(t As Double) As Double
		Return Math.Sin(t * Math.PI / 2)
	End Function

	Dim isHiding As Boolean
	Dim targetY As Integer
	Dim _hideStartTime As DateTime

	Private Sub timFade_Tick(sender As Object, e As System.EventArgs) Handles timFade.Tick
		Try
			Dim showTime As Double = (DateTime.Now - _firstShowTime).TotalSeconds
			Dim shortShowTime As Double = (DateTime.Now - _lastShowTime).TotalSeconds
			Dim interpShow As Double = EaseInterpolate(showTime / 0.2)

			'If shortShowTime > 0.5 Then
			'    If Not isHiding AndAlso Not New Rectangle(Me.Left, Me.Top, Me.ClientSize.Width, Me.ClientSize.Height).Contains(Control.MousePosition) Then
			'        _hideStartTime = Now
			'        isHiding = True
			'    End If
			'Else
			'    isHiding = False
			'End If

			Dim hideTime As Double = (DateTime.Now - _hideStartTime).TotalSeconds
			Dim interpHide As Double = 1 - EaseInterpolate(1 - hideTime / 0.2)
			If isHiding Then
				If hideTime < 0.2 Then
					Me.Opacity = 1 - interpHide
					Me.Top = targetY + interpHide * 10
				Else
					Me.Hide()
					timFade.Enabled = False
				End If
			ElseIf showTime < 0.2 Then
				Me.Opacity = interpShow
				Me.Top = targetY + (1 - interpShow) * 10
			Else
				Me.Opacity = 1
				Me.Top = targetY
				timFade.Enabled = False
			End If
		Catch ex As Exception

		End Try
	End Sub

	Private Sub AeroMiniPreviewWindow_HandleCreated(sender As Object, e As System.EventArgs) Handles Me.HandleCreated
		'Me.EnableNcRendering()
		Me.EnableBlur()
		Me.GlassMargin = New Padding(-1)
	End Sub

	Private Sub AeroMiniPreviewWindow_LostFocus(sender As Object, e As System.EventArgs) Handles Me.LostFocus
		isHiding = True
		_hideStartTime = Now
		timFade.Enabled = True
	End Sub

	Private Sub MiniPreviewWindow_MouseClick(sender As Object, e As System.Windows.Forms.MouseEventArgs) Handles Me.MouseClick
		For i As Integer = 0 To vdm.Desktops.Count - 1
			If CalcPrevRect(i).Contains(e.Location) Then
				vdm.CurrentDesktopIndex = i
				If _atMouse Then Me.Hide()
				Exit Sub
			End If
		Next
	End Sub

	Private Sub AeroMiniPreviewWindow_MouseDown(sender As Object, e As System.Windows.Forms.MouseEventArgs) Handles Me.MouseDown
		Me.Refresh()
	End Sub

	Private Sub MiniPreviewWindow_MouseMove(sender As Object, e As System.Windows.Forms.MouseEventArgs) Handles Me.MouseMove
		Me.Refresh()
	End Sub

	Private Sub renderer_DoWork(sender As Object, e As System.ComponentModel.DoWorkEventArgs) Handles renderer.DoWork
		e.Result = RenderDesktops()
	End Sub

	Private Sub renderer_RunWorkerCompleted(sender As Object, e As System.ComponentModel.RunWorkerCompletedEventArgs) Handles renderer.RunWorkerCompleted
		Dim prevImg As Bitmap = Me.PreviewImage
		Me.PreviewImage = CType(e.Result, Image)
		If prevImg IsNot Nothing Then
			prevImg.Dispose()
		End If
		Me.Refresh()
	End Sub

	Dim desktopHoverBrush As New SolidBrush(Color.FromArgb(50, Color.White))
	Dim desktopDownBrush As New SolidBrush(Color.FromArgb(100, Color.White))
	Dim desktopSelectedBrush As New SolidBrush(Color.FromArgb(100, SystemColors.Highlight))

	Protected Overrides Sub OnPaint(e As System.Windows.Forms.PaintEventArgs)
		'MyBase.OnPaint(e)
		e.Graphics.Clear(Color.FromArgb(0, 0, 0, 0))
		e.Graphics.SmoothingMode = Drawing2D.SmoothingMode.HighQuality
		Dim winPath As Drawing2D.GraphicsPath = ZPixel.GraphicsRenderer.GetRoundedRect(Me.ClientRectangle, 10)
		e.Graphics.FillPath(New SolidBrush(Color.FromArgb(40, 0, 0, 0)), winPath)

		If PreviewImage IsNot Nothing Then
			e.Graphics.DrawImage(PreviewImage, 0, 0, Me.ClientSize.Width, Me.ClientSize.Height)
		End If

		For i As Integer = 0 To vdm.Desktops.Count - 1
			Dim r As Rectangle = CalcPrevRect(i)
			r.Width -= 1
			r.Height -= 1
			Dim deskPath As Drawing2D.GraphicsPath = ZPixel.GraphicsRenderer.GetRoundedRect(r, 5)

			If i = vdm.CurrentDesktopIndex Then
				e.Graphics.FillPath(desktopSelectedBrush, deskPath)
				e.Graphics.DrawPath(Pens.White, deskPath)
			End If

			If r.Contains(Me.PointToClient(Control.MousePosition)) Then
				If Control.MouseButtons = Windows.Forms.MouseButtons.Left AndAlso Me.Capture Then
					e.Graphics.FillPath(desktopDownBrush, deskPath)
				Else
					e.Graphics.FillPath(desktopHoverBrush, deskPath)
				End If
				e.Graphics.DrawPath(New Pen(Color.FromArgb(127, 255, 255, 255)), deskPath)
			End If
			Dim f As New Font(SystemFonts.CaptionFont.FontFamily, Math.Max(10, r.Height / 8), FontStyle.Regular, GraphicsUnit.Pixel)
			r.Inflate(-4, -4)
			e.Graphics.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAliasGridFit
			e.Graphics.DrawString(vdm.Desktops(i).Name, f, Brushes.White, r, New StringFormat() With {.LineAlignment = StringAlignment.Far, .Alignment = StringAlignment.Near})
			'GlassForm.DrawGlassText(e.Graphics, VirtualDesktopManager.Desktops(i).Name, f, Color.Black, r, TextFormatFlags.Left Or TextFormatFlags.Bottom Or TextFormatFlags.SingleLine, 5)
		Next
	End Sub

End Class