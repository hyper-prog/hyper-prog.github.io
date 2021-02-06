#!/bin/bash

GITSTATUSOUT=`(cd /etc;git status|grep 'nothing to commit, working tree clean')`
GITDIFFOUT=`(cd /etc;git diff)`
DATE=`date`

if test "$GITSTATUSOUT" != "nothing to commit, working tree clean" ||
   test "$GITDIFFOUT" != ""
then
	echo -e "\n===== /etc changed need git commit ====="
	(
		cd /etc/
		/usr/local/bin/eg-save
		git add --all
		git commit . -m "Automatic commit after apt ($DATE)"
	)
	echo -e "\n===== Comitted ====="
	exit 0
fi
exit 0
