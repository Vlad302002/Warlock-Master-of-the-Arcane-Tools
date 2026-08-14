title Unpacking .binary files to .xml
for /r %%I in (*.binary) do txmlconvert.exe \tUTF8 "%%~fI" "%%~dI%%~pI%%~nI.xml">>"w2extract.txt"

title w2extract Finished!