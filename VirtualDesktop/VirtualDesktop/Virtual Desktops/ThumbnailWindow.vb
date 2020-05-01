Imports System.Drawing.Imaging
Imports System.Runtime.InteropServices

''' <summary>
''' Displays a window for the desktop switcher.
''' </summary>
''' <remarks></remarks>
Public Class SwitcherThumbnailWindow
	Inherits ZPixel.LayeredForm	'Inherit the layered window class

	Dim thumb As Thumbnail 'The thumbnail used to display the window

	WithEvents tltTip As New ToolTip
	Dim _baseImage As Bitmap

	Dim _thumbMan As ThumbnailManager
	Dim _window As WindowInfo
	Dim _desktop As VirtualDesktop
	Dim _vdm As VirtualDesktopManager
	Dim _desktopIndex As Integer
	Dim _monitor As Integer
	Dim _expose As Boolean

	''' <summary>
	''' Gets or sets the monitor associated with this window.
	''' </summary>
	''' <value></value>
	''' <returns></returns>
	''' <remarks></remarks>
	Public Property Monitor() As Integer
		Get
			Return _monitor
		End Get
		Set(value As Integer)
			_monitor = value
		End Set
	End Property

	''' <summary>
	''' Gets or sets the window associated with the thumbnail window.
	''' </summary>
	''' <value></value>
	''' <returns></returns>
	''' <remarks></remarks>
	Public Property Window() As WindowInfo
		Get
			Return _window
		End Get
		Set(value As WindowInfo)
			_window = value
		End Set
	End Property

	''' <summary>
	''' Gets or sets the virtual desktop associated with the window represented by the thumbnail window
	''' </summary>
	''' <value></value>
	''' <returns></returns>
	''' <remarks></remarks>
	Public Property Desktop() As VirtualDesktop
		Get
			Return _desktop
		End Get
		Set(value As VirtualDesktop)
			_desktop = value
		End Set
	End Property

	''' <summary>
	''' Gets the thumbnail class used to display the thumbnail of the window.
	''' </summary>
	''' <value></value>
	''' <returns></returns>
	''' <remarks></remarks>
	Public ReadOnly Property Thumbnail() As Thumbnail
		Get
			Return thumb
		End Get
	End Property

	''' <summary>
	''' Returns the index of the desktop of the window represented from the VirtualDesktopManager.
	''' </summary>
	''' <value></value>
	''' <returns></returns>
	''' <remarks></remarks>
	Public Property DesktopIndex() As Integer
		Get
			Return _vdm.Desktops.IndexOf(Me.Desktop)
		End Get
		Set(value As Integer)
			Desktop = _vdm.Desktops(value)
		End Set
	End Property

	''' <summary>
	''' Returns whether this ThumbnailWindow is in Expose mode.
	''' </summary>
	''' <value></value>
	''' <returns></returns>
	''' <remarks></remarks>
	Public ReadOnly Property Expose() As Boolean
		Get
			Return _expose
		End Get
	End Property

	Dim _exposeRect As Rectangle

	''' <summary>
	''' Returns the location and size of this window in Expose mode.
	''' </summary>
	''' <value></value>
	''' <returns></returns>
	''' <remarks></remarks>
	Public ReadOnly Property ExposeRectangle() As Rectangle
		Get
			Return _exposeRect
		End Get
	End Property

	''' <summary>
	''' Determines whether Z-order changes are ignored.
	''' </summary>
	''' <value></value>
	''' <returns></returns>
	''' <remarks></remarks>
	Public Property IsZLocked As Boolean

	Public Sub New(thumbMan As ThumbnailManager, w As WindowInfo, d As VirtualDesktop, vdm As VirtualDesktopManager, expose As Boolean, exposeRect As Rectangle)
		Me.Image = New Bitmap(1, 1, Imaging.PixelFormat.Format32bppPArgb)  'To get rid of errors

		'Set some properties
		Me.ShowInTaskbar = False
		Me.TopMost = True
		Me.Opacity = 0
		_thumbMan = thumbMan
		Desktop = d
		_window = w
		_vdm = vdm
		Monitor = Array.IndexOf(Screen.AllScreens, Screen.FromHandle(w.Handle))
		_expose = expose
		If expose Then
			_exposeRect = exposeRect
		End If

		tltTip.ShowAlways = True
		tltTip.SetToolTip(Me, _window.Text)
	End Sub

	Protected Overrides Sub Dispose(disposing As Boolean)
		If thumb IsNot Nothing Then
			thumb.Dispose()
			thumb = Nothing
		End If
		If tltTip IsNot Nothing Then
			tltTip.Dispose()
			tltTip = Nothing
		End If
		If _baseImage IsNot Nothing Then
			_baseImage.Dispose()
			_baseImage = Nothing
		End If
		MyBase.Dispose(disposing)
	End Sub

	Private Const WM_MOUSEACTIVATE As Integer = &H21
	Private Const MA_NOACTIVATE As Integer = 3
	Private Const WM_WINDOWPOSCHANGING As Integer = &H46
	Private Const SWP_NOZORDER As Short = &H4

	<StructLayout(LayoutKind.Sequential)>
	Private Structure WINDOWPOS
		Public Hwnd As IntPtr
		Public HwndInsertAfter As IntPtr
		Public X As Short
		Public Y As Short
		Public CX As Short
		Public CY As Short
		Public Flags As Short
	End Structure

	Protected Overrides Sub WndProc(ByRef m As System.Windows.Forms.Message)
		If IsZLocked AndAlso m.Msg = WM_MOUSEACTIVATE Then
			'Don't allow clicking/dragging the thumbnail to change its Z-order
			m.Result = MA_NOACTIVATE
		Else
			MyBase.WndProc(m)
		End If
	End Sub

	Dim isDragging As Boolean
	Dim startPos As Point
	Dim dragPos As Point

	Private Sub ThumbnailWindow_Load(sender As Object, e As System.EventArgs) Handles Me.Load
		'Set up the thumbnail
		If Thumbnail.IsDWMEnabled Then
			thumb = New Thumbnail(_window.Handle, Me)
		End If
		ResetThumbnail()

		If Not Expose Then
			'Set the location based on the window position
			Dim monBounds = Screen.AllScreens(Monitor).Bounds
			Dim prevBounds = _vdm.GetDesktopPreviewBounds(DesktopIndex, monBounds)
			Me.Location = prevBounds.Location
			If Not (_window.Maximized Or _window.Minimized) Then
				Me.Location += New Point((_window.Rectangle.X - monBounds.X) / monBounds.Width * prevBounds.Width, (_window.Rectangle.Y - monBounds.Y) / monBounds.Height * prevBounds.Height)
			End If
		Else
			Me.Location = ExposeRectangle.Location + New Point(ExposeRectangle.Width / 2 - Me.Width / 2, ExposeRectangle.Height / 2 - Me.Height / 2)
		End If
	End Sub

	Public Sub ResetThumbnail()
		Dim monBounds = Screen.AllScreens(Monitor).Bounds
		Dim prevBounds = _vdm.GetDesktopPreviewBounds(DesktopIndex, monBounds)

		If Thumbnail.IsDWMEnabled AndAlso _window.Visible Then
			Dim oldBaseImage = _baseImage

			'Set up the background to an almost transparent color so that it can be dragged
			If Me.Expose Then
				Dim ratio As Double = Math.Min(Me.ExposeRectangle.Width / Math.Max(1, thumb.SourceSize.Width), Me.ExposeRectangle.Height / Math.Max(1, thumb.SourceSize.Height))
				_baseImage = New Bitmap(CInt(Math.Max(1, thumb.SourceSize.Width * ratio)), CInt(Math.Max(1, thumb.SourceSize.Height * ratio)), Imaging.PixelFormat.Format32bppPArgb)
			Else
				_baseImage = New Bitmap(Math.Max(1, CInt(thumb.SourceSize.Width / monBounds.Width * prevBounds.Width)), Math.Max(1, CInt(thumb.SourceSize.Height / monBounds.Height * prevBounds.Height)), Imaging.PixelFormat.Format32bppPArgb)
			End If
			If oldBaseImage IsNot Nothing Then oldBaseImage.Dispose()

			'Finish setting up the thumbnail
			thumb.UseEntireSource = True
			thumb.Visible = True
			thumb.DestinationRectangle = New Rectangle(0, 0, _baseImage.Width, _baseImage.Height)
			thumb.Opacity = Me.Opacity
			thumb.UpdateRendering()

			If _window.Visible AndAlso (Not _window.Minimized OrElse _vdm.GetThumbnail(_window) Is Nothing) AndAlso My.Settings.CacheWindowThumbnails Then
				Threading.ThreadPool.QueueUserWorkItem(Sub()
														   _vdm.AddThumbnail(_window)
													   End Sub)
			End If
		Else
			If Thumbnail.IsDWMEnabled Then
				thumb.Visible = False
				thumb.UpdateRendering()
			End If
			Try
				Dim b As Bitmap
				Dim thumbSize As Size

				If _window.Minimized Then
					thumbSize = If(_window.Maximized, monBounds.Size, _window.NormalRectangle.Size)

					Dim thumbBit As Bitmap = _vdm.GetThumbnail(_window)
					If thumbBit IsNot Nothing AndAlso thumbBit.Width > thumbSize.Width AndAlso thumbBit.Height > thumbSize.Height Then thumbSize = thumbBit.Size
				Else
					thumbSize = _window.Size
				End If

				If Expose Then
					Dim ratio As Double = Math.Min(Me.ExposeRectangle.Width / Math.Max(1, thumbSize.Width), Me.ExposeRectangle.Height / Math.Max(1, thumbSize.Height))
					b = New Bitmap(CInt(Math.Max(1, thumbSize.Width * ratio)), CInt(Math.Max(1, thumbSize.Height * ratio)), Imaging.PixelFormat.Format32bppPArgb)
				Else
					b = New Bitmap(Math.Max(1, CInt(thumbSize.Width / monBounds.Width * prevBounds.Width)), Math.Max(1, CInt(thumbSize.Height / monBounds.Height * prevBounds.Height)), Imaging.PixelFormat.Format32bppPArgb)
				End If

				Using gr As Graphics = Graphics.FromImage(b)
					gr.PixelOffsetMode = Drawing2D.PixelOffsetMode.HighQuality
					gr.SmoothingMode = Drawing2D.SmoothingMode.HighQuality
					gr.InterpolationMode = Drawing2D.InterpolationMode.HighQualityBicubic
					If _window.Visible AndAlso My.Settings.CacheWindowThumbnails Then
						_vdm.AddThumbnail(_window)
						Dim thumb = _vdm.GetThumbnail(_window)
						If thumb IsNot Nothing Then
							gr.DrawImage(thumb, New Rectangle(0, 0, b.Width, b.Height))
						End If
					Else
						Dim b2 As Bitmap = _vdm.GetThumbnail(_window)
						If b2 IsNot Nothing Then
							gr.DrawImage(b2, New Rectangle(0, 0, b.Width, b.Height))
						Else
							Using ico = _window.Icon(WindowInfo.WindowIconSize.Big)
								gr.PixelOffsetMode = Drawing2D.PixelOffsetMode.Half
								gr.SmoothingMode = Drawing2D.SmoothingMode.HighQuality

								gr.FillPath(New SolidBrush(Color.FromArgb(100, 255, 255, 255)), ZPixel.GraphicsRenderer.GetRoundedRect(New RectangleF(0, 0, b.Width, b.Height), 4))

								Dim icoRect As New Rectangle(b.Width \ 2 - ico.Width \ 2, b.Height \ 2 - ico.Height \ 2, ico.Width, ico.Height)
								icoRect.Inflate(4, 4)
								gr.FillPath(New SolidBrush(Color.FromArgb(128, 128, 128, 128)), ZPixel.GraphicsRenderer.GetRoundedRect(icoRect, 4))
								icoRect.Inflate(-4, -4)
								gr.PixelOffsetMode = Drawing2D.PixelOffsetMode.Default
								gr.DrawIcon(ico, icoRect)
							End Using
						End If
					End If
				End Using

				Dim oldBaseImage = _baseImage
				_baseImage = b
				If oldBaseImage IsNot Nothing Then
					oldBaseImage.Dispose()
				End If
			Catch ex As Exception
				Me.Image = New Bitmap(Me.Image.Width, Me.Image.Height, Imaging.PixelFormat.Format32bppPArgb)
			End Try
		End If
		ChangeViewState(If(isDragging, ThumbnailViewState.Pressed, ThumbnailViewState.Normal))
	End Sub

	Private Sub SelectThumbnail()
		_vdm.CurrentDesktop = Desktop
		_thumbMan.HideSwitcher()
		_window.BringToFront()
		If _window.Minimized Then
			_window.State = WindowInfo.WindowState.Restore
		End If
		isDragging = False
	End Sub

	Private Sub ThumbnailWindow_MouseClick(sender As Object, e As MouseEventArgs) Handles Me.MouseClick
		If e.Button = Windows.Forms.MouseButtons.Left Then
			If Expose OrElse Control.MousePosition = startPos Then
				SelectThumbnail()
			End If
		End If
	End Sub

	Private Sub ThumbnailWindow_MouseDoubleClick(sender As Object, e As System.Windows.Forms.MouseEventArgs) Handles Me.MouseDoubleClick
		SelectThumbnail()
	End Sub

	Private Sub ThumbnailWindow_MouseDown(sender As Object, e As System.Windows.Forms.MouseEventArgs) Handles Me.MouseDown
		If _window.IsValid Then
			If Not Expose Then
				'Start the drag by setting some useful properties
				startPos = Control.MousePosition
				dragPos = Control.MousePosition - Me.Location
				Try
					_window.Refresh() 'Try refreshing the window (this doesn't seem to work usually)
				Catch ex As Exception

				End Try
				isDragging = True
			End If
		Else
			_thumbMan.thumbWins.Remove(Me)
			Me.Dispose()
		End If
	End Sub

	Private Enum ThumbnailViewState
		Normal
		Hover
		Pressed
	End Enum

	Private Sub ChangeViewState(state As ThumbnailViewState)
		Dim hoverImage As Bitmap
		If _baseImage IsNot Nothing Then
			hoverImage = New Bitmap(_baseImage.Width, _baseImage.Height, PixelFormat.Format32bppPArgb)
		Else
			ResetThumbnail()
			Return
		End If

		Using gr = Graphics.FromImage(hoverImage)
			gr.Clear(Color.FromArgb(1, 0, 0, 0))
			If state <> ThumbnailViewState.Normal Then
				gr.PixelOffsetMode = Drawing2D.PixelOffsetMode.Half
				gr.SmoothingMode = Drawing2D.SmoothingMode.HighQuality
				gr.FillPath(New SolidBrush(If(state = ThumbnailViewState.Pressed, Color.FromArgb(200, 0, 0, 0), Color.FromArgb(200, 255, 255, 255))),
				   ZPixel.GraphicsRenderer.GetRoundedRect(New RectangleF(0, 0, hoverImage.Width, hoverImage.Height), 4))
			End If
			If _baseImage IsNot Nothing Then
				gr.DrawImage(_baseImage, New Rectangle(0, 0, hoverImage.Width, hoverImage.Height))
			End If
		End Using
		Dim oldImage = Me.Image
		Me.Image = hoverImage
		oldImage.Dispose()

		If thumb IsNot Nothing Then
			thumb.Opacity = If(state = ThumbnailViewState.Normal, 255, 200)
			thumb.UpdateRendering()
		End If
	End Sub

	Private Sub SwitcherThumbnailWindow_MouseEnter(sender As Object, e As System.EventArgs) Handles Me.MouseEnter
		ChangeViewState(ThumbnailViewState.Hover)
	End Sub

	Private Sub SwitcherThumbnailWindow_MouseLeave(sender As Object, e As System.EventArgs) Handles Me.MouseLeave
		ChangeViewState(ThumbnailViewState.Normal)
		tltTip.Hide(Me)
	End Sub

	Private Sub SwitcherThumbnailWindow_MouseDown(sender As Object, e As MouseEventArgs) Handles Me.MouseDown
		ChangeViewState(ThumbnailViewState.Pressed)
	End Sub

	Private Sub SwitcherThumbnailWindow_MouseUp(sender As Object, e As MouseEventArgs) Handles Me.MouseUp
		ChangeViewState(ThumbnailViewState.Hover)
	End Sub

	Private Sub ThumbnailWindow_MouseMove(sender As Object, e As System.Windows.Forms.MouseEventArgs) Handles Me.MouseMove
		If _window.IsValid Then
			If Not Expose AndAlso e.Button = Windows.Forms.MouseButtons.Left And isDragging Then
				Try
					Dim newMonitor As Integer = Array.IndexOf(Screen.AllScreens, Screen.FromPoint(Control.MousePosition))
					Dim setMonitor As Boolean = False
					For Each b As DoubleBufferedForm In _thumbMan.backgroundWins
						Dim s1 As String = Screen.FromControl(b).DeviceName
						If s1.IndexOf(vbNullChar) >= 0 Then s1 = s1.Substring(0, s1.IndexOf(vbNullChar))
						Dim s2 As String = Screen.AllScreens(newMonitor).DeviceName
						If s2.IndexOf(vbNullChar) >= 0 Then s2 = s2.Substring(0, s2.IndexOf(vbNullChar))
						If s1 = s2 Then
							Me.Owner = b
							setMonitor = True
							Exit For
						End If
					Next
					If Not setMonitor Then
						Exit Sub
					Else
						If Monitor <> newMonitor AndAlso _window.Maximized Then
							Try
								If _window.Minimized Then
									'We can't use the working area here because the location is in working area coordinates
									_window.MaximizedLocation = Screen.AllScreens(newMonitor).Bounds.Location
								Else
									_window.Rectangle = Screen.AllScreens(newMonitor).WorkingArea
								End If
							Catch ex As Exception

							End Try
							ResetThumbnail()
						End If
						Monitor = newMonitor
					End If

					Me.Location = Control.MousePosition - dragPos 'Display the thumbnail attached to the cursor during a drag

					Dim newDesk As Integer = _thumbMan.DesktopFromPoint(Control.MousePosition - Screen.AllScreens(Monitor).Bounds.Location, Monitor)
					'Was the window dragged to a different desktop?
					If newDesk <> -1 AndAlso newDesk <> DesktopIndex Then
						_vdm.SendWindowToDesktop(_window, newDesk, DesktopIndex)
						DesktopIndex = newDesk
						ResetThumbnail()
					End If

					'If the window cannot be positioned
					Dim monBounds = Screen.AllScreens(Monitor).Bounds
					Dim prevBounds = _vdm.GetDesktopPreviewBounds(DesktopIndex, monBounds)
					If _window.Maximized Or _window.Minimized Or newDesk = -1 Then
						_window.NormalRectangle = New Rectangle(MultiplyPoints(Me.Location - prevBounds.Location, New Point(monBounds.Width / prevBounds.Width, monBounds.Height / prevBounds.Height)) + monBounds.Location, _window.NormalRectangle.Size)
					Else 'Otherwise, move it to the position it was dragged to
						_window.Location = MultiplyPoints(Me.Location - prevBounds.Location, New Point(monBounds.Width / prevBounds.Width, monBounds.Height / prevBounds.Height)) + monBounds.Location
					End If
				Catch ex As Exception
					isDragging = False
				End Try
			End If
		Else
			_thumbMan.thumbWins.Remove(Me)
			Me.Dispose()
		End If
	End Sub

	Private Sub ThumbnailWindow_MouseUp(sender As Object, e As System.Windows.Forms.MouseEventArgs) Handles Me.MouseUp
		If Not Expose Then
			Try
				'If the window cannot be positioned
				Dim monBounds = Screen.AllScreens(Monitor).Bounds
				Dim prevBounds = _vdm.GetDesktopPreviewBounds(DesktopIndex, monBounds)

				If _window.Maximized Or _window.Minimized Then
					Me.Location = prevBounds.Location	'Display it at the selected desktop's origin
				Else 'Otherwise, move it to the position it was dragged to
					_window.Location = MultiplyPoints(Me.Location - prevBounds.Location, New Point(monBounds.Width / prevBounds.Width, monBounds.Height / prevBounds.Height)) + monBounds.Location
				End If
			Catch ex As Exception

			End Try
		End If
		isDragging = False
	End Sub

	''' <summary>
	''' Multiplies x1 by x2 and y1 by y2 and puts them into a new point.
	''' </summary>
	''' <param name="p1"></param>
	''' <param name="p2"></param>
	''' <returns></returns>
	''' <remarks></remarks>
	Private Function MultiplyPoints(p1 As Point, p2 As Point) As Point
		Return New Point(p1.X * p2.X, p1.Y * p2.Y)
	End Function

	Private Sub ThumbnailWindow_Shown(sender As Object, e As System.EventArgs) Handles Me.Shown
		Try
			Me.Refresh()
			_window.Refresh() 'Try refreshing the window (usually this doesn't seem to work)
		Catch ex As Exception

		End Try
		If thumb IsNot Nothing Then thumb.UpdateRendering() 'Display the thumbnail image
	End Sub

	''' <summary>
	''' Controls the opacity of the entire thumbnail, including the DWM thumbnail
	''' </summary>
	''' <value></value>
	''' <returns></returns>
	''' <remarks></remarks>
	Public Overrides Property Opacity As Byte
		Get
			Return MyBase.Opacity
		End Get
		Set(value As Byte)
			MyBase.Opacity = value
			If Me.Thumbnail IsNot Nothing Then
				Me.Thumbnail.Opacity = value
				Me.Thumbnail.UpdateRendering()
			End If
		End Set
	End Property

End Class