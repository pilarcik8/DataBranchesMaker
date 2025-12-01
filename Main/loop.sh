#!/bin/bash

for i in {0..9}; do
    diff3 -m createdFiles/$i/left$i.xml createdFiles/$i/base$i.xml createdFiles/$i/right$i.xml > createdFiles/$i/diff3Output$i.xml
done

for i in {0..9}; do
    echo "===== out$i.xml ====="
    cat "createdFiles/$i/diff3Output$i.xml"
    echo ""
done
