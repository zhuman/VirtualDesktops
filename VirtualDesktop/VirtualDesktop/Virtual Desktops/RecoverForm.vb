Public Class RecoverForm

	Dim vdm As VirtualDesktopManager

	Public Sub New(vdm As VirtualDesktopManager)
		Me.vdm = vdm

		' This call is required by the designer.
		InitializeComponent()

		' Add any initialization after the InitializeComponent() call.

	End Sub

	Private Sub RecoverForm_FormClosing(sender As Object, e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
		If e.CloseReason = CloseReason.UserClosing Then
			e.Cancel = True
			Me.Hide()
		End If
	End Sub

	Private Sub RecoverForm_Load(sender As System.Object, e As System.EventArgs) Handles MyBase.Load
		Cursor.Current = Cursors.AppStarting
		RefreshList()
		Cursor.Current = Cursors.Default
	End Sub

	Private Sub btnRefresh_Click(sender As System.Object, e As System.EventArgs) Handles btnRefresh.Click
		Cursor.Current = Cursors.AppStarting
		RefreshList()
		Cursor.Current = Cursors.Default
	End Sub

	Private Sub RefreshList()
		Try
			lstWindows.Items.Clear()
			imlWindows.Images.Clear()
			lstWindows.BeginUpdate()
			Dim wins As List(Of WindowInfo) = WindowInfo.GetWindows
			SyncLock wins
				For Each w As WindowInfo In wins
					If vdm.IsWindowValid(w, True, True) AndAlso w.Text <> "Default IME" AndAlso w.Text <> "M" AndAlso w.Text <> "GDI+ Window" AndAlso w.Text <> "DDE Server Window" Then
						Try
							Dim lvi As New ListViewItem(w.Text)
							'lvi.SubItems.Add(w.Process.MainModule.FileName)
							imlWindows.Images.Add(w.Handle.ToString, w.Icon(WindowInfo.WindowIconSize.Small2).ToBitmap)
							lvi.ImageKey = w.Handle.ToString
							lvi.Tag = w.Handle
							lstWindows.Items.Add(lvi)
						Catch ex As Exception

						End Try
					End If
				Next
			End SyncLock
			lstWindows.EndUpdate()
		Catch ex As Exception

		End Try
	End Sub

	Private Sub btnUnhide_Click(sender As System.Object, e As System.EventArgs) Handles btnUnhide.Click
		If Not lstWindows.SelectedItems.Count = 0 Then
			Dim w As New WindowInfo(CType(lstWindows.SelectedItems(0).Tag, IntPtr))
			If Not w.Visible Then
				Try
					w.State = WindowInfo.WindowState.Show
				Catch ex As Exception
					MessageBox.Show("There was an error showing the window.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
				End Try
			Else
				Try
					w.State = WindowInfo.WindowState.Hide
				Catch ex As Exception
					MessageBox.Show("There was an error hiding the window.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
				End Try
			End If

		End If
	End Sub

	Private Sub btnClose_Click(sender As System.Object, e As System.EventArgs) Handles btnClose.Click
		Me.Hide()
	End Sub

End Class