Imports SlimDX.RawInput
Imports ZPixel

''' <summary>
''' Implements the ability to change desktops by simply moving the mouse to the edge of the screen.
''' </summary>
''' <remarks></remarks>
Public Class MouseEdgeSwitcher
	Inherits VirtualDesktopPlugin

	Dim timingForm As LayeredForm
	WithEvents timingTimer As Timer

	Dim mouseEdgeDelay As Double = 0.5

	Public Sub New(vdm As VirtualDesktopManager)
		MyBase.New(vdm)
	End Sub

	WithEvents mouseTimer As New Timer
	Dim appContext As ApplicationContext

	Public Overrides Sub Start()
		If My.Settings.EdgeSwitchEnable Then
			Dim t As New Threading.Thread(AddressOf MouseEdgeWatcherProc)
			t.Start()
			mouseEdgeDelay = My.Settings.EdgeSwitchDelay
		End If
	End Sub

	Public Overrides Sub [Stop]()
		If appContext IsNot Nothing Then
			appContext.ExitThread()
		End If
	End Sub

	Private Function GenerateTimerImage(fraction As Double) As Bitmap
		Dim b As New Bitmap(32, 32, Imaging.PixelFormat.Format32bppPArgb)
		Using gr = Graphics.FromImage(b)
			gr.SmoothingMode = Drawing2D.SmoothingMode.HighQuality
			Dim r As New Rectangle(4, 4, 32 - 8, 32 - 8)
			gr.FillPie(New SolidBrush(Color.Black), r, 0, fraction * 360)
			Using p As New Pen(Color.White, 3)
				p.LineJoin = Drawing2D.LineJoin.Round
				gr.DrawPie(p, r, 0, CSng(fraction * 360))
			End Using
		End Using
		Return b
	End Function

	''' <summary>
	''' The thread procedure for a thread that watches the mouse and switches the desktop when the mouse is on the edge of the screen for a period.
	''' </summary>
	''' <remarks></remarks>
	Private Sub MouseEdgeWatcherProc()
		Threading.Thread.CurrentThread.Name = "Finestra: Mouse Watcher"

		Try
			appContext = New ApplicationContext()

			'Create a window for listening to mouse messages
			Dim w As New NativeWindow()
			w.CreateHandle(New CreateParams)

			'Create the timing pie window
			timingForm = New LayeredForm(True)
			timingForm.ShowInTaskbar = False
			timingForm.ControlBox = False
			timingForm.TopMost = True
			timingForm.Image = GenerateTimerImage(0.5)

			timingTimer = New Timer
			timingTimer.Interval = 10

			AddHandler Device.MouseInput, AddressOf Device_MouseInput
			Device.RegisterDevice(SlimDX.Multimedia.UsagePage.Generic, SlimDX.Multimedia.UsageId.Mouse, DeviceFlags.InputSink, w.Handle)

			mouseTimer.Interval = 500
			mouseTimer.Enabled = True
			Windows.Forms.Application.Run(appContext)
			mouseTimer.Enabled = False

			w.DestroyHandle()
			RemoveHandler Device.MouseInput, AddressOf Device_MouseInput

		Catch ex As Exception
			Debug.Print("Error on mouse watching thread. " & ex.ToString)
		End Try
	End Sub

	Dim _lastMovement As DateTime
	Dim _lastNewDesk As Integer = -1
	Dim _firstNewDeskMovement As DateTime
	Dim _nextCursPos As Point

	Private Sub Device_MouseInput(sender As Object, e As MouseInputEventArgs)
		Dim currTime = DateTime.Now

		'Figure out which desktop the mouse is switching to
		Dim pos As Point = Control.MousePosition
		Dim newCursPos As Point
		Dim s As Screen = Screen.FromPoint(pos)
		Dim nextDesk As Integer = -1
		If pos.X = s.Bounds.Left Then
			nextDesk = VirtualDesktopManager.GetDesktopDirectional(Direction.Left, VirtualDesktopManager.CurrentDesktopIndex)
			newCursPos = New Point(s.Bounds.Right - 3, pos.Y)
			timingForm.Location = pos + New Point(32, -16)
			Debug.Print("Moving left for mouse position " & pos.ToString)
		ElseIf pos.X = s.Bounds.Right - 1 Then
			nextDesk = VirtualDesktopManager.GetDesktopDirectional(Direction.Right, VirtualDesktopManager.CurrentDesktopIndex)
			newCursPos = New Point(s.Bounds.Left + 3, pos.Y)
			timingForm.Location = pos + New Point(-64, -16)
			Debug.Print("Moving right for mouse position " & pos.ToString)
		ElseIf pos.Y = s.Bounds.Top Then
			nextDesk = VirtualDesktopManager.GetDesktopDirectional(Direction.Up, VirtualDesktopManager.CurrentDesktopIndex)
			newCursPos = New Point(pos.X, s.Bounds.Bottom - 3)
			timingForm.Location = pos + New Point(-16, 32)
			Debug.Print("Moving up for mouse position " & pos.ToString)
		ElseIf pos.Y = s.Bounds.Bottom - 1 Then
			nextDesk = VirtualDesktopManager.GetDesktopDirectional(Direction.Down, VirtualDesktopManager.CurrentDesktopIndex)
			newCursPos = New Point(pos.X, s.Bounds.Top + 3)
			timingForm.Location = pos + New Point(-16, -64)
			Debug.Print("Moving down for mouse position " & pos.ToString)
		End If

		_lastMovement = DateTime.Now

		If nextDesk >= 0 Then
			_nextCursPos = newCursPos
			If _lastNewDesk <> nextDesk Then
				_firstNewDeskMovement = _lastMovement
				_lastNewDesk = nextDesk
				timingForm.Image = GenerateTimerImage(0)
			End If

			Dim win As New WindowInfo(timingForm.Handle)
			win.SetPosition(Rectangle.Empty, IntPtr.Zero, WindowInfo.SetPositionFlags.NoActivate Or WindowInfo.SetPositionFlags.ShowWindow Or WindowInfo.SetPositionFlags.NoMove Or WindowInfo.SetPositionFlags.NoSize)
			timingTimer.Enabled = True
		Else
			_lastNewDesk = -1
			timingForm.Visible = False
			timingTimer.Enabled = False
		End If

		_lastMovement = DateTime.Now
	End Sub

	Private Sub timingTimer_Tick(sender As Object, e As System.EventArgs) Handles timingTimer.Tick
		If _lastNewDesk >= 0 Then
			Dim currTime = DateTime.Now
			Dim timeDiff = (currTime - _firstNewDeskMovement).TotalSeconds
			timingForm.Image = GenerateTimerImage(timeDiff / mouseEdgeDelay)

			'Perform the switch after the delay
			If timeDiff > mouseEdgeDelay Then
				timingTimer.Enabled = False
				timingForm.Visible = False

				If My.Settings.EdgeSwitchMouseWrap Then
					Cursor.Position = _nextCursPos
				End If
				VirtualDesktopManager.CurrentDesktopIndex = _lastNewDesk
			End If
		End If
	End Sub

End Class
