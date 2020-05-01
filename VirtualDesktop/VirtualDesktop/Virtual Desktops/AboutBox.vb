Public NotInheritable Class AboutBox

	Private Sub AboutBox_Load(sender As System.Object, e As System.EventArgs) Handles MyBase.Load
		Dim ApplicationTitle As String
		If My.Application.Info.Title <> "" Then
			ApplicationTitle = My.Application.Info.Title
		Else
			ApplicationTitle = System.IO.Path.GetFileNameWithoutExtension(My.Application.Info.AssemblyName)
		End If
		Me.Text = String.Format("About {0}", ApplicationTitle)
		Me.LabelProductName.Text = My.Application.Info.ProductName
		Me.LabelVersion.Text = String.Format("Version {0}", My.Application.Info.Version.ToString)
		Me.LabelCopyright.Text = My.Application.Info.Copyright
		Me.LabelCompanyName.Text = My.Application.Info.CompanyName
	End Sub

	Private Sub OKButton_Click(sender As System.Object, e As System.EventArgs) Handles OKButton.Click
		Me.Close()
	End Sub

End Class
