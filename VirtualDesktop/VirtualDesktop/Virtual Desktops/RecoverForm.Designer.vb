<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class RecoverForm
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
		Me.components = New System.ComponentModel.Container()
		Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(RecoverForm))
		Me.lstWindows = New System.Windows.Forms.ListView()
		Me.ColumnHeader1 = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
		Me.imlWindows = New System.Windows.Forms.ImageList(Me.components)
		Me.Label1 = New System.Windows.Forms.Label()
		Me.btnUnhide = New System.Windows.Forms.Button()
		Me.btnClose = New System.Windows.Forms.Button()
		Me.btnRefresh = New System.Windows.Forms.Button()
		Me.SuspendLayout()
		'
		'lstWindows
		'
		resources.ApplyResources(Me.lstWindows, "lstWindows")
		Me.lstWindows.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.ColumnHeader1})
		Me.lstWindows.LargeImageList = Me.imlWindows
		Me.lstWindows.Name = "lstWindows"
		Me.lstWindows.SmallImageList = Me.imlWindows
		Me.lstWindows.UseCompatibleStateImageBehavior = False
		Me.lstWindows.View = System.Windows.Forms.View.Details
		'
		'ColumnHeader1
		'
		resources.ApplyResources(Me.ColumnHeader1, "ColumnHeader1")
		'
		'imlWindows
		'
		Me.imlWindows.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit
		resources.ApplyResources(Me.imlWindows, "imlWindows")
		Me.imlWindows.TransparentColor = System.Drawing.Color.Transparent
		'
		'Label1
		'
		resources.ApplyResources(Me.Label1, "Label1")
		Me.Label1.Name = "Label1"
		'
		'btnUnhide
		'
		resources.ApplyResources(Me.btnUnhide, "btnUnhide")
		Me.btnUnhide.Name = "btnUnhide"
		Me.btnUnhide.UseVisualStyleBackColor = True
		'
		'btnClose
		'
		resources.ApplyResources(Me.btnClose, "btnClose")
		Me.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel
		Me.btnClose.Name = "btnClose"
		Me.btnClose.UseVisualStyleBackColor = True
		'
		'btnRefresh
		'
		resources.ApplyResources(Me.btnRefresh, "btnRefresh")
		Me.btnRefresh.Name = "btnRefresh"
		Me.btnRefresh.UseVisualStyleBackColor = True
		'
		'RecoverForm
		'
		Me.AcceptButton = Me.btnUnhide
		resources.ApplyResources(Me, "$this")
		Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
		Me.CancelButton = Me.btnClose
		Me.Controls.Add(Me.btnRefresh)
		Me.Controls.Add(Me.btnClose)
		Me.Controls.Add(Me.btnUnhide)
		Me.Controls.Add(Me.Label1)
		Me.Controls.Add(Me.lstWindows)
		Me.MaximizeBox = False
		Me.Name = "RecoverForm"
		Me.TopMost = True
		Me.ResumeLayout(False)
		Me.PerformLayout()

	End Sub
    Friend WithEvents lstWindows As System.Windows.Forms.ListView
    Friend WithEvents imlWindows As System.Windows.Forms.ImageList
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents btnUnhide As System.Windows.Forms.Button
    Friend WithEvents btnClose As System.Windows.Forms.Button
    Friend WithEvents ColumnHeader1 As System.Windows.Forms.ColumnHeader
    Friend WithEvents btnRefresh As System.Windows.Forms.Button
End Class
