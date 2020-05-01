Imports Microsoft.WindowsAPICodePack.Taskbar

''' <summary>
''' Implements the Windows 7 taskbar virtual desktop switcher.
''' </summary>
''' <remarks></remarks>
Public Class TaskbarSwitcher
	Inherits VirtualDesktopPlugin

	Dim _hasInited As Boolean = False
	WithEvents _tempForm As ZPixel.LayeredForm
	Dim _thumbnails As New List(Of TabbedThumbnail)

	Public Sub New(vdm As VirtualDesktopManager)
		MyBase.New(vdm)
	End Sub

	Public Overrides Sub Start()
		If TaskbarManager.IsPlatformSupported AndAlso Not _hasInited AndAlso My.Settings.TaskbarSwitching Then

			'Create a transparent window used to handle taskbar messages
			_tempForm = New ZPixel.LayeredForm(True) With {
			 .ShowInTaskbar = True,
			 .Icon = My.Resources.Main_Icon,
			 .MaximizeBox = False,
			 .MinimizeBox = False,
			 .ControlBox = False,
			 .BackColor = Color.White,
			 .Text = "Finestra Virtual Desktops"
			}

			'Cover the primary screen with the window
			Dim primaryBounds = Screen.PrimaryScreen.Bounds
			_tempForm.SetBounds(primaryBounds.X, primaryBounds.Y, primaryBounds.Width, primaryBounds.Height)
			_tempForm.Show()

			'Add taskbar thumbnails to represent virtual desktops
			For i As Integer = 0 To VirtualDesktopManager.Desktops.Count - 1
				Dim c As New Control
				_tempForm.Controls.Add(c)
				Dim thumb As New TabbedThumbnail(_tempForm.Handle, c.Handle)
				AddHandler thumb.TabbedThumbnailBitmapRequested, AddressOf TabbedThumb_BitmapRequested
				AddHandler thumb.TabbedThumbnailActivated, AddressOf TabbedThumb_Activate
				AddHandler thumb.TabbedThumbnailClosed, AddressOf TabbedThumb_Closed

				TaskbarManager.Instance.TabbedThumbnail.AddThumbnailPreview(thumb)
				thumb.SetWindowIcon(My.Resources.Main_Icon)
				thumb.Title = VirtualDesktopManager.Desktops(i).Name
				thumb.DisplayFrameAroundBitmap = False
				_thumbnails.Add(thumb)
			Next
			TaskbarManager.Instance.TabbedThumbnail.SetActiveTab(_thumbnails(0))
			TaskbarManager.Instance.SetOverlayIcon(MainUiPlugin.GetDesktopTrayIcon(0, True), VirtualDesktopManager.CurrentDesktop.Name)
			AddHandler VirtualDesktopManager.VirtualDesktopSwitched, AddressOf VirtualDesktopSwitched

			Try
				'Create a jump list
				Dim jump = JumpList.CreateJumpList
				jump.ClearAllUserTasks()
				Dim assemblyPath As String = Reflection.Assembly.GetCallingAssembly.Location
				jump.AddUserTasks(New JumpListLink(assemblyPath, "Show Switcher") With {.Arguments = "/switcher"},
				   New JumpListLink(assemblyPath, "Options") With {.Arguments = "/options"},
				   New JumpListLink("http://www.z-sys.org/donate/", "Donate"))
				jump.Refresh()
			Catch ex As Exception
				Debug.Print("An error occurred initializing the jump list: " & ex.Message)
			End Try

			_hasInited = True
		End If
	End Sub

	Public Overrides Sub [Stop]()
		If _hasInited Then
			For Each t As TabbedThumbnail In _thumbnails
				TaskbarManager.Instance.TabbedThumbnail.RemoveThumbnailPreview(t)
				RemoveHandler t.TabbedThumbnailBitmapRequested, AddressOf TabbedThumb_BitmapRequested
				RemoveHandler t.TabbedThumbnailActivated, AddressOf TabbedThumb_Activate
				t.Dispose()
			Next
			For Each c As Control In _tempForm.Controls
				c.Dispose()
			Next
			appClose = True
			_tempForm.Close()
			appClose = False
			_tempForm.Dispose()
			_tempForm = Nothing
		End If

		RemoveHandler VirtualDesktopManager.VirtualDesktopSwitched, AddressOf VirtualDesktopSwitched
		_thumbnails.Clear()
		_hasInited = False
	End Sub

	Private Sub VirtualDesktopSwitched(prevDesk As Integer, newDesk As Integer)
		If _tempForm IsNot Nothing Then
			_tempForm.Invoke(Sub()
								 TaskbarManager.Instance.TabbedThumbnail.SetActiveTab(_thumbnails(VirtualDesktopManager.CurrentDesktopIndex))
								 TaskbarManager.Instance.SetOverlayIcon(MainUiPlugin.GetDesktopTrayIcon(VirtualDesktopManager.CurrentDesktopIndex, True), VirtualDesktopManager.CurrentDesktop.Name)
							 End Sub)
		End If
	End Sub

	Private Sub TabbedThumb_BitmapRequested(sender As Object, e As TabbedThumbnailBitmapRequestedEventArgs)
		Dim thumb As TabbedThumbnail = sender
		Dim bounds As Rectangle = Screen.PrimaryScreen.Bounds
		If e.RequestedSize.Width <= 0 OrElse e.RequestedSize.Height <= 0 Then
			If e.IsIconic Then
				e.RequestedSize = New Size(bounds.Width / My.Settings.IndicWinSize, bounds.Height / My.Settings.IndicWinSize)
			Else
				e.RequestedSize = bounds.Size
			End If
		End If
		thumb.SetImage(RenderDesktop(e.RequestedSize, _thumbnails.IndexOf(thumb), e.IsIconic))
		e.Handled = True
	End Sub

	Private Sub TabbedThumb_Activate(sender As Object, e As TabbedThumbnailEventArgs)
		VirtualDesktopManager.CurrentDesktopIndex = _thumbnails.IndexOf(sender)
	End Sub

	Private Sub TabbedThumb_Closed(sender As Object, e As TabbedThumbnailClosedEventArgs)
		e.Cancel = True
		Windows.Forms.Application.Exit()
	End Sub

	''' <summary>
	''' Renders a virtual desktop preview onto a bitmap.
	''' </summary>
	''' <param name="size">The size of the bitmap to generate.</param>
	''' <param name="i">The index of the desktop whose preview image to generate.</param>
	''' <param name="real">Whether the preview should contain real thumbnails.</param>
	''' <returns></returns>
	''' <remarks></remarks>
	Private Function RenderDesktop(size As Size, i As Integer, real As Boolean) As Image
		Dim b As New Bitmap(size.Width, size.Height, Imaging.PixelFormat.Format32bppArgb)
		Using gr As Graphics = Graphics.FromImage(b)
			If i = VirtualDesktopManager.CurrentDesktopIndex Then
				gr.Clear(Color.FromArgb(128, 28, 38, 59))
			Else
				gr.Clear(Color.FromArgb(128, 0, 0, 0))
			End If

			Dim wins As List(Of WindowInfo) = VirtualDesktopManager.Desktops(i).Windows
			SyncLock wins
				For Each w As WindowInfo In wins
					If VirtualDesktopManager.IsWindowValid(w, False, True) Then
						Try
							Dim r As Rectangle = CalcWinPrevRect(w, New Rectangle(Point.Empty, size))
							r.Width -= 1
							r.Height -= 1
							Dim thumb As Bitmap = Nothing
							If i <> VirtualDesktopManager.CurrentDesktopIndex AndAlso Not w.Minimized Then
								thumb = VirtualDesktopManager.GetThumbnail(w)
							End If
							If thumb IsNot Nothing Then
								gr.DrawImage(thumb, r)
							Else
								Dim lb As New Drawing2D.LinearGradientBrush(r.Location, r.Location + New Size(0, r.Bottom), Color.FromArgb(200, 100, 100, 100), Color.FromArgb(200, 80, 80, 80))
								gr.FillRectangle(lb, r)
								gr.DrawRectangle(Pens.White, r)
								Using ico As Icon = w.Icon(WindowInfo.WindowIconSize.Small2)
									gr.DrawIcon(ico, New Rectangle(r.Location + New Point(r.Width / 2 - 8, r.Height / 2 - 8), New Size(16, 16)))
								End Using
							End If
						Catch ex As Exception

						End Try
					End If
				Next
			End SyncLock
		End Using

		Return b
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

	Dim appClose As Boolean = False

	Private Sub _tempForm_FormClosing(sender As Object, e As System.Windows.Forms.FormClosingEventArgs) Handles _tempForm.FormClosing
		If Not appClose AndAlso e.CloseReason <> CloseReason.ApplicationExitCall Then
			e.Cancel = True
			Windows.Forms.Application.Exit()
		End If
	End Sub

End Class