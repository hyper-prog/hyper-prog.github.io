#!/bin/bash

GITSTATUSOUT=`(cd /etc;git status|grep 'nothing to commit, working tree clean')`
GITDIFFOUT=`(cd /etc;git diff)`

if test "$GITSTATUSOUT" != "nothing to commit, working tree clean" ||
   test "$GITDIFFOUT" != ""
then
    echo -e "\n\n !!!WARNING!!! Uncomitted changes in /etc !!!WARNING!!!"
    echo -e "                     ( Run: eg-commit )\n"
    exit 1
fi
exit 0
