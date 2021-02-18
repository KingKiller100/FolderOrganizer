from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler

import shutil
import os
import json
import time
import xml.etree.ElementTree as ET

# Impl
cwd = os.getcwd()
configFolder = cwd + "/../Configurations"


class FolderConfig:
    def __init__(self):
        self.ReadConfig()

    def ReadConfig(self):
        file = open(configFolder + "/Paths.ini", "r")
        lines = file.readlines()
        for line in lines:
            split = line.split(":", 1)
            key = split[0]
            path = split[1]
            key = key.replace(" ", "")
            path = path.replace(" ", "")
            path = path.strip()

            if key.lower() == "source":
                self.source = path
            else:
                self.destination = path

    source = str()
    destination = str()


class RedirectConfig:
    def __init__(self):
        self.ReadConfig()

    def ReadConfig(self):
        file = ET.parse(configFolder + "/Redirects.xml")
        root = file.getroot()
        for folderItem in root.findall("Folder"):
            extList = []
            folder = folderItem.attrib["name"]
            for extsItem in folderItem.findall("Extensions"):
                for extItem in extsItem.findall("Extension"):
                    ext = extItem.text
                    extList.append(ext)
            self.redirects[folder] = extList

    redirects = {}


class MyHandler(FileSystemEventHandler):
    def MoveFile(self, src, dest, copyNum):
        try:
            if not os.path.exists(dest):
                shutil.move(src, dest)
                print("New path file: {}".format(dest))
            else:
                split = dest.split('.')
                pathNoExt = split[0]
                ext = str()
                i = 1
                while i < len(split):
                    ext += "." + split[i]
                    i += 1
                pathNoExt.removesuffix("{}".format(copyNum - 1))
                dest = "{}{}{}".format(pathNoExt, copyNum, ext)
                self.MoveFile(src, dest, copyNum + 1)
        except:
            print("Error: unable to move file: " + src)

    def on_modified(self, event):
        for filename in os.listdir(folderConfig.source):
            srcFilepath = folderConfig.source + "/" + filename
            destDirectory = str()

            found = False
            for (path, exts) in redirConfifg.redirects.items():
                for ext in exts:
                    if (filename.lower().endswith(ext)):
                        destDirectory = "{}/{}".format(folderConfig.destination, path)
                        print("Extension match: '{}' ".format(ext))
                        print("Path: '{}'".format(srcFilepath))

                        destPath = destDirectory + "/" + filename
                        self.MoveFile(srcFilepath, destPath, 1)
                        found = True
                        break
                if found:
                    break




# Main
event_handler = MyHandler()

folderConfig = FolderConfig()
redirConfifg = RedirectConfig()

observer = Observer()
observer.schedule(event_handler, folderConfig.source, recursive=True)
observer.start()

try:
    for (path, exts) in redirConfifg.redirects.items():
        dest = "{}/{}".format(folderConfig.destination, path)
        for ext in exts:
            os.makedirs(dest, exist_ok=True)
            print("File redirect: {} -> {}".format(ext, dest))

    print("Entering loop")
    while True:
        time.sleep(10)
    print("Loop terminated")
except:
    observer.stop()

print("Observer joining")
observer.join()
