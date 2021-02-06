#!/bin/bash
echo "Creating /etc/etc-git/ directory"
mkdir /etc/etc-git
curl http://hyperprog.com/howto/aptpreinstall.bash -o /etc/etc-git/aptpreinstall.bash
curl http://hyperprog.com/howto/aptpostinstall.bash -o /etc/etc-git/aptpostinstall.bash
chmod +x /etc/etc-git/*.bash

echo "Set up APT hooks"
echo "DPkg::Pre-Invoke { '/etc/etc-git/aptpreinstall.bash' };" \
  >> /etc/apt/apt.conf.d/90etc-git
echo "DPkg::Post-Invoke { '/etc/etc-git/aptpostinstall.bash' };" \
  >> /etc/apt/apt.conf.d/90etc-git

echo "Creating scripts in /usr/local/bin"
curl http://hyperprog.com/howto/eg-save -o /usr/local/bin/eg-save
curl http://hyperprog.com/howto/eg-restore -o /usr/local/bin/eg-restore
curl http://hyperprog.com/howto/eg-check -o /usr/local/bin/eg-check
curl http://hyperprog.com/howto/eg-commit -o /usr/local/bin/eg-commit
chmod +x /usr/local/bin/eg-*

echo "Initialising the git repository"
cd /etc
git init
echo "System ETC stored in GIT" > /etc/.git/description
# git config --global user.name "Your Name"        # Fill this values
# git config --global user.email you@example.com   # Fill this values

echo "Set up hook in git of etc"
curl http://hyperprog.com/howto/pre-commit -o /etc/.git/hooks/pre-commit
chmod +x /etc/.git/hooks/pre-commit

echo "Save the current git content as initial state"
eg-save
git add --all
eg-commit "Initial state"
