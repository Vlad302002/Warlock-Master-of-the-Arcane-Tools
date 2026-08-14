FOR  %%I  IN (%1\*.xml) DO TXMLConvert.exe \tbin %%I "%1\%%~nI.binary"
Squeezer.exe /d ./%1 %1.pack .wav.ogg.ogm.mp3.avi.dds.png.tga.jpg.fev.fsb .wav.ogg.ogm.mp3.avi.dds.png.tga.jpg.fev.fsb 5
 rem Copy d179.pack c:\Program Files (x86)\Paradox Interactive\Warlock - Master of the Arcane\