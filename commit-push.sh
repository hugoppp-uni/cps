#!/bin/bash

version=$1

if [[ -n "$version" ]]; then
  docker commit dashboard hugopp/cps-dashboard:$version && /
  docker push hugopp/cps-dashboard:$version && /
  docker push hugopp/cps-dashboard:latest
else
    echo "argument error"
fi
