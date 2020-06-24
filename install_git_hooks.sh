#!/bin/bash

# "installer"
install()
{
   SOURCE_DIR=.githooks
   DEST_DIR=.git/hooks
   cp "${SOURCE_DIR}/pre-commit" "${DEST_DIR}/pre-commit" 
   chmod +x "${DEST_DIR}/pre-commit" 
   exit
}

# if no hook present, then it is safe to copy over the hook from repository
[[ ! -f "${DEST_DIR}/pre-commit" ]] && install

# if file exists locally already, ask the user if he wants to apply the update 
echo "Pre-existing pre-commit git hook found in .git/hooks. Do you want to replace 
with updated hook from the remote repository?"
select yn in "Yes" "No" "Diff"; do
   case $yn in
       Yes ) install; break;;
       No ) exit;;
      Diff ) diff $SOURCE_DIR/pre-commit $DEST_DIR/pre-commit; exit;;
   esac
done
