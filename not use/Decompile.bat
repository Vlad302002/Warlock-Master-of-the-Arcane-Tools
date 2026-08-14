rem ExtractLoc is a subfolder of where data is found.
set ExtractLoc=\
title Unpacking .pack files with Squeezer.exe
for /r %%I in (*.pack) do mkdir "%%~dI%%~pI%ExtractLoc%%%~nI"
for /r %%I in (*.pack) do squeezer.exe /e "%%~fI" "%%~dI%%~pI%ExtractLoc%%%~nI"

title Unpacking .xr files to .xml
for /r %%I in (*.xr) do xrconvert_final.exe -t:text "%%~fI" "%%~dI%%~pI%%~nI.xml"

title Unpacking .binary files to .xml
for /r %%I in (*.binary) do txmlconvert.exe \tUTF8 "%%~fI" "%%~dI%%~pI%%~nI.xml"

title w2extract Finished!