<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ThumbnailToolForm
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
        Me.components = New System.ComponentModel.Container
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(ThumbnailToolForm))
        Me.ContextMenuStrip1 = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.StayOnTopToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
        Me.ShowTransparencySliderToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
        Me.TrackBar1 = New System.Windows.Forms.TrackBar
        Me.ContextMenuStrip1.SuspendLayout()
        CType(Me.TrackBar1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'ContextMenuStrip1
        '
        Me.ContextMenuStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.StayOnTopToolStripMenuItem, Me.ShowTransparencySliderToolStripMenuItem})
        Me.ContextMenuStrip1.Name = "ContextMenuStrip1"
        resources.ApplyResources(Me.ContextMenuStrip1, "ContextMenuStrip1")
        '
        'StayOnTopToolStripMenuItem
        '
        Me.StayOnTopToolStripMenuItem.Checked = True
        Me.StayOnTopToolStripMenuItem.CheckOnClick = True
        Me.StayOnTopToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked
        Me.StayOnTopToolStripMenuItem.Name = "StayOnTopToolStripMenuItem"
        resources.ApplyResources(Me.StayOnTopToolStripMenuItem, "StayOnTopToolStripMenuItem")
        '
        'ShowTransparencySliderToolStripMenuItem
        '
        Me.ShowTransparencySliderToolStripMenuItem.CheckOnClick = True
        Me.ShowTransparencySliderToolStripMenuItem.Name = "ShowTransparencySliderToolStripMenuItem"
        resources.ApplyResources(Me.ShowTransparencySliderToolStripMenuItem, "ShowTransparencySliderToolStripMenuItem")
        '
        'TrackBar1
        '
        resources.ApplyResources(Me.TrackBar1, "TrackBar1")
        Me.TrackBar1.Maximum = 100
        Me.TrackBar1.Name = "TrackBar1"
        Me.TrackBar1.TickFrequency = 5
        Me.TrackBar1.Value = 100
        '
        'ThumbnailToolForm
        '
        resources.ApplyResources(Me, "$this")
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ContextMenuStrip = Me.ContextMenuStrip1
        Me.Controls.Add(Me.TrackBar1)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow
        Me.Name = "ThumbnailToolForm"
        Me.TopMost = True
        Me.ContextMenuStrip1.ResumeLayout(False)
        CType(Me.TrackBar1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents ContextMenuStrip1 As System.Windows.Forms.ContextMenuStrip
    Friend WithEvents StayOnTopToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents ShowTransparencySliderToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents TrackBar1 As System.Windows.Forms.TrackBar
End Class
