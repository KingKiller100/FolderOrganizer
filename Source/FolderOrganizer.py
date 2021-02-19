from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler

# import logging
import shutil
import os
import time
import numpy as np
import xml.etree.ElementTree as ET

# Impl


class MyScriptData:
    def __init__(self):
        realpath = os.path.realpath(__file__)
        self.cwd = os.path.dirname(realpath)
        self.configFolder = os.path.realpath(
            os.path.join(self.cwd, "../Configurations"))
        # logging.Logger.info("Current working directory: %s", self.cwd)
        print("Current working directory: '{}'".format(self.cwd))
        print("Configuration path: '{}'".format(self.configFolder))

    cwd: str
    configFolder: str


class ConfigPair:
    def __init__(self, k: str, v):
        self.key = k.lower()
        self.value = v

    key: str
    value: str


class ConfigFileReader:
    @staticmethod
    def ReadFile(filename, cb):
        gh = myScript.configFolder
        path = os.path.join(myScript.configFolder, filename)
        print("Reading configurations: '{}'".format(path))
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
        self.updateRate = 300
        self.ReadConfig()

    def SetUpdateRate(self, cfg: ConfigPair):
        if int(cfg.value) > 0:
            self.updateRate = int(cfg.value)

    def ReadConfig(self):
        print("Internals Configurations")
        ConfigFileReader.ReadFile("Internals.ini",
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
        ConfigFileReader.ReadFile("Paths.ini",
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
            elemTree = ET.parse(myScript.configFolder + self.settingsFile)
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


class DirectoryEventHandler(FileSystemEventHandler):
    def MoveFile(self, src, dest, copyNum):
        try:
            if not os.path.exists(dest):
                shutil.move(src, dest)
                print("  - New path file: '{}'".format(dest))
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
            print("[Error] unable to move file: '{}'\n".format(src))

    def Organize(self):
        if self.handled == True:
            return

        FolderManager.MakeSubDirs()
        self.handled = True
        for filename in os.listdir(folderCfg.source):
            srcFilepath = os.path.join(folderCfg.source, filename)
            if not os.path.isfile(srcFilepath):
                continue

            found = False
            destDirectory = str()
            for (path, exts) in redirCfg.redirects.items():
                for ext in exts:
                    if (filename.lower().endswith(ext)):
                        destDirectory = os.path.join(
                            folderCfg.destination, path)
                        print("Extension match: '{}'".format(ext))
                        print("  - Path: '{}'".format(srcFilepath))

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
        fdrs = os.listdir(folderCfg.destination)
        for fdr in fdrs:
            path = os.path.join(folderCfg.destination, fdr)
            if not os.path.isdir(path):
                continue

            deleteFolder = True
            for f in redirCfg.redirects.keys():
                if (fdr.endswith(f)):
                    deleteFolder = False
                    break

            if deleteFolder:
                for file in os.listdir(path):
                    filepath = os.path.join(path, file)
                    shutil.move(filepath, folderCfg.destination)

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
                moveFile = True
                for ext in exts:
                    if (file.endswith(ext)):
                        moveFile = False
                        break
                if (moveFile):
                    filepath = os.path.join(path, file)
                    shutil.move(filepath, folderCfg.source)

    @staticmethod
    def MakeSubDirs():
        for (fdr, exts) in redirCfg.redirects.items():
            path = os.path.join(folderCfg.destination, fdr)

            if os.path.exists(path):
                continue

            os.makedirs(path)
            print("Created directory: '{}'".format(path))


def Run():
    print("Entering loop")

    iteration = np.uint64(0)
    while True:
        print("Run: {}".format(iteration))
        ++iteration
        FolderManager.SantizeSubFolders()
        FolderManager.SanitizeDestDir()

        internalCfg.Update()
        redirCfg.Update()
        dirEventHandler.Organize()
        time.sleep(int(internalCfg.updateRate))
    print("Loop terminated")


# Main
dirEventHandler = DirectoryEventHandler()

myScript = MyScriptData()
internalCfg = InternalConfig()
folderCfg = FolderConfig()
redirCfg = RedirectConfig()

observer = Observer()

def main():
    observer.schedule(dirEventHandler, folderCfg.source, recursive=True)
    observer.start()

    try:
        Run()
    except Exception as e:
        print(e)
        observer.stop()

    print("Observer joining")
    observer.join()


main()
