Imports System.ComponentModel

''' <summary>
''' A plugin that animates windows into and out of screen space when switching desktops. It's a pretty nice effect, overall.
''' </summary>
''' <remarks></remarks>
Public Class WindowAnimationPlugin
	Inherits VirtualDesktopPlugin

	Dim hasInit As Boolean = False

	'The original positions for windows that were hiding
	Dim oldWindowOldPositions As New Dictionary(Of WindowInfo, Rectangle)

	'The 'from' and 'to' positions for windows that are appearing
	Dim newWindowOldPositions As New Dictionary(Of WindowInfo, Rectangle)
	Dim newWindowNewPositions As New Dictionary(Of WindowInfo, Rectangle)

	'The animation time in seconds
	Dim animTime As Double = 0.2
	'Whether windows animate out of view
	Dim animOut As Boolean = True
	'Whether windows animate into view
	Dim animIn As Boolean = True

	'We must tell DWM to disable window transitions for windows that we want to animate. If not, the windows will 
	'tend to very briefly reappear on the screen after they animated off to the side. This flag keeps track of whether 
	'or not DWM is available. It only checks when the application starts, but this should be sufficient.
	Dim dwmEnabled As Boolean = GlassForm.IsCompositionEnabled

	Public Sub New(vdm As VirtualDesktopManager)
		MyBase.New(vdm)
	End Sub

	Public Overrides Sub Start()
		If My.Settings.WinAnimEnable AndAlso Not hasInit Then
			AddHandler Me.VirtualDesktopManager.VirtualDesktopSwitching, AddressOf VirtualDesktopSwitching
			AddHandler Me.VirtualDesktopManager.VirtualDesktopSwitched, AddressOf VirtualDesktopSwitched

			hasInit = True
		End If
	End Sub

	Public Overrides Sub [Stop]()
		If hasInit Then
			RemoveHandler Me.VirtualDesktopManager.VirtualDesktopSwitching, AddressOf VirtualDesktopSwitching
			RemoveHandler Me.VirtualDesktopManager.VirtualDesktopSwitched, AddressOf VirtualDesktopSwitched
			hasInit = False
		End If
	End Sub

	Private ReadOnly Property AnimationFlags As WindowInfo.SetPositionFlags
		Get
			Dim ret = WindowInfo.SetPositionFlags.NoActivate Or WindowInfo.SetPositionFlags.NoSize Or WindowInfo.SetPositionFlags.NoZOrder Or WindowInfo.SetPositionFlags.NoOwnerZOrder
			If dwmEnabled Then
				ret = ret Or WindowInfo.SetPositionFlags.NoRedraw
			End If
			Return ret
		End Get
	End Property

	Private Sub VirtualDesktopSwitching(prevDesk As Integer, newDesk As Integer)
		Dim allScreens() As Screen = Screen.AllScreens
		Dim extents = allScreens(0).Bounds

		'Update to the current settings
		animTime = My.Settings.WinAnimTime
		animOut = My.Settings.WinAnimOut
		animIn = My.Settings.WinAnimIn

		'Find the outer bounds of all monitors
		For i As Integer = 1 To allScreens.Length - 1
			Dim bounds = allScreens(i).Bounds
			If bounds.X < extents.X Then
				extents.Width += extents.X - bounds.X
				extents.X = bounds.X
			End If
			If bounds.Y < extents.Y Then
				extents.Height += extents.Y - bounds.Y
				extents.Y = bounds.Y
			End If
			If bounds.Right > extents.Right Then
				extents.Width = bounds.Right - extents.Width
			End If
			If bounds.Bottom > extents.Bottom Then
				extents.Height = bounds.Bottom - extents.Y
			End If
		Next

		'Calculate the imaginary locations that windows should animate to/from
		Dim rows As Integer = Math.Round(Math.Sqrt(VirtualDesktopManager.Desktops.Count))
		Dim cols As Integer = Math.Ceiling(CDbl(VirtualDesktopManager.Desktops.Count) / CDbl(rows))
		Dim entirePrevBounds As New Rectangle(0, 0, extents.Width * cols, extents.Height * rows)
		Dim oldDeskBounds = VirtualDesktopManager.GetDesktopPreviewBounds(prevDesk, entirePrevBounds)
		Dim newDeskBounds = VirtualDesktopManager.GetDesktopPreviewBounds(newDesk, entirePrevBounds)

		If animOut Then
			Dim oldWindows = VirtualDesktopManager.CurrentDesktop.Windows
			Dim oldWindowPositions As New Dictionary(Of WindowInfo, Rectangle)
			Dim newWindowPositions As New Dictionary(Of WindowInfo, Rectangle)

			'Store the old and new positions of the hiding windows
			For Each w In oldWindows
				If Not w.Minimized Then
					Dim r = w.Rectangle
					oldWindowPositions.Add(w, r)
					newWindowPositions.Add(w, New Rectangle(r.X + oldDeskBounds.X - newDeskBounds.X, r.Y + oldDeskBounds.Y - newDeskBounds.Y, r.Width, r.Height))

					If dwmEnabled Then w.SetForceTransitionsDisabled(True)
				End If
			Next

			Dim animationFlags As WindowInfo.SetPositionFlags = Me.AnimationFlags
			Dim winPosInfo As IntPtr
			Dim lastPositions As New Dictionary(Of WindowInfo, Rectangle)

			'Animate the hiding windows
			For i As Double = 0 To animTime + 0.01 Step 0.01
				Dim part = EaseOut(i / animTime)
				Dim wasError As Boolean
				Do
					wasError = False
					winPosInfo = WindowInfo.BeginDeferredPositions(oldWindows.Count)
					For Each w In oldWindowPositions
						Dim oldPos = w.Value
						Dim newPos = newWindowPositions(w.Key)
						Dim lastPos As Rectangle
						Dim hadLast = lastPositions.TryGetValue(w.Key, lastPos)
						Dim currPos = w.Key.Rectangle
						Dim nextPos As New Rectangle(oldPos.Location + New Size((newPos.X - oldPos.X) * part, (newPos.Y - oldPos.Y) * part), oldPos.Size)
						If Not hadLast OrElse currPos = lastPos Then
							winPosInfo = w.Key.SetDeferredPosition(winPosInfo, nextPos, IntPtr.Zero, animationFlags)
						End If
						If winPosInfo = IntPtr.Zero OrElse (hadLast AndAlso currPos <> lastPos) Then
							oldWindows.Remove(w.Key)
							oldWindowPositions.Remove(w.Key)
							If dwmEnabled Then w.Key.SetForceTransitionsDisabled(False)
							wasError = True
							Continue Do
						Else
							lastPositions(w.Key) = nextPos
						End If
					Next
					WindowInfo.EndDeferredPositions(winPosInfo)
				Loop While wasError AndAlso oldWindowPositions.Count > 0

				If oldWindowPositions.Count = 0 Then Exit For
				Threading.Thread.Sleep(10)
			Next

			Me.oldWindowOldPositions = oldWindowPositions
		End If

		'Reposition and store the old and new positions for the showing windows
		If animIn Then
			Dim newWindows = VirtualDesktopManager.Desktops(newDesk).Windows
			Me.newWindowOldPositions = New Dictionary(Of WindowInfo, Rectangle)
			Me.newWindowNewPositions = New Dictionary(Of WindowInfo, Rectangle)
			For Each w In newWindows
				Dim r = w.Rectangle
				newWindowNewPositions(w) = r
				Dim newRect As New Rectangle(r.X + newDeskBounds.X - oldDeskBounds.X, r.Y + newDeskBounds.Y - oldDeskBounds.Y, r.Width, r.Height)
				newWindowOldPositions(w) = newRect
				If dwmEnabled Then w.SetForceTransitionsDisabled(True)
				Try
					w.Rectangle = newRect
				Catch ex As Exception
					Debug.Print("Error moving window: " & ex.ToString)
				End Try
			Next
		End If
	End Sub

	Private Sub VirtualDesktopSwitched(prevDesk As Integer, newDesk As Integer)
		Dim animationFlags As WindowInfo.SetPositionFlags = Me.AnimationFlags

		'Restore the old (hidden) windows back to their proper position
		If animOut Then
			Dim winPosInfo = WindowInfo.BeginDeferredPositions(oldWindowOldPositions.Count)
			For Each w In oldWindowOldPositions
				winPosInfo = w.Key.SetDeferredPosition(winPosInfo, w.Value, IntPtr.Zero, animationFlags)
				If winPosInfo = IntPtr.Zero Then Exit For
			Next
			If winPosInfo = IntPtr.Zero Then
				For Each w In oldWindowOldPositions
					w.Key.SetPosition(w.Value, IntPtr.Zero, animationFlags)
				Next
			Else
				WindowInfo.EndDeferredPositions(winPosInfo)
			End If
		End If

		'Animate the showing windows
		If animIn Then
			Dim winPosInfo As IntPtr
			Dim lastPositions As New Dictionary(Of WindowInfo, Rectangle)

			'Animate the hiding windows
			For i As Double = 0 To animTime + 0.01 Step 0.01
				Dim part = EaseOut(i / animTime)
				Dim wasError As Boolean
				Do
					wasError = False
					winPosInfo = WindowInfo.BeginDeferredPositions(newWindowOldPositions.Count)
					For Each w In newWindowOldPositions
						Dim oldPos = w.Value
						Dim newPos = newWindowNewPositions(w.Key)
						Dim lastPos As Rectangle
						Dim hadLast = lastPositions.TryGetValue(w.Key, lastPos)
						Dim currPos = w.Key.Rectangle
						Dim nextPos As New Rectangle(oldPos.Location + New Size((newPos.X - oldPos.X) * part, (newPos.Y - oldPos.Y) * part), oldPos.Size)
						If part >= 1 Then
							winPosInfo = w.Key.SetDeferredPosition(winPosInfo, newPos, IntPtr.Zero, animationFlags And (Not WindowInfo.SetPositionFlags.NoRedraw))
						Else
							winPosInfo = w.Key.SetDeferredPosition(winPosInfo, nextPos, IntPtr.Zero, animationFlags)
						End If
						If winPosInfo = IntPtr.Zero OrElse (hadLast AndAlso lastPos <> currPos) Then
							Dim win = w.Key
							Try
								win.Rectangle = newWindowNewPositions(w.Key)
							Catch ex As Exception
								Debug.Print("Error moving window: " & ex.ToString)
							End Try
							newWindowOldPositions.Remove(w.Key)
							wasError = True
							Continue Do
						Else
							lastPositions(w.Key) = nextPos
						End If
					Next
					WindowInfo.EndDeferredPositions(winPosInfo)
				Loop While wasError AndAlso newWindowOldPositions.Count > 0

				If newWindowOldPositions.Count = 0 Then Exit For
				Threading.Thread.Sleep(10)
			Next

			'Force the animated windows to refresh
			For Each w In newWindowNewPositions
				w.Key.SetPosition(w.Value, IntPtr.Zero, WindowInfo.SetPositionFlags.NoZOrder Or WindowInfo.SetPositionFlags.NoOwnerZOrder Or WindowInfo.SetPositionFlags.NoSize Or WindowInfo.SetPositionFlags.NoActivate)
				w.Key.Refresh()
				If dwmEnabled Then w.Key.SetForceTransitionsDisabled(False)
			Next
		End If
	End Sub

	''' <summary>
	''' Implements a simple quadratic ease function.
	''' </summary>
	''' <param name="x">A number between 0 and 1 that indicates the linear ease value.</param>
	''' <returns></returns>
	''' <remarks></remarks>
	Private Function EaseOut(x As Double) As Double
		x = Math.Min(1, Math.Max(0, x))
		Return 1 - (1 - x) ^ 2
	End Function

End Class
