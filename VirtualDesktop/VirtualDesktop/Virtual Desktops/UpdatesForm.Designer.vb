<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class UpdatesForm
    Inherits System.Windows.Forms.Form

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
		Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(UpdatesForm))
		Me.lblStatus = New System.Windows.Forms.Label()
		Me.btnCancel = New System.Windows.Forms.Button()
		Me.prgProg = New System.Windows.Forms.ProgressBar()
		Me.btnDload = New System.Windows.Forms.Button()
		Me.wrkDload = New System.ComponentModel.BackgroundWorker()
		Me.LinkLabel1 = New System.Windows.Forms.LinkLabel()
		Me.Label1 = New System.Windows.Forms.Label()
		Me.SuspendLayout()
		'
		'lblStatus
		'
		Me.lblStatus.AutoSize = True
		Me.lblStatus.Location = New System.Drawing.Point(12, 16)
		Me.lblStatus.Name = "lblStatus"
		Me.lblStatus.Size = New System.Drawing.Size(117, 13)
		Me.lblStatus.TabIndex = 0
		Me.lblStatus.Text = "Checking for updates..."
		'
		'btnCancel
		'
		Me.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel
		Me.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System
		Me.btnCancel.Location = New System.Drawing.Point(316, 73)
		Me.btnCancel.Name = "btnCancel"
		Me.btnCancel.Size = New System.Drawing.Size(75, 23)
		Me.btnCancel.TabIndex = 1
		Me.btnCancel.Text = "&Cancel"
		Me.btnCancel.UseVisualStyleBackColor = True
		'
		'prgProg
		'
		Me.prgProg.Location = New System.Drawing.Point(15, 44)
		Me.prgProg.Name = "prgProg"
		Me.prgProg.Size = New System.Drawing.Size(376, 23)
		Me.prgProg.Style = System.Windows.Forms.ProgressBarStyle.Marquee
		Me.prgProg.TabIndex = 2
		'
		'btnDload
		'
		Me.btnDload.Enabled = False
		Me.btnDload.FlatStyle = System.Windows.Forms.FlatStyle.System
		Me.btnDload.Location = New System.Drawing.Point(15, 73)
		Me.btnDload.Name = "btnDload"
		Me.btnDload.Size = New System.Drawing.Size(176, 23)
		Me.btnDload.TabIndex = 3
		Me.btnDload.Text = "Download && Install Update"
		Me.btnDload.UseVisualStyleBackColor = True
		'
		'wrkDload
		'
		Me.wrkDload.WorkerReportsProgress = True
		Me.wrkDload.WorkerSupportsCancellation = True
		'
		'LinkLabel1
		'
		Me.LinkLabel1.AutoSize = True
		Me.LinkLabel1.LinkArea = New System.Windows.Forms.LinkArea(0, 16)
		Me.LinkLabel1.Location = New System.Drawing.Point(218, 108)
		Me.LinkLabel1.Margin = New System.Windows.Forms.Padding(0)
		Me.LinkLabel1.Name = "LinkLabel1"
		Me.LinkLabel1.Size = New System.Drawing.Size(86, 13)
		Me.LinkLabel1.TabIndex = 4
		Me.LinkLabel1.TabStop = True
		Me.LinkLabel1.Text = "Finestra Website"
		'
		'Label1
		'
		Me.Label1.AutoSize = True
		Me.Label1.Location = New System.Drawing.Point(12, 108)
		Me.Label1.Margin = New System.Windows.Forms.Padding(0)
		Me.Label1.Name = "Label1"
		Me.Label1.Size = New System.Drawing.Size(206, 13)
		Me.Label1.TabIndex = 5
		Me.Label1.Text = "You can also download updates manually:"
		'
		'UpdatesForm
		'
		Me.AcceptButton = Me.btnDload
		Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
		Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
		Me.CancelButton = Me.btnCancel
		Me.ClientSize = New System.Drawing.Size(403, 130)
		Me.Controls.Add(Me.LinkLabel1)
		Me.Controls.Add(Me.Label1)
		Me.Controls.Add(Me.btnDload)
		Me.Controls.Add(Me.prgProg)
		Me.Controls.Add(Me.btnCancel)
		Me.Controls.Add(Me.lblStatus)
		Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
		Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
		Me.MaximizeBox = False
		Me.Name = "UpdatesForm"
		Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
		Me.Text = "Finestra Updates"
		Me.ResumeLayout(False)
		Me.PerformLayout()

	End Sub
    Friend WithEvents lblStatus As System.Windows.Forms.Label
    Friend WithEvents btnCancel As System.Windows.Forms.Button
    Friend WithEvents prgProg As System.Windows.Forms.ProgressBar
    Friend WithEvents btnDload As System.Windows.Forms.Button
    Friend WithEvents wrkDload As System.ComponentModel.BackgroundWorker
    Friend WithEvents LinkLabel1 As System.Windows.Forms.LinkLabel
    Friend WithEvents Label1 As System.Windows.Forms.Label
End Class
