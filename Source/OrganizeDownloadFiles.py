from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler

import shutil
import os
import json
import time
import xml.etree.ElementTree as ET

# Impl
class FolderConfig:
    def __init__(self):
        self.ReadConfig()

    def ReadConfig(self):
        cwd = os.getcwd()
        file = open(cwd + "/Paths.ini", "r")
        lines = file.readlines();
        for line in lines:
            line.replace(" ", "")
            split = line.split(":", 1)
            key = split[0]
            path = split[1]

            if key.lower() == "source":
                source = path
            else:
                destination = path

    source = str()
    destination = str()

class RedirectConfig:
    redirects = dict()
            
    def __init__(self):
        self.ReadConfig()

    def ReadConfig(self):
        cwd = os.getcwd()
        file = ET.parse(cwd + "/Redirect.xml")
        root = file.getroot()
        extList = list()
        for folderItem in root.findall("Folder"):
            folder = folderItem.attrib["name"]
            for extItem in folderItem.findall("Extensions"):
                ext = extItem.text()
                extList.append(ext)
            redirects[folder] = extList
        

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
        for filename in os.listdir(FolderConfig.source):
            srcFilepath = FolderConfig.source + "/" + filename
            destDirectory = str()

            for (path, exts) in destFolders.items():
                for ext in exts:
                    if (filename.lower().endswith(ext)):
                        destDirectory = path
                        print("Extension match: '{}' ".format(ext))
                        print("Path: '{}'".format(srcFilepath))

            if (len(destDirectory) > 0):
                destPath = destDirectory + "/" + filename
                self.MoveFile(srcFilepath, destPath, 1)

# Main
event_handler = MyHandler()

folderConfig = FolderConfig()
redirectConfig = RedirectConfig()

observer = Observer()
observer.schedule(event_handler, FolderConfig.source, recursive=True)
observer.start()

try:
    for (path, exts) in redirectConfig.items():
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
