Imports System.Runtime.InteropServices

''' <summary>
''' Implements a preview window that appears on hover over the main tray icon.
''' </summary>
''' <remarks></remarks>
Public Class MiniPreviewWindow
	Inherits ZPixel.LayeredForm

	Dim baseImage As Bitmap
	Const borderNum As Integer = 10
	WithEvents timFade As New Timer()
	Dim monitor As Integer
	WithEvents renderer As New System.ComponentModel.BackgroundWorker
	Dim vdm As VirtualDesktopManager
	Dim ui As MainUiPlugin

	Public Sub New(vdm As VirtualDesktopManager, ui As MainUiPlugin)
		MyBase.New(False)
		Me.vdm = vdm
		Me.ui = ui

		For Each s As Screen In Screen.AllScreens
			If Array.IndexOf(vdm.MonitorIndices, Array.IndexOf(Screen.AllScreens, s)) >= 0 Then
				If s.Bounds.Width * s.Bounds.Height > Screen.AllScreens(monitor).Bounds.Width * Screen.AllScreens(monitor).Bounds.Height Then
					monitor = Array.IndexOf(Screen.AllScreens, s)
				End If
			End If
		Next
		baseImage = New Bitmap(CInt(My.Computer.Screen.Bounds.Width / My.Settings.MiniPrevSize), CInt(My.Computer.Screen.Bounds.Height / My.Settings.MiniPrevSize), Imaging.PixelFormat.Format32bppPArgb)
		Me.ShowInTaskbar = False
		Me.TopMost = True
		SyncLock baseImage
			Using gr As Graphics = Graphics.FromImage(baseImage)
				gr.SmoothingMode = Drawing2D.SmoothingMode.HighQuality
				Dim roundPath As Drawing2D.GraphicsPath = ZPixel.GraphicsRenderer.GetRoundedRect(New Rectangle(0, 0, Me.baseImage.Width, Me.baseImage.Height), borderNum)
				gr.FillPath(New SolidBrush(Color.FromArgb(200, 127, 127, 127)), roundPath)
			End Using
			Me.Image = baseImage.Clone
		End SyncLock
		Me.Opacity = 0
	End Sub

	Private Function RenderDesktops() As Image
		SyncLock baseImage
			Dim b As Bitmap = baseImage.Clone
			Dim gr As Graphics = Graphics.FromImage(b)
			gr.SmoothingMode = Drawing2D.SmoothingMode.HighQuality

			For i As Integer = 0 To vdm.Desktops.Count - 1
				Try
					Dim rect As Rectangle = CalcPrevRect(i)
					gr.FillPath(New SolidBrush(Color.FromArgb(If(i = vdm.CurrentDesktopIndex, 200, 100), 200, 200, 200)), ZPixel.GraphicsRenderer.GetRoundedRect(rect, borderNum))
					Dim wins As List(Of WindowInfo) = vdm.Desktops(i).Windows
					SyncLock wins
						For Each w As WindowInfo In wins
							If vdm.IsWindowValid(w, False, True) Then
								Dim r As Rectangle = CalcWinPrevRect(w, rect)
								gr.FillRectangle(New SolidBrush(Color.FromArgb(200, 100, 100, 100)), r)
								Try
									Dim ico As Icon = w.Icon(WindowInfo.WindowIconSize.Small2)
									gr.DrawIcon(ico, New Rectangle(r.Location + New Point(r.Width / 2 - 8, r.Height / 2 - 8), New Size(16, 16)))
									ico.Dispose()
								Catch ex As Exception

								End Try
							End If
						Next
					End SyncLock

					Dim sf As New StringFormat
					sf.Alignment = StringAlignment.Near
					sf.LineAlignment = StringAlignment.Far
					gr.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAliasGridFit
					Dim f As New Font(SystemFonts.CaptionFont.FontFamily, Math.Max(10, rect.Height / 7), FontStyle.Regular, GraphicsUnit.Pixel)
					gr.DrawString(vdm.Desktops(i).Name, f, If(vdm.CurrentDesktopIndex = i, Brushes.Black, Brushes.White), rect, sf)
				Catch ex As Exception

				End Try
			Next

			gr.Dispose()

			Return b
		End SyncLock
	End Function

	Private Function CalcPrevRect(desktop As Integer) As Rectangle
		Dim monBounds = New Rectangle(New Point(borderNum, borderNum), baseImage.Size - New Size(borderNum, borderNum))
		Dim rect = vdm.GetDesktopPreviewBounds(desktop, monBounds)
		rect.Width -= borderNum
		rect.Height -= borderNum
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

	Public Sub ShowPreview(Optional atMouse As Boolean = False)
		If Me.InvokeRequired Then
			Me.BeginInvoke(New Action(Of Boolean)(AddressOf ShowPreview), atMouse)
		Else
			_lastShowTime = DateTime.Now
			If Not Me.Visible Then
				PositionMe(atMouse)
				If Not renderer.IsBusy Then renderer.RunWorkerAsync()
				Me.Opacity = 255
				Me.Refresh()
				Me.Show()

				Try
					timFade.Interval = 500
					timFade.Enabled = True
				Catch ex As Exception

				End Try
			End If
		End If
	End Sub

	Private Sub PositionMe(atMouse As Boolean)
		SyncLock baseImage
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
			Else
				workingArea = Screen.FromPoint(Me.Location).WorkingArea
			End If

			Me.Location = IconToolbar.CorrectPoint(Me.Location, Me.Size, workingArea)

		End SyncLock
	End Sub

	Private Sub timFade_Tick(sender As Object, e As System.EventArgs) Handles timFade.Tick
		Try
			SyncLock baseImage
				If DateTime.Now - _lastShowTime > New TimeSpan(0, 0, 0, 0, 500) Then
					If Not New Rectangle(Me.Left, Me.Top, Me.Width, Me.Height).Contains(Control.MousePosition) Then
						Me.Hide()
						timFade.Enabled = False
					End If
				End If
			End SyncLock
		Catch ex As Exception

		End Try
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

	Private Sub MiniPreviewWindow_MouseMove(sender As Object, e As System.Windows.Forms.MouseEventArgs) Handles Me.MouseMove
		'Me.ShowPreview()
	End Sub

	Private Sub renderer_DoWork(sender As Object, e As System.ComponentModel.DoWorkEventArgs) Handles renderer.DoWork
		e.Result = RenderDesktops()
	End Sub

	Private Sub renderer_RunWorkerCompleted(sender As Object, e As System.ComponentModel.RunWorkerCompletedEventArgs) Handles renderer.RunWorkerCompleted
		Dim prevImg As Bitmap = Me.Image
		Me.Image = CType(e.Result, Image)
		prevImg.Dispose()
		Me.Refresh()
	End Sub

End Class