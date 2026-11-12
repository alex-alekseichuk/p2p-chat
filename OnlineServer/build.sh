#!/bin/sh
clear
g++ -o online_server -lexpat -lpq -lssl *.cc expat/expatcc.cc net/*.cc openssl/openssl.cc
