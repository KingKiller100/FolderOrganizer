from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler

import shutil
import os
import logging
import time
import collections
import numpy as np
import xml.etree.ElementTree as et


import ConfigFile
import RuntimeData
import Logger

# Impl
class FolderConfig:
    def __init__(self):
        self.ReadConfig()

    def ReadConfig(self):
        Logger.Inf("Folder Configurations")
        self.source = str()
        self.destination = str()
        path = os.path.join(runData.configFolder, "Paths.ini")
        ConfigFile.IniFileParser.ReadFile(path,
                               self.AssignPathCallback)

    def AssignPathCallback(self, config: ConfigFile.ConfigPair):
        if config.key.lower().find("source") != -1:
            self.SetSource(config.value)
        elif config.key.lower().find("destination") != -1:
            self.SetDestination(config.value)

    def SetSource(self, path):
        self.source = path
        Logger.Inf("Tracking source path: " + self.source)

    def SetDestination(self, path):
        self.destination = path
        Logger.Inf("Destination path: " + self.destination)

    source = str()
    destination = str()


class RedirectConfig:
    def __init__(self):
        self.ReadConfig()

    def ReadConfig(self):
        Logger.Inf("File redirect configurations")
        try:
            path = os.path.join(runData.configFolder, self.settingsFile)
            elemTree = et.parse(path)
            self.redirects = {}
            root = elemTree.getroot()
            for folderItem in root.findall("Folder"):
                extList = []
                folder = folderItem.attrib["name"]
                Logger.Inf("Folder: " + folder)
                for extsItem in folderItem.findall("Extensions"):
                    for extItem in extsItem.findall("Extension"):
                        ext = extItem.text
                        extList.append(ext)
                        Logger.Inf("  - Extension: " + ext)
                self.redirects[folder] = extList
        except:
            Logger.Err("Problem reading file: " + self.settingsFile)

    def Update(self):
        self.ReadConfig()

    settingsFile = "Redirects.xml"
    redirects = {}


class DirectoryEventHandler(FileSystemEventHandler):
    def MoveFile(self, src, dest, copyNum):
        try:
            if not os.path.exists(dest):
                shutil.move(src, dest)
                Logger.Inf("  - New path file: '{}'".format(dest))
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
            Logger.Err("[Error] unable to move file: '{}'\n".format(src))

    def Organize(self):
        if self.handled == True:
            return

        FolderManager.MakeSubDirs()
        self.handled = True

        for filename in os.listdir(folderCfg.source):
            srcFilepath = os.path.join(folderCfg.source, filename)
            if not os.path.isfile(srcFilepath):
                continue

            destDirectory = str()
            for (path, exts) in redirCfg.redirects.items():
                found = False
                for ext in exts:
                    if (filename.lower().endswith(ext)):
                        destDirectory = os.path.join(folderCfg.destination, path)
                        Logger.Inf("Extension match: '{}'".format(ext))
                        Logger.Inf("  - Path: '{}'".format(srcFilepath))

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
            Logger.Dbg("Created directory: '{}'".format(path))


def Run():
    startTime = time.time()
    
    runData._flagResolver.AddFlag("RELOAD", UpdateSettings)
    FlagUpdateLoop()
    
    elapsedTime = time.time() - startTime
    Logger.Inf("Total run time: {} secs".format(elapsedTime))

def FlagUpdateLoop():
    Logger.Bnr("Flag loop")

    while runData.runningFlag:
        runData.UpdateFlags()
        time.sleep(2.5)            
    
    Logger.Bnr("Flag loop terminated")

def UpdateSettings():
    dirEventHandler.handled = True        

    FolderManager.SantizeSubFolders()
    FolderManager.SanitizeDestDir()

    redirCfg.Update()
    dirEventHandler.Organize()


# Main
runData = RuntimeData.RuntimeData()

Logger.InitializeLogger(logging.INFO, os.path.join(runData.logFolder, "Logs.txt"))
dirEventHandler = DirectoryEventHandler()
folderCfg = FolderConfig()
redirCfg = RedirectConfig()

observer = Observer()


def main():
    observer.schedule(dirEventHandler, folderCfg.source, recursive=True)
    observer.start()

    try:
        Run()
    except Exception as e:
        Logger.Inf("Observer stopping")
        observer.stop()
        Logger.Ftl("[Exception] {}".format(e))

    Logger.Inf("Observer joining")
    observer.join()


main()
