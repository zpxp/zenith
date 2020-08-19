#!/usr/bin/python

import sys
import os
import re
from os import path
import semver
import subprocess

project = str(sys.argv[1]).replace("src/", "")
testreg = re.compile(r"test", re.IGNORECASE)
projects = [
    name
    for name in os.listdir("./src")
    if path.isdir(path.join(".", "src", name)) and not testreg.search(name)
]


def commit(file, version):
    process = subprocess.Popen(["git", "add", file], stdout=subprocess.PIPE)
    process.communicate()
    process = subprocess.Popen(["git", "commit", "-m", version], stdout=subprocess.PIPE)
    process.communicate()
    process = subprocess.Popen(["git", "tag", version], stdout=subprocess.PIPE)
    process.communicate()
    process = subprocess.Popen(["git", "push", "--tags", "origin"], stdout=subprocess.PIPE)
    process.communicate()



if project in projects:
    filepath = path.abspath(path.join(".", "src", project, ".version"))
    with open(filepath, "r") as reader:
        version = reader.readline()
    while True:
        opt = input("Action: I̲ncrement Patch; S̲pecify Version;")
        if opt == "i" or opt == "s":
            break
    if opt == "i":
        try:
            ver = semver.VersionInfo.parse(version)
            ver = ver.bump_patch()
            print(project, version, "->", str(ver))
            with open(filepath, "w") as writer:
                writer.write(str(ver))
            commit(filepath, str(ver))
        except:
            pass

    if opt == "s":
        try:
            ver = semver.VersionInfo.parse(input("Enter new semver: "))
            print(project, version, "->", str(ver))
            with open(filepath, "w") as writer:
                writer.write(str(ver))
            commit(filepath, str(ver))
        except:
            pass
else:
    raise Exception("project {} not found".format(project))

