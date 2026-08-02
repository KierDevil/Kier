# Kier Mobile

This is the Android/iOS mobile wrapper for the Kier web app. It opens the same Vue site inside a .NET MAUI app, so students, RFID, QR attendance, fines, and history still use the same ASP.NET API and MySQL database.

## How to run

1. Start the main Kier system on the server/laptop:

   ```powershell
   cd C:\appdev\Kier
   .\start.cmd
   ```

2. Make sure the phone and laptop are on the same Wi-Fi.

3. Build/run the mobile app from:

   ```text
   C:\appdev\Kier\mobile\Kier.Mobile
   ```

4. In the mobile app, enter the site URL, for example:

   ```text
   https://192.168.10.205:5173
   ```

For the QR scanner camera, Android or iOS will ask for camera permission. Allow it.

## Build Targets

Android can be built on Windows after installing the MAUI Android workload.

iOS support is included in the project, but building/running on iPhone requires a Mac with Xcode and Apple signing.

## Important

The mobile app is only the phone shell. The backend and database still need to run somewhere:

- local laptop/server for classroom use, or
- online hosting for access anywhere.
