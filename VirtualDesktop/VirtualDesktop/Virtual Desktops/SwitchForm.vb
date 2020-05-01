Imports System.Drawing, System.Drawing.Drawing2D

''' <summary>
''' Provides an "indicator window" whenever the user switches desktops.
''' The window simply shows a small desktop preview with an arrow indicating
''' what desktop is being switched to and from what desktop you are moving.
''' </summary>
''' <remarks></remarks>
Public Class SwitchForm
	Inherits ZPixel.LayeredForm

	Dim baseImage As Bitmap
	Const borderNum As Integer = 20
	WithEvents timFade As New Timer()
	Dim monitor As Integer
	Dim vdm As VirtualDesktopManager

	Public Sub New(vdm As VirtualDesktopManager)
		MyBase.New(True)
		Me.vdm = vdm

		monitor = Array.IndexOf(Screen.AllScreens, Screen.PrimaryScreen)

		Me.ShowInTaskbar = False
		Me.TopMost = True
		Me.Opacity = CByte(0)

		'Generate the base image
		baseImage = New Bitmap(CInt(My.Computer.Screen.Bounds.Width / My.Settings.IndicWinSize) + 3 * borderNum, CInt(My.Computer.Screen.Bounds.Height / My.Settings.IndicWinSize) + 3 * borderNum, Imaging.PixelFormat.Format32bppPArgb)
		Me.Image = baseImage

		'Choose whether to start centered or in the upper left corner
		If My.Settings.CenterIndicWindow Then
			Me.StartPosition = FormStartPosition.CenterScreen
		Else
			Me.StartPosition = FormStartPosition.Manual
			Me.Location = New Point(0, 0)
		End If
	End Sub

	Private Sub DrawAntiAliasedClip(gr As Graphics, img As Image, clipPath As GraphicsPath, dest As Rectangle, src As Rectangle)
		Dim origClip = gr.Clip
		gr.SetClip(dest, CombineMode.Intersect)
		gr.FillPath(New TextureBrush(img, New Rectangle(dest.X - src.X, dest.Y - src.Y, src.Width, src.Height)), clipPath)
		gr.Clip = origClip
	End Sub

	''' <summary>
	''' Sets the form's current image to the base image with an arrow and desktop names superimposed.
	''' </summary>
	''' <param name="prevDesk">The desktop the user is switching from (in order to draw the arrow from it).</param>
	''' <remarks></remarks>
	Private Sub RenderDesktops(prevDesk As Integer, newDesk As Integer)
		Dim isGlassOn As Boolean = GlassForm.IsCompositionEnabled

		Dim b As New Bitmap(baseImage.Width, baseImage.Height, Imaging.PixelFormat.Format32bppPArgb)
		Dim hBlurRegion As IntPtr
		Using gr As Graphics = Graphics.FromImage(b)
			gr.SmoothingMode = Drawing2D.SmoothingMode.HighQuality
			gr.PixelOffsetMode = PixelOffsetMode.Half
			gr.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAliasGridFit
			Dim blurPath As New GraphicsPath
			Dim captionFont As FontFamily = SystemFonts.CaptionFont.FontFamily
			Dim prevRect As New Rectangle(borderNum, borderNum, baseImage.Width - 3 * borderNum, baseImage.Height - 3 * borderNum)

			'Draw the glassy background
			Dim roundRect As New Rectangle(prevRect.X, prevRect.Y, prevRect.Width + borderNum, prevRect.Height + borderNum)
			Dim roundPath As Drawing2D.GraphicsPath = ZPixel.GraphicsRenderer.GetRoundedRect(roundRect, borderNum)
			blurPath = roundPath.Clone
			Try
				'Draw the shadow parts
				Dim clipRect As Rectangle = roundRect
				gr.SetClip(CType(ZPixel.GraphicsRenderer.GetRoundedRect(clipRect, borderNum + 1).Clone, GraphicsPath), CombineMode.Exclude)
				Dim shadowMargins As New Padding(16, 16, 16, 16)
				Dim shadowImage As Bitmap = My.Resources.GaussianShadow

				Dim backPath = ZPixel.GraphicsRenderer.GetRoundedRect(clipRect, borderNum + 1)

				gr.DrawImage(shadowImage, New Rectangle(roundRect.X - shadowMargins.Left, roundRect.Y - shadowMargins.Top, shadowMargins.Left + borderNum, shadowMargins.Top + borderNum), New Rectangle(0, 0, shadowMargins.Left + borderNum, shadowMargins.Top + borderNum), GraphicsUnit.Pixel)
				gr.DrawImage(shadowImage, New Rectangle(roundRect.Right - borderNum, roundRect.Y - shadowMargins.Top, borderNum + shadowMargins.Right, shadowMargins.Top + borderNum), New Rectangle(shadowImage.Width - borderNum - shadowMargins.Right, 0, borderNum + shadowMargins.Right, borderNum + shadowMargins.Top), GraphicsUnit.Pixel)
				gr.DrawImage(shadowImage, New Rectangle(roundRect.X - shadowMargins.Left, roundRect.Bottom - borderNum, shadowMargins.Left + borderNum, shadowMargins.Bottom + borderNum), New Rectangle(0, shadowImage.Height - shadowMargins.Bottom - borderNum, borderNum + shadowMargins.Left, borderNum + shadowMargins.Bottom), GraphicsUnit.Pixel)
				gr.DrawImage(shadowImage, New Rectangle(roundRect.Right - borderNum, roundRect.Bottom - borderNum, borderNum + shadowMargins.Right, borderNum + shadowMargins.Bottom), New Rectangle(shadowImage.Width - borderNum - shadowMargins.Right, shadowImage.Height - borderNum - shadowMargins.Bottom, shadowMargins.Right + borderNum, shadowMargins.Bottom + borderNum), GraphicsUnit.Pixel)

				gr.DrawImage(shadowImage, New Rectangle(roundRect.X + borderNum, roundRect.Y - shadowMargins.Top, roundRect.Width - 2 * borderNum, shadowMargins.Top), New Rectangle(shadowMargins.Left + borderNum, 0, shadowImage.Width - shadowMargins.Horizontal - borderNum * 2, shadowMargins.Top), GraphicsUnit.Pixel)
				gr.DrawImage(shadowImage, New Rectangle(roundRect.X - shadowMargins.Left, roundRect.Y + borderNum, shadowMargins.Left, roundRect.Height - 2 * borderNum), New Rectangle(0, shadowMargins.Top + borderNum, shadowMargins.Left, shadowImage.Height - shadowMargins.Vertical - borderNum * 2), GraphicsUnit.Pixel)
				gr.DrawImage(shadowImage, New Rectangle(roundRect.X + borderNum, roundRect.Bottom, roundRect.Width - 2 * borderNum, shadowMargins.Bottom), New Rectangle(shadowMargins.Left + borderNum, shadowImage.Height - shadowMargins.Bottom, shadowImage.Width - shadowMargins.Horizontal - borderNum * 2, shadowMargins.Bottom), GraphicsUnit.Pixel)
				gr.DrawImage(shadowImage, New Rectangle(roundRect.Right, roundRect.Y + borderNum, shadowMargins.Right, roundRect.Height - 2 * borderNum), New Rectangle(shadowImage.Width - shadowMargins.Right, shadowMargins.Top + borderNum, shadowMargins.Right, shadowImage.Height - shadowMargins.Vertical - 2 * borderNum), GraphicsUnit.Pixel)

				gr.ResetClip()

				For i As Integer = 0 To vdm.Desktops.Count - 1
					Dim rect As Rectangle = CalcPrevRect(i, prevRect)
					Dim deskPath As GraphicsPath = ZPixel.GraphicsRenderer.GetRoundedRect(rect, borderNum)
					gr.FillPath(New SolidBrush(Color.FromArgb(If(i = newDesk, 150, 50) + If(isGlassOn, 0, 100), 28, 38, 59)), deskPath)
					roundPath.AddPath(deskPath, False)
					Dim sf As New StringFormat
					sf.Alignment = StringAlignment.Near
					sf.LineAlignment = StringAlignment.Far

					rect.Inflate(-borderNum / 2, -borderNum / 2)
					Dim f As New Font(captionFont, Math.Max(10, rect.Height / 7), FontStyle.Regular, GraphicsUnit.Pixel)
					gr.DrawString(vdm.Desktops(i).Name, f, New SolidBrush(Color.FromArgb(100, 28, 38, 59)), New Rectangle(rect.Location + New Size(1, 1), rect.Size), sf)
					gr.DrawString(vdm.Desktops(i).Name, f, Brushes.White, rect, sf)
				Next

				Dim roundBackBrush As New LinearGradientBrush(roundRect.Location, New Point(roundRect.X, roundRect.Y + roundRect.Height / 2), Color.FromArgb(200, 149, 160, 182), Color.FromArgb(200, 225, 220, 240))
				gr.SetClip(New Rectangle(roundRect.X, roundRect.Y, roundRect.Width, roundRect.Height / 2))
				gr.FillPath(roundBackBrush, roundPath)
				gr.SetClip(New Rectangle(roundRect.X, roundRect.Y + roundRect.Height / 2, roundRect.Width, roundRect.Height / 2))
				gr.FillPath(New SolidBrush(roundBackBrush.LinearColors(1)), roundPath)
				gr.ResetClip()

				gr.PixelOffsetMode = PixelOffsetMode.Default

				Dim highlightPath As GraphicsPath = ZPixel.GraphicsRenderer.GetRoundedRect(New Rectangle(roundRect.X, roundRect.Y, roundRect.Width - 1, roundRect.Height - 1), borderNum)
				gr.SetClip(New Rectangle(roundRect.X, roundRect.Y, roundRect.Width, borderNum))
				gr.DrawPath(New Pen(New LinearGradientBrush(roundRect.Location, roundRect.Location + New Size(0, borderNum), Color.FromArgb(100, 255, 255, 255), Color.Transparent)), highlightPath)
				gr.ResetClip()

			Catch ex As Exception
				Stop
			End Try

			Dim currentDesk As Integer = newDesk
			Dim prevDeskRect = CalcPrevRect(prevDesk, prevRect)
			Dim currDeskRect = CalcPrevRect(currentDesk, prevRect)
			Dim center1 As New Point(prevDeskRect.X + prevDeskRect.Width / 2, prevDeskRect.Y + prevDeskRect.Height / 2)
			Dim center2 As New Point(currDeskRect.X + currDeskRect.Width / 2, currDeskRect.Y + currDeskRect.Height / 2)
			Dim arrowPen As New Pen(Color.White, Math.Min(prevRect.Width, prevRect.Height) / 7)
			arrowPen.EndCap = Drawing2D.LineCap.ArrowAnchor
			gr.DrawLine(arrowPen, center1, center2)
			hBlurRegion = New Region(blurPath).GetHrgn(gr)
		End Using

		Me.Image = b

		If isGlassOn AndAlso hBlurRegion <> IntPtr.Zero Then
			GlassForm.NativeMethods.DwmEnableBlurBehindWindow(Me.Handle, New GlassForm.NativeMethods.DWM_BLURBEHIND With {.Enable = True, .RgnBlur = hBlurRegion, .Flags = GlassForm.NativeMethods.DWM_BB_BLURREGION Or GlassForm.NativeMethods.DWM_BB_ENABLE})
		End If

		Me.Refresh()
	End Sub

	''' <summary>
	''' Calculates the size of a virtual desktop's rectangle on the switcher image.
	''' </summary>
	''' <param name="desktop"></param>
	''' <returns></returns>
	''' <remarks></remarks>
	Private Function CalcPrevRect(desktop As Integer, prevRect As Rectangle) As Rectangle
		Dim monBounds As New Rectangle(New Point(borderNum + prevRect.X, borderNum + prevRect.Y), prevRect.Size)
		Dim rect = vdm.GetDesktopPreviewBounds(desktop, monBounds)
		rect.Width -= borderNum
		rect.Height -= borderNum
		Return rect
	End Function

	''' <summary>
	''' Called by VirtualDesktopManager to display the indicator window and automatically fade itself out afterward.
	''' </summary>
	''' <param name="prevDesk"></param>
	''' <remarks></remarks>
	Public Sub SwitchDesktop(prevDesk As Integer, newDesk As Integer)
		If Me.Opacity > CByte(0) Then
			timFade.Enabled = False
			Me.StopFade()
		End If
		RenderDesktops(prevDesk, newDesk)
		Me.Opacity = CByte(255)
		Me.Visible = True
		If Not My.Settings.CenterIndicWindow Then
			Me.Location = New Point(0, 0)
		Else
			Dim primScr = Screen.PrimaryScreen.Bounds
			Me.Location = New Point(primScr.X + primScr.Width / 2 - Me.Width / 2, primScr.Y + primScr.Height / 2 - Me.Height / 2)
		End If
		Dim w As New WindowInfo(Me.Handle)
		w.SetPosition(Rectangle.Empty, -1, WindowInfo.SetPositionFlags.ShowWindow Or WindowInfo.SetPositionFlags.NoMove Or WindowInfo.SetPositionFlags.NoSize Or WindowInfo.SetPositionFlags.NoActivate)
		w.SetPosition(Rectangle.Empty, 0, WindowInfo.SetPositionFlags.NoMove Or WindowInfo.SetPositionFlags.NoSize Or WindowInfo.SetPositionFlags.NoActivate)
		Try
			timFade.Interval = 1000
			timFade.Enabled = True
		Catch ex As Exception

		End Try
	End Sub

	Private Sub timFade_Tick(sender As Object, e As System.EventArgs) Handles timFade.Tick
		Try
			If Me.Visible Then
				Me.StartFade(300)
			End If
			timFade.Enabled = False
		Catch ex As Exception

		End Try
	End Sub

