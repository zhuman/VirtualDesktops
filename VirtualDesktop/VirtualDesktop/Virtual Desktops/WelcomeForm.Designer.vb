<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class WelcomeForm
	Inherits Form

    'Form overrides dispose to clean up the component list.
	<System.Diagnostics.DebuggerNonUserCode()> _
	Protected Overrides Sub Dispose(disposing As Boolean)
		Try
			If disposing AndAlso components IsNot Nothing Then
				components.Dispose()
			End If
		Finally
			MyBase.Dispose(disposing)
		End Try
	End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
		Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(WelcomeForm))
		Me.PictureBox1 = New System.Windows.Forms.PictureBox()
		Me.Label3 = New System.Windows.Forms.Label()
		Me.Label1 = New System.Windows.Forms.Label()
		Me.Button2 = New System.Windows.Forms.Button()
		Me.Button3 = New System.Windows.Forms.Button()
		Me.lblVersion = New System.Windows.Forms.Label()
		Me.RichTextBox1 = New System.Windows.Forms.RichTextBox()
		CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).BeginInit()
		Me.SuspendLayout()
		'
		'PictureBox1
		'
		Me.PictureBox1.BackColor = System.Drawing.Color.Transparent
		Me.PictureBox1.Image = Global.Finestra.My.Resources.Resources.Finestra128
		resources.ApplyResources(Me.PictureBox1, "PictureBox1")
		Me.PictureBox1.Name = "PictureBox1"
		Me.PictureBox1.TabStop = False
		'
		'Label3
		'
		resources.ApplyResources(Me.Label3, "Label3")
		Me.Label3.BackColor = System.Drawing.Color.Transparent
		Me.Label3.Name = "Label3"
		'
		'Label1
		'
		resources.ApplyResources(Me.Label1, "Label1")
		Me.Label1.BackColor = System.Drawing.Color.Transparent
		Me.Label1.Name = "Label1"
		'
		'Button2
		'
		resources.ApplyResources(Me.Button2, "Button2")
		Me.Button2.BackColor = System.Drawing.Color.Transparent
		Me.Button2.Name = "Button2"
		Me.Button2.UseVisualStyleBackColor = False
		'
		'Button3
		'
		resources.ApplyResources(Me.Button3, "Button3")
		Me.Button3.BackColor = System.Drawing.Color.Transparent
		Me.Button3.DialogResult = System.Windows.Forms.DialogResult.Cancel
		Me.Button3.Name = "Button3"
		Me.Button3.UseVisualStyleBackColor = False
		'
		'lblVersion
		'
		Me.lblVersion.BackColor = System.Drawing.Color.Transparent
		Me.lblVersion.ForeColor = System.Drawing.SystemColors.ControlDarkDark
		resources.ApplyResources(Me.lblVersion, "lblVersion")
		Me.lblVersion.Name = "lblVersion"
		'
		'RichTextBox1
		'
		resources.ApplyResources(Me.RichTextBox1, "RichTextBox1")
		Me.RichTextBox1.BackColor = System.Drawing.SystemColors.ControlLightLight
		Me.RichTextBox1.BorderStyle = System.Windows.Forms.BorderStyle.None
		Me.RichTextBox1.Name = "RichTextBox1"
		Me.RichTextBox1.ReadOnly = True
		'
		'WelcomeForm
		'
		Me.AcceptButton = Me.Button3
		resources.ApplyResources(Me, "$this")
		Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
		Me.BackColor = System.Drawing.Color.White
		Me.CancelButton = Me.Button3
		Me.Controls.Add(Me.RichTextBox1)
		Me.Controls.Add(Me.lblVersion)
		Me.Controls.Add(Me.Button3)
		Me.Controls.Add(Me.Button2)
		Me.Controls.Add(Me.Label1)
		Me.Controls.Add(Me.Label3)
		Me.Controls.Add(Me.PictureBox1)
		Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
		Me.MaximizeBox = False
		Me.MinimizeBox = False
		Me.Name = "WelcomeForm"
		CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).EndInit()
		Me.ResumeLayout(False)
		Me.PerformLayout()

	End Sub
	Friend WithEvents PictureBox1 As System.Windows.Forms.PictureBox
	Friend WithEvents Label3 As System.Windows.Forms.Label
	Friend WithEvents Label1 As System.Windows.Forms.Label
	Friend WithEvents Button2 As System.Windows.Forms.Button
	Friend WithEvents Button3 As System.Windows.Forms.Button
	Friend WithEvents lblVersion As System.Windows.Forms.Label
	Friend WithEvents RichTextBox1 As System.Windows.Forms.RichTextBox
End Class
