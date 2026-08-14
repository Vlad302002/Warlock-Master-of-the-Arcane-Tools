rem ExtractLoc is a subfolder of where data is found.
set ExtractLoc=w2extract\
title Unpacking .pack files with Squeezer.exe
for /r %%I in (*.pack) do mkdir "%%~dI%%~pI%ExtractLoc%%%~nI"
for /r %%I in (*.pack) do squeezer.exe /e "%%~fI" "%%~dI%%~pI%ExtractLoc%%%~nI">>w2extract.txt

title Unpacking .xr files to .xml
for /r %%I in (*.xr) do xrconvert_final.exe -t:text "%%~fI" "%%~dI%%~pI%%~nI.xml">>"w2extract.txt"

title Unpacking .binary files to .xml
for /r %%I in (*.binary) do txmlconvert.exe \tUTF8 "%%~fI" "%%~dI%%~pI%%~nI.xml">>"w2extract.txt"

title w2extract Finished!