#!/bin/sh
# Converts NeatHtml.dtd, htmlentities.dtd, and propidx.html into javascript
# fragments allelements.txt, allattrs.txt, allentities.txt, and allstyles.txt.
# NOTE: This is quick and dirty and has only been run on Fedora Core 6 Linux.
grep ELEMENT NeatHtml.dtd | cut -d" " -f2 | sort | uniq | awk '{print "\"" $0 "\","}' > allelements.txt
sed -n -r -e '/^<!(ATTLIST|ENTITY)[^>]*$/,/>/ p' NeatHtml.dtd | sed -n -r -e  '/^ [" ][A-Z:a-z]+ / p' | cut -c3- | cut -d" " -f1 | sort | uniq | awk '{print "\"" $1 "\","}' > allattrs.txt
sed -n -r -e '/^<!ENTITY/ s/^[^ ]* *([^ ]*)[^#]*#([^;]*);.*/\1:\2,/ p' < htmlentities.dtd > allentities.txt
sed -n -r -e '/^<!ENTITY/ s/^[^ ]* *([^ ]*)[^#]*#([^;]*);.*/\1:\2,/ p' < propidx.html > allstyles.txt
