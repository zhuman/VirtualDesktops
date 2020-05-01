Imports System.Runtime.InteropServices

Public Class WelcomeWizard
	Inherits GlassForm

	Dim vdm As VirtualDesktopManager

	Public Sub New(vdm As VirtualDesktopManager)
		Me.vdm = vdm
		Me.Font = SystemFonts.DialogFont

		' This call is required by the designer.
		InitializeComponent()

		' Add any initialization after the InitializeComponent() call.
		Me.SetStyle(ControlStyles.OptimizedDoubleBuffer, True)
	End Sub

	Private Sub WelcomeWizard_Load(sender As Object, e As System.EventArgs) Handles Me.Load
		RichTextBox1.Rtf = My.Resources.NewFeatures
		Me.BackColor = Color.Transparent
		Me.GlassMargin = New Padding(0, Panel1.Top, 0, 0)
		GlassForm.NativeMethods.SetWindowThemeAttribute(Me.Handle, NativeMethods.WindowThemeAttributeType.WTA_NONCLIENT, New NativeMethods.WTA_OPTIONS With {.Flags = NativeMethods.WindowThemeNonClientAttribute.WTNCA_NODRAWCAPTION Or NativeMethods.WindowThemeNonClientAttribute.WTNCA_NODRAWICON, .Mask = .Flags}, Marshal.SizeOf(GetType(NativeMethods.WTA_OPTIONS)))
	End Sub

	Private Sub WelcomeWizard_Paint(sender As Object, e As System.Windows.Forms.PaintEventArgs) Handles Me.Paint
		GlassForm.DrawGlassText(e.Graphics, "Finestra Virtual Desktops", New Font("Gabriola", 32, FontStyle.Regular, GraphicsUnit.Pixel), Color.Black, New Rectangle(PictureBox1.Right + 12, -5, 500, 58), 0, 10)
		GlassForm.DrawGlassText(e.Graphics, My.Application.Info.Version.ToString, SystemFonts.CaptionFont, Color.Black, New Rectangle(0, 0, Me.ClientRectangle.Width - 10, Panel1.Top - 10), TextFormatFlags.Right Or TextFormatFlags.Bottom Or TextFormatFlags.SingleLine, 10)
	End Sub

	Private Sub CommandLink1_Click(sender As System.Object, e As System.EventArgs) Handles CommandLink1.Click
		Me.Close()
	End Sub

	Private Sub CommandLink3_Click(sender As System.Object, e As System.EventArgs) Handles CommandLink3.Click
		Me.Close()
		vdm.SendGlobalCommand("ShowOptions")
	End Sub

	Private Sub CommandLink2_Click(sender As System.Object, e As System.EventArgs) Handles CommandLink2.Click
		Utilities.LaunchApplication(New ProcessStartInfo("http://www.z-sys.org/donate/") With {.UseShellExecute = True})
	End Sub

End Class