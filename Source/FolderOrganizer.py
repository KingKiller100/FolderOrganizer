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


class ConfigPair:
    def __init__(self, k: str, v):
        self.key = k.lower()
        self.value = v

    key: str
    value: str

class ConfigFileReader:
    @staticmethod
    def Get(path, cb):
        print("Reading configurations: " + path)
        file = open(path, "r")
        lines = file.readlines()
        for line in lines:
            line = line.split("*", 1)[0]
            line = line.strip()
            if line == "":
                continue

            split = line.split(":", 1)
            key = split[0]
            value = split[1]
            key = key.replace(" ", "")
            value = value.replace(" ", "")
            value = value.strip()
            config = ConfigPair(key.lower(), value)
            cb(config)
        file.close()

class InternalConfig:
    def __init__(self):
        self.ReadConfig()

    def SetUpdateRate(self, cfg: ConfigPair):
        self.updateRate = int(cfg.value)

    def ReadConfig(self):
        print("Internals Configurations")
        ConfigFileReader.Get(configFolder + "/Internals.ini",
                             self.SetUpdateRate)

    def Update(self):
        self.ReadConfig()

    updateRate: int


class FolderConfig:
    def __init__(self):
        self.ReadConfig()

    def AssignPathCallback(self, config: ConfigPair):
        if not config.key.lower().find("source") == -1:
            self.SetSource(config.value)
        else:
            self.SetDestination(config.value)

    def ReadConfig(self):
        print("Folder Configurations")
        self.source = str()
        self.destination = str()
        ConfigFileReader.Get(configFolder + "/Paths.ini",
                             self.AssignPathCallback)

    def SetSource(self, path):
        self.source = path
        print("Tracking source path: " + self.source)

    def SetDestination(self, path):
        self.destination = path
        print("Destination path: " + self.destination)

    source = str()
    destination = str()


class RedirectConfig:
    def __init__(self):
        self.ReadConfig()

    def ReadConfig(self):
        print("File redirect Configurations")
        try:
            elemTree = ET.parse(configFolder + self.settingsFile)
            self.redirects = {}
            root = elemTree.getroot()
            for folderItem in root.findall("Folder"):
                extList = []
                folder = folderItem.attrib["name"]
                print("Folder: " + folder)
                for extsItem in folderItem.findall("Extensions"):
                    for extItem in extsItem.findall("Extension"):
                        ext = extItem.text
                        extList.append(ext)
                        print("  - Extension: " + ext)
                self.redirects[folder] = extList
        except:
            print("Problem reading file: " + self.settingsFile)

    def Update(self):
        self.ReadConfig()

    settingsFile = "/Redirects.xml"
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
            print("[Error] unable to move file: " + src)

    def on_modified(self, event):
        for filename in os.listdir(folderCfg.source):
            srcFilepath = folderCfg.source + "/" + filename
            destDirectory = str()

            found = False
            for (path, exts) in redirCfg.redirects.items():
                for ext in exts:
                    if (filename.lower().endswith(ext)):
                        destDirectory = "{}/{}".format(
                            folderCfg.destination, path)
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

internalCfg = InternalConfig()
folderCfg = FolderConfig()
redirCfg = RedirectConfig()

observer = Observer()
observer.schedule(event_handler, folderCfg.source, recursive=True)
observer.start()

try:
    print("Entering loop")

    while True:
        time.sleep(int(internalCfg.updateRate))
        redirCfg.Update()
        internalCfg.Update()
    print("Loop terminated")
except:
    observer.stop()

print("Observer joining")
observer.join()
