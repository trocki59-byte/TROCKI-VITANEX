@echo off
chcp 65001 >nul
echo TROCKI VITANEX APK olusturuluyor...
cd /d "%~dp0VITANEX"
dotnet publish -f net10.0-android -c Release -p:AndroidPackageFormat=apk
if errorlevel 1 (
  echo.
  echo HATA: Derleme basarisiz. Yukaridaki hata mesajini Claude'a gonderin.
  pause
  exit /b 1
)
copy /Y "bin\Release\net10.0-android\publish\com.trocki.vitanex-Signed.apk" "%~dp0TROCKI_VITANEX.apk" >nul
if exist "%~dp0..\Trocki22Web\wwwroot\downloads" (
  copy /Y "%~dp0TROCKI_VITANEX.apk" "%~dp0..\Trocki22Web\wwwroot\downloads\TROCKI_VITANEX.apk" >nul
  echo Web sitesi icin de kopyalandi: Trocki22Web\wwwroot\downloads
)
echo.
echo TAMAM: APK hazir -^> %~dp0TROCKI_VITANEX.apk
pause
