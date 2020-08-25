#!/usr/bin/python

import sys
import os
import re
from os import path
import subprocess


testreg = re.compile(r"test", re.IGNORECASE)
projects = [
    name
    for name in os.listdir("./src")
    if path.isdir(path.join(".", "src", name)) and not testreg.search(name)
]


for p in projects:
    if path.exists(path.join(".", "src", p, ".version")):
        with open(path.abspath(path.join(".", "src", p, ".version")), "r") as reader:
            version = reader.readline()
        os.environ[p.replace(".", "_") + "_PACKAGE_VERSION"] = version


process = subprocess.Popen(["dotnet", "build", "-c", "Release"], stdout=subprocess.PIPE, stderr=subprocess.PIPE)
out, err = process.communicate()
print(out.decode())
if err:
    raise Exception(err)


process = subprocess.Popen(["dotnet", "test"], stdout=subprocess.PIPE, stderr=subprocess.PIPE)
out, err = process.communicate()
print(out.decode())
if err:
    raise Exception(err)


try:
    process = subprocess.Popen(
        [
            "dotnet",
            "nuget",
            "push",
            "*/**/Release/*.nupkg",
            "--skip-duplicate",
            "-k",
            os.environ["NUGET_KEY"],
            "-s",
            "https://api.nuget.org/v3/index.json",
        ],
        stdout=subprocess.PIPE,
    )
    print(process.communicate())
except:
    pass
