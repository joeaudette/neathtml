#!/bin/sh
echo "Converting .flatxml to .html via OpenOffice..."
ooffice -invisible 'macro:///SaveAsHtml.Module1.SaveAsHTML("'`pwd`'/Fighting_XSS_with_JavaScript_Judo.flatxml")' || (echo "ERROR: Unable to build Fighting_XSS_with_JavaScript_Judo.html" ; exit 1)
ooffice -invisible 'macro:///SaveAsHtml.Module1.SaveAsHTML("'`pwd`'/Manual.flatxml")' || (echo "ERROR: Unable to build Manual.html" ; exit 1)
