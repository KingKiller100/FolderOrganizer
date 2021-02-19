from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler

import shutil
import os
import json
import time
import numpy
import xml.etree.ElementTree as ET


# Impl
cwd = os.getcwd()
configFolder = os.path.join(cwd, "../Configurations")


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
            key = key.replace(" ", "")
            key = key.lower()

            value = split[1]
            value = value.replace(" ", "")
            value = value.strip()
            config = ConfigPair(key, value)
            print("  - [{}, {}]".format(key, value))
            cb(config)
        file.close()


class InternalConfig:
    def __init__(self):
        self.ReadConfig()

    def SetUpdateRate(self, cfg: ConfigPair):
        self.updateRate = int(cfg.value)

    def ReadConfig(self):
        print("Internals Configurations")
        ConfigFileReader.Get(os.path.join(configFolder, "Internals.ini"),
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
        ConfigFileReader.Get(os.path.join(configFolder, "Paths.ini"),
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

    def Organize(self):
        self.handled = True
        for filename in os.listdir(folderCfg.source):
            srcFilepath = os.path.join(folderCfg.source, filename)
            if not os.path.isfile(srcFilepath):
                continue

            destDirectory = str()

            found = False
            for (path, exts) in redirCfg.redirects.items():
                for ext in exts:
                    if (filename.lower().endswith(ext)):
                        destDirectory = os.path.join(
                            folderCfg.destination, path)
                        print("Extension match: '{}' ".format(ext))
                        print("Path: '{}'".format(srcFilepath))

                        destPath = os.path.join(destDirectory, filename)
                        self.MoveFile(srcFilepath, destPath, 1)
                        found = True
                        break
                if found:
                    break
        self.handled = False

    def on_modified(self, event):
        if not self.handled:
            self.Organize()

    handled = False

class FolderManager:
    @staticmethod
    def SanitizeDestDir():
        for fdr in os.listdir(folderCfg.destination):
            path = os.path.join(folderCfg.destination, fdr)
            if not os.path.isdir(path):
                continue

            toDelete = True
            for f in redirCfg.redirects.keys():
                if (fdr.endswith(f)):
                    toDelete = False
                    break

            if toDelete:
                os.rmdir(path)

    @staticmethod
    def SantizeSubFolders():
        for (fdr, exts) in redirCfg.redirects.items():
            path = os.path.join(folderCfg.destination, fdr)

            if not os.path.exists(path):
                continue
            if not os.path.isdir(path):
                continue

            for file in os.listdir(path):
                match = False
                for ext in exts:
                    if (file.endswith(ext)):
                        match = True
                        break
                if not (match):
                    filepath = "{}/{}".format(path, file)
                    shutil.move(filepath, folderCfg.source)

    @staticmethod
    def MakeSubDirs():
        for (fdr, exts) in redirCfg.redirects.items():
            path = os.path.join(folderCfg.destination, fdr)

            if os.path.exists(path):
                continue

            os.makedirs(path)
            print("Created directory: " + path)

def Run():
    print("Entering loop")
    
    iteration = 0
    while True:
        ++iteration
        FolderManager.SantizeSubFolders()
        FolderManager.SanitizeDestDir()
        FolderManager.MakeSubDirs()

        internalCfg.Update()
        redirCfg.Update()
        event_handler.Organize()
        time.sleep(int(internalCfg.updateRate))
        print("Run {}".format(iteration))
    print("Loop terminated")


# Main
event_handler = MyHandler()

internalCfg = InternalConfig()
folderCfg = FolderConfig()
redirCfg = RedirectConfig()

observer = Observer()
observer.schedule(event_handler, folderCfg.source, recursive=True)
observer.start()

try:
    Run()
except Exception as e:
    print(e)
    observer.stop()

print("Observer joining")
observer.join()
