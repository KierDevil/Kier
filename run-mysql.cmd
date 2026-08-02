@echo off
cd /d "%~dp0"
"%~dp0mysql-8.4.10-winx64\bin\mysqld.exe" --defaults-file="%~dp0mysql-local.ini"
