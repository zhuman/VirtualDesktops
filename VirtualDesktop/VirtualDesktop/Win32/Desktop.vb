Public Class Desktop

	Private Sub New()

	End Sub

	Private Declare Auto Function SystemParametersInfo Lib "user32.dll" (uiAction As Integer, uiParam As Integer, pvParam As IntPtr, WinIni As Integer) As Boolean
	Private Declare Auto Function SetDeskWallpaper Lib "user32.dll" Alias "SystemParametersInfo" (uiAction As Integer, uiParam As Integer, pvParam As String, WinIni As Integer) As Boolean
	Private Declare Auto Function GetDeskWallpaper Lib "user32.dll" Alias "SystemParametersInfo" (uiAction As Integer, uiParam As Integer, pvParam As System.Text.StringBuilder, winini As Integer) As Boolean

	Private Declare Auto Function PaintDesktop Lib "user32.dll" (hdc As IntPtr) As Boolean

	Private Const SPI_SETDESKWALLPAPER As Integer = &H14
	Private Const SPI_GETDESKWALLPAPER As Integer = &H73

	Private Const SPIF_UPDATEINIFILE As Integer = &H1
	Private Const SPIF_SENDWININICHANGE As Integer = &H2

	Public Shared Property Wallpaper() As String
		Get
			Dim sb As New System.Text.StringBuilder(260)
			If GetDeskWallpaper(SPI_GETDESKWALLPAPER, 260, sb, 0) Then
				If Environment.OSVersion.Version.Major < 6 AndAlso CStr(Microsoft.Win32.Registry.GetValue("HKEY_CURRENT_USER\Control Panel\Desktop", "OriginalWallpaper", "NotConverted")) = sb.ToString Then
					Return Microsoft.Win32.Registry.GetValue("HKEY_CURRENT_USER\Control Panel\Desktop", "ConvertedWallpaper", sb.ToString)
				Else
					Return sb.ToString
				End If
			Else
				Return ""
			End If
		End Get
		Set(value As String)
			Try
				If IO.File.Exists(value) Then
					If Environment.OSVersion.Version.Major < 6 AndAlso Not (LCase(value) Like "*.bmp") Then
						Dim savepath As String = Microsoft.Win32.Registry.GetValue("HKEY_CURRENT_USER\Control Panel\Desktop", "OriginalWallpaper", "")
						If savepath <> "" Then
							Try
								Dim b As New Bitmap(value)
								b.Save(savepath, Imaging.ImageFormat.Bmp)
								Microsoft.Win32.Registry.SetValue("HKEY_CURRENT_USER\Control Panel\Desktop", "ConvertedWallpaper", value)
								SetDeskWallpaper(SPI_SETDESKWALLPAPER, 0, savepath, SPIF_SENDWININICHANGE Or SPIF_UPDATEINIFILE)
							Catch ex As Exception
								SetDeskWallpaper(SPI_SETDESKWALLPAPER, 0, value, SPIF_SENDWININICHANGE Or SPIF_UPDATEINIFILE)
							End Try
						Else
							SetDeskWallpaper(SPI_SETDESKWALLPAPER, 0, value, SPIF_SENDWININICHANGE Or SPIF_UPDATEINIFILE)
						End If
					Else
						SetDeskWallpaper(SPI_SETDESKWALLPAPER, 0, value, SPIF_SENDWININICHANGE Or SPIF_UPDATEINIFILE)
					End If
				End If
			Catch ex As Exception
				Debug.Print("Error setting wallpaper: " & ex.Message)
			End Try
		End Set
	End Property

	Public Enum WallpaperStyleEnum As Integer
		Tiled
		Centered
		Stretched
	End Enum

	Public Shared Property WallpaperStyleData1() As String
		Get
			Dim key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Control Panel\Desktop", True)
			Return CStr(key.GetValue("WallpaperStyle", 2.ToString()))
		End Get
		Set(value As String)
			Dim key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Control Panel\Desktop", True)
			key.SetValue("WallpaperStyle", value)
		End Set
	End Property

	Public Shared Property WallpaperStyleData2() As String
		Get
			Dim key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Control Panel\Desktop", True)
			Return CStr(key.GetValue("TileWallpaper", 0.ToString))
		End Get
		Set(value As String)
			Dim key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Control Panel\Desktop", True)
			key.SetValue("TileWallpaper", value)
		End Set
	End Property

	Public Shared Property WallpaperStyle() As WallpaperStyleEnum
		Get
			Dim key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Control Panel\Desktop", True)
			Select Case CStr(key.GetValue("WallpaperStyle", "2"))
				Case "2"""
					Return WallpaperStyleEnum.Stretched
				Case Else
					If CStr(key.GetValue("TileWallpaper", "0")) = "0" Then
						Return WallpaperStyleEnum.Centered
					Else
						Return WallpaperStyleEnum.Tiled
					End If
			End Select
		End Get
		Set(value As WallpaperStyleEnum)
			Dim key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Control Panel\Desktop", True)
			Select Case value
				Case WallpaperStyleEnum.Stretched
					key.SetValue("WallpaperStyle", 2.ToString())
					key.SetValue("TileWallpaper", 0.ToString())
				Case WallpaperStyleEnum.Centered
					key.SetValue("WallpaperStyle", 0.ToString())
					key.SetValue("TileWallpaper", 0.ToString())
				Case WallpaperStyleEnum.Tiled
					key.SetValue("WallpaperStyle", 1.ToString())
					key.SetValue("TileWallpaper", 1.ToString())
			End Select
			'Wallpaper = Wallpaper
		End Set
	End Property

	Public Shared Sub DrawDesktopBackground(gr As Graphics)
		Dim hdc = gr.GetHdc()
		PaintDesktop(hdc)
		gr.ReleaseHdc()
	End Sub

End Class
