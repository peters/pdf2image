pdf2image
=========

A simple windows commandline utility that allows you to quickly
convert pdf documents to images. 

The software itself uses ImageMagick and GhostScript.
ImageMagick supports 228 image formats.

If you haven't already please install [GhostScript 9.07 (32-bit)](http://downloads.ghostscript.com/public/gs907w32.exe)
before you proceed. ImageMagick will be automatically downloaded by pdf2image.

Obtaining ze binary
==================
git clone https://github.com/peters/pdf2image pdf2image

Add C:\my\path\pdf2image\dist\pdf2image to your PATH environment variable.

pdf2image.exe --help


Supported platforms
===================
- Windows XP SP3 
- Windows Vista
- Windows 7
- Windows 8

Development prerequisites
=========================
- NET Framework >= 3.5
- [GhostScript 9.07 (32-bit)](http://downloads.ghostscript.com/public/gs907w32.exe)
