Public Class WelcomeForm

	Public Sub New()
		Me.Font = SystemFonts.DialogFont

		' This call is required by the designer.
		InitializeComponent()

		' Add any initialization after the InitializeComponent() call.

	End Sub

	Private Sub Button2_Click(sender As System.Object, e As System.EventArgs) Handles Button2.Click
		Utilities.LaunchApplication(New ProcessStartInfo("http://www.z-sys.org/donate/") With {.UseShellExecute = True})
	End Sub

	Private Sub Button3_Click(sender As System.Object, e As System.EventArgs) Handles Button3.Click
		Me.Close()
	End Sub

	Private Sub WelcomeForm_Load(sender As System.Object, e As System.EventArgs) Handles MyBase.Load
		Me.Font = SystemFonts.DefaultFont
		lblVersion.Text = My.Application.Info.Version.ToString
		RichTextBox1.Rtf = My.Resources.NewFeatures
	End Sub

End Class