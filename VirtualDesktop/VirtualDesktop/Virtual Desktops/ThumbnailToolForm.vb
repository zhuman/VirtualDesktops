Public Class ThumbnailToolForm

	Dim _window As WindowInfo

	Public Property Window() As WindowInfo
		Get
			Return _window
		End Get
		Set(value As WindowInfo)
			_window = value
		End Set
	End Property

	Public Sub New(win As WindowInfo)
		InitializeComponent()

		_window = win
	End Sub

	Dim thumb As Thumbnail

	Private Sub ThumbnailToolForm_Load(sender As System.Object, e As System.EventArgs) Handles MyBase.Load
		If _window.IsValid Then
			thumb = New Thumbnail(_window.Handle, Me)
			thumb.Visible = True
			thumb.DestinationRectangle = New Rectangle(New Point(0, 0), Me.ClientSize)
			thumb.UseEntireSource = True
			thumb.UpdateRendering()
		End If
	End Sub

	Private Sub ThumbnailToolForm_Resize(sender As Object, e As System.EventArgs) Handles Me.Resize
		If thumb IsNot Nothing Then
			If _window.IsValid Then
				thumb.DestinationRectangle = New Rectangle(New Point(0, 0), Me.ClientSize - New Size(0, If(Me.TrackBar1.Visible, Me.TrackBar1.Height, 0)))
				thumb.UpdateRendering()
			Else
				thumb.Dispose()
				Me.Close()
			End If
		End If
	End Sub

	Private Sub StayOnTopToolStripMenuItem_Click(sender As System.Object, e As System.EventArgs) Handles StayOnTopToolStripMenuItem.Click
		Me.TopMost = Me.StayOnTopToolStripMenuItem.Checked
	End Sub

	Private Sub TrackBar1_Scroll(sender As System.Object, e As System.EventArgs) Handles TrackBar1.Scroll
		Me.Opacity = Me.TrackBar1.Value / 100
	End Sub

	Private Sub ShowTransparencySliderToolStripMenuItem_Click(sender As System.Object, e As System.EventArgs) Handles ShowTransparencySliderToolStripMenuItem.Click
		Me.TrackBar1.Visible = Me.ShowTransparencySliderToolStripMenuItem.Checked
		If thumb IsNot Nothing Then
			thumb.DestinationRectangle = New Rectangle(New Point(0, 0), Me.ClientSize - New Size(0, If(Me.TrackBar1.Visible, Me.TrackBar1.Height, 0)))
			thumb.UpdateRendering()
		End If
	End Sub

End Class