End Class

''' <summary>
''' Implements the switch form plug-in.
''' </summary>
''' <remarks></remarks>
Public Class SwitchFormPlugin
	Inherits VirtualDesktopPlugin

	Dim appContext As ApplicationContext
	Dim switchForm As SwitchForm

	Public Sub New(vdm As VirtualDesktopManager)
		MyBase.New(vdm)
	End Sub

	Public Overrides Sub Start()
		Dim t As New Threading.Thread(AddressOf SwitchFormThread)
		t.Start()
	End Sub

	Public Overrides Sub [Stop]()
		If appContext IsNot Nothing Then
			appContext.ExitThread()
		End If
	End Sub

	Private Sub SwitchFormThread()
		Threading.Thread.CurrentThread.Name = "Finestra: Switch Form Thread"
		switchForm = New SwitchForm(VirtualDesktopManager)
		appContext = New ApplicationContext(switchForm)
		AddHandler VirtualDesktopManager.VirtualDesktopSwitching, AddressOf VDM_SwitchDesktop
		Windows.Forms.Application.Run(appContext)
		RemoveHandler VirtualDesktopManager.VirtualDesktopSwitching, AddressOf VDM_SwitchDesktop
	End Sub

	Private Sub VDM_SwitchDesktop(prevDesk As Integer, newDesk As Integer)
		If My.Settings.ShowIndicator Then
			switchForm.BeginInvoke(Sub()
									   switchForm.SwitchDesktop(prevDesk, newDesk)
								   End Sub)
		End If
	End Sub

End Class