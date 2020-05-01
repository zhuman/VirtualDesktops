<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class WelcomeWizard
	Inherits GlassForm

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
		Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(WelcomeWizard))
		Me.CommandLink1 = New Microsoft.WindowsAPICodePack.Controls.WindowsForms.CommandLink()
		Me.Panel1 = New System.Windows.Forms.Panel()
		Me.CommandLink2 = New Microsoft.WindowsAPICodePack.Controls.WindowsForms.CommandLink()
		Me.RichTextBox1 = New System.Windows.Forms.RichTextBox()
		Me.Label1 = New System.Windows.Forms.Label()
		Me.CommandLink3 = New Microsoft.WindowsAPICodePack.Controls.WindowsForms.CommandLink()
		Me.PictureBox1 = New System.Windows.Forms.PictureBox()
		Me.Panel1.SuspendLayout()
		CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).BeginInit()
		Me.SuspendLayout()
		'
		'CommandLink1
		'
		Me.CommandLink1.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
				  Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.CommandLink1.FlatStyle = System.Windows.Forms.FlatStyle.System
		Me.CommandLink1.Location = New System.Drawing.Point(15, 194)
		Me.CommandLink1.Name = "CommandLink1"
		Me.CommandLink1.NoteText = ""
		Me.CommandLink1.Size = New System.Drawing.Size(275, 45)
		Me.CommandLink1.TabIndex = 0
		Me.CommandLink1.Text = "Just start using Finestra!"
		Me.CommandLink1.UseVisualStyleBackColor = True
		'
		'Panel1
		'
		Me.Panel1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
				  Or System.Windows.Forms.AnchorStyles.Left) _
				  Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.Panel1.BackColor = System.Drawing.SystemColors.ControlLightLight
		Me.Panel1.Controls.Add(Me.CommandLink2)
		Me.Panel1.Controls.Add(Me.RichTextBox1)
		Me.Panel1.Controls.Add(Me.Label1)
		Me.Panel1.Controls.Add(Me.CommandLink3)
		Me.Panel1.Controls.Add(Me.CommandLink1)
		Me.Panel1.Location = New System.Drawing.Point(0, 54)
		Me.Panel1.Name = "Panel1"
		Me.Panel1.Size = New System.Drawing.Size(566, 258)
		Me.Panel1.TabIndex = 1
		'
		'CommandLink2
		'
		Me.CommandLink2.FlatStyle = System.Windows.Forms.FlatStyle.System
		Me.CommandLink2.Location = New System.Drawing.Point(15, 143)
		Me.CommandLink2.Name = "CommandLink2"
		Me.CommandLink2.NoteText = ""
		Me.CommandLink2.Size = New System.Drawing.Size(275, 45)
		Me.CommandLink2.TabIndex = 10
		Me.CommandLink2.Text = "Donate"
		Me.CommandLink2.UseVisualStyleBackColor = True
		'
		'RichTextBox1
		'
		Me.RichTextBox1.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
				  Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.RichTextBox1.BackColor = System.Drawing.SystemColors.ControlLightLight
		Me.RichTextBox1.BorderStyle = System.Windows.Forms.BorderStyle.None
		Me.RichTextBox1.Location = New System.Drawing.Point(296, 92)
		Me.RichTextBox1.Name = "RichTextBox1"
		Me.RichTextBox1.ReadOnly = True
		Me.RichTextBox1.Size = New System.Drawing.Size(255, 147)
		Me.RichTextBox1.TabIndex = 9
		Me.RichTextBox1.Text = ""
		'
		'Label1
		'
		Me.Label1.Font = New System.Drawing.Font("Segoe UI", 9.0!)
		Me.Label1.Location = New System.Drawing.Point(12, 20)
		Me.Label1.Name = "Label1"
		Me.Label1.Size = New System.Drawing.Size(520, 69)
		Me.Label1.TabIndex = 3
		Me.Label1.Text = resources.GetString("Label1.Text")
		'
		'CommandLink3
		'
		Me.CommandLink3.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
				  Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.CommandLink3.FlatStyle = System.Windows.Forms.FlatStyle.System
		Me.CommandLink3.Location = New System.Drawing.Point(15, 92)
		Me.CommandLink3.Name = "CommandLink3"
		Me.CommandLink3.NoteText = ""
		Me.CommandLink3.Size = New System.Drawing.Size(275, 45)
		Me.CommandLink3.TabIndex = 2
		Me.CommandLink3.Text = "Setup hotkeys and other options"
		Me.CommandLink3.UseVisualStyleBackColor = True
		'
		'PictureBox1
		'
		Me.PictureBox1.BackColor = System.Drawing.Color.Transparent
		Me.PictureBox1.Image = Global.Finestra.My.Resources.Resources.Finestra48
		Me.PictureBox1.Location = New System.Drawing.Point(12, 0)
		Me.PictureBox1.Name = "PictureBox1"
		Me.PictureBox1.Size = New System.Drawing.Size(48, 48)
		Me.PictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize
		Me.PictureBox1.TabIndex = 4
		Me.PictureBox1.TabStop = False
		'
		'WelcomeWizard
		'
		Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
		Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
		Me.ClientSize = New System.Drawing.Size(566, 312)
		Me.Controls.Add(Me.PictureBox1)
		Me.Controls.Add(Me.Panel1)
		Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
		Me.MaximizeBox = False
		Me.MinimizeBox = False
		Me.Name = "WelcomeWizard"
		Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
		Me.Text = "Welcome"
		Me.TopMost = True
		Me.Panel1.ResumeLayout(False)
		CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).EndInit()
		Me.ResumeLayout(False)
		Me.PerformLayout()

	End Sub
	Friend WithEvents CommandLink1 As Microsoft.WindowsAPICodePack.Controls.WindowsForms.CommandLink
	Friend WithEvents Panel1 As System.Windows.Forms.Panel
	Friend WithEvents CommandLink3 As Microsoft.WindowsAPICodePack.Controls.WindowsForms.CommandLink
	Friend WithEvents Label1 As System.Windows.Forms.Label
	Friend WithEvents PictureBox1 As System.Windows.Forms.PictureBox
	Friend WithEvents RichTextBox1 As System.Windows.Forms.RichTextBox
	Friend WithEvents CommandLink2 As Microsoft.WindowsAPICodePack.Controls.WindowsForms.CommandLink
End Class
