Imports System.Windows.Forms

Public Class SplashScreen
	Inherits ZPixel.LayeredForm

	WithEvents fadeOutTim As New Timer With {.Interval = 1}
	Dim continueFadeOut, finishedFadeIn As Boolean

	Public Sub New()
		MyBase.New(True)
		Me.ShowInTaskbar = False
		Me.ControlBox = False
		Me.TopMost = True
		Dim b As New Bitmap(My.Resources.Splash.Width, My.Resources.Splash.Height, Imaging.PixelFormat.Format32bppPArgb)
		Using gr As Graphics = Graphics.FromImage(b)
			gr.DrawImage(My.Resources.Splash, Point.Empty)
		End Using
		Me.Image = b
		Me.StartPosition = Windows.Forms.FormStartPosition.CenterScreen
	End Sub

	Shared splash As SplashScreen

	Private Shared Sub SplashScreenThread()
		Try
			Threading.Thread.CurrentThread.Name = "Finestra: Splash Screen Thread"
			splash = New SplashScreen
			splash.StartFade(300)
			Windows.Forms.Application.Run(splash)
		Catch ex As Exception

		End Try
	End Sub

	Public Shared Sub ShowSplash()
		Dim t As New Threading.Thread(AddressOf SplashScreenThread)
		t.Start()
	End Sub

	Public Shared Sub QuickCloseSplash()
		If splash IsNot Nothing Then
			Try
				splash.Invoke(Sub()
								  splash.Close()
							  End Sub)
			Catch ex As Exception

			End Try
		End If
	End Sub

	Public Shared Sub CloseSplash()
		If splash IsNot Nothing Then
			Try
				splash.Invoke(Sub()
								  If splash.finishedFadeIn Then
									  splash.fadeOutTim.Enabled = True
								  Else
									  splash.continueFadeOut = True
								  End If
							  End Sub)
			Catch ex As Exception

			End Try
		End If
	End Sub

	Public Shared Sub AddOnCloseHandler(handler As EventHandler)
		If splash IsNot Nothing Then
			If splash.Visible Then
				AddHandler splash.FadeOutFinished, handler
			Else
				handler.Invoke(splash, New EventArgs)
			End If
		End If
	End Sub

	Private Sub fadeOutTim_Tick(sender As Object, e As System.EventArgs) Handles fadeOutTim.Tick
		Me.StartFade(500)
		fadeOutTim.Enabled = False
	End Sub

	Private Event FadeOutFinished As EventHandler

	Private Sub SplashScreen_FadeFinished(sender As Object, e As System.EventArgs) Handles Me.FadeFinished
		If Not finishedFadeIn Then
			finishedFadeIn = True
			If continueFadeOut Then
				fadeOutTim.Enabled = True
			End If
		Else
			RaiseEvent FadeOutFinished(Me, New EventArgs)
		End If
	End Sub

End Class
