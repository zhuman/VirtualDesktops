Imports System.Windows

Public Class HotkeyControl

	Dim _isNumberKey As Boolean = False

	Public Sub New()

		' This call is required by the designer.
		InitializeComponent()

		' Add any initialization after the InitializeComponent() call.
		For Each k In [Enum].GetValues(GetType(Keys))
			cboKeys.Items.Add(k)
		Next
	End Sub

	Public Property Header As Object
		Get
			Return GetValue(HeaderProperty)
		End Get
		Set(value As Object)
			SetValue(HeaderProperty, value)
		End Set
	End Property

	Public Shared ReadOnly HeaderProperty As DependencyProperty = _
	  DependencyProperty.Register("Header", _
	  GetType(Object), GetType(HotkeyControl), _
	  New FrameworkPropertyMetadata(Nothing))

	Public Property HasKey As Boolean
		Get
			Return GetValue(HasKeyProperty)
		End Get
		Set(value As Boolean)
			SetValue(HasKeyProperty, value)
		End Set
	End Property

	Public Shared ReadOnly HasKeyProperty As DependencyProperty = _
	  DependencyProperty.Register("HasKey", _
	  GetType(Boolean), GetType(HotkeyControl), _
	  New FrameworkPropertyMetadata(True, AddressOf HasKeyChanged))

	Private Shared Sub HasKeyChanged(obj As DependencyObject, e As DependencyPropertyChangedEventArgs)
		Dim hot = CType(obj, HotkeyControl)
		hot.cboKeys.Visibility = If(CBool(e.NewValue), Visibility.Visible, Visibility.Collapsed)
	End Sub

	Public Property IsHotkeyEnabled As Boolean
		Get
			Return GetValue(IsHotkeyEnabledProperty)
		End Get
		Set(value As Boolean)
			SetValue(IsHotkeyEnabledProperty, value)
		End Set
	End Property

	Public Shared ReadOnly IsHotkeyEnabledProperty As DependencyProperty = _
	  DependencyProperty.Register("IsHotkeyEnabled", _
	  GetType(Boolean), GetType(HotkeyControl), _
	  New FrameworkPropertyMetadata(True))

	Public Property AcceleratorValue As Integer
		Get
			Dim value = 0
			If chkWin.IsChecked Then value = value Or HotKey.ModifierKeys.Windows
			If chkShift.IsChecked Then value = value Or HotKey.ModifierKeys.Shift
			If chkCtrl.IsChecked Then value = value Or HotKey.ModifierKeys.Control
			If chkAlt.IsChecked Then value = value Or HotKey.ModifierKeys.Alt
			Return value
		End Get
		Set(value As Integer)
			chkWin.IsChecked = (value And HotKey.ModifierKeys.Windows) = HotKey.ModifierKeys.Windows
			chkCtrl.IsChecked = (value And HotKey.ModifierKeys.Control) = HotKey.ModifierKeys.Control
			chkShift.IsChecked = (value And HotKey.ModifierKeys.Shift) = HotKey.ModifierKeys.Shift
			chkAlt.IsChecked = (value And HotKey.ModifierKeys.Alt) = HotKey.ModifierKeys.Alt
		End Set
	End Property

	Public Property SelectedKey As Windows.Forms.Keys
		Get
			Return cboKeys.SelectedValue
		End Get
		Set(value As Windows.Forms.Keys)
			cboKeys.SelectedValue = value
		End Set
	End Property

	Public Property SelectedKeyIndex As Integer
		Get
			Return cboKeys.SelectedIndex
		End Get
		Set(value As Integer)
			cboKeys.SelectedIndex = value
		End Set
	End Property

	Public Property IsNumberKey As Boolean
		Get
			Return _isNumberKey
		End Get
		Set(value As Boolean)
			_isNumberKey = value
			cboKeys.Items.Clear()
			If value Then
				cboKeys.Items.Add("Numpad")
				cboKeys.Items.Add("Top number keys")
			Else
				For Each k In [Enum].GetValues(GetType(Keys))
					cboKeys.Items.Add(k)
				Next
			End If
		End Set
	End Property


End Class
