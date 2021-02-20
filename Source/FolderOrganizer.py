from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler

import logging
import shutil
import os
import time
import numpy as np
import xml.etree.ElementTree as ET

# Impl


class ConfigPair:
    def __init__(self, k: str, v):
        self.key = k.lower()
        self.value = v

    key: str
    value: str


class IniFileParser:
    @staticmethod
    def ReadFile(filename, cb):
        gh = runData.configFolder
        path = os.path.join(runData.configFolder, filename)
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


class RuntimeData:
    def __init__(self):
        realpath = os.path.realpath(__file__)
        self.cwd = os.path.dirname(realpath)
        print("Current working directory: '{}'".format(self.cwd))

        self.configFolder = self.AssignFolderPath("../Configurations")
        self.flagsFolder = self.AssignFolderPath("../Flags")
        self.logFolder = self.AssignFolderPath("../Logs")

    def AssignFolderPath(self, folderStr):
        path = os.path.realpath(
            os.path.join(self.cwd, folderStr))
        print("Registered path: '{}'".format(path))
        if not os.path.exists(path):
            os.makedirs(path)
        return path

    def UpdateFlags(self):
        if not os.path.exists(self.flagsFolder):
            msg = "No flags folder found"
            logger.Crt(msg)
            raise FileExistsError(msg)

        for file in os.listdir(self.flagsFolder):
            if file.upper() == "TERMINATE":
                self.running = False
                path = os.path.join(self.flagsFolder, file)
                os.remove(path)
                logger.Inf("Terminate flag found")

    def InitializeLogging(self, filename):
        path = os.path.join(self.logFolder, filename)
        logging.basicConfig(level=logging.INFO, filename=path)

    cwd = str()
    configFolder = str()
    logFolder = str()
    flagsFolder = str()
    running = True


class Logger:
    def __init__(self, name, filename):
        self.ReadConfig()
        msgFormat = "[%(asctime)s] - [%(levelname)s]: %(message)s"
        path = os.path.join(runData.logFolder, filename)
        logging.basicConfig(filename=path, level=self.level, format=msgFormat)

    def ReadConfigCallBack(self, cfg: ConfigPair):
        if cfg.key == "level":
            self.level = int(cfg.value)

    def ReadConfig(self):
        IniFileParser.ReadFile("Logging.ini", self.ReadConfigCallBack)

    def Dbg(self, msg):
        print(msg)
        logging.debug(msg)

    def Inf(self, msg):
        print(msg)
        logging.info(msg)

    def Wrn(self, msg):
        print(msg)
        logging.warning(msg)

    def Err(self, msg):
        print(msg)
        logging.error(msg)

    def Crt(self, msg):
        print(msg)
        logging.critical(msg)

    level: int


class InternalConfig:
    def __init__(self):
        self.ReadConfig()

    def SetUpdateRate(self, cfg: ConfigPair):
        dayInSecs = 60*60*24  # Max update rate is 1 day(s)
        if int(cfg.value) > 0 and int(cfg.value) <= dayInSecs:
            self.updateRate = int(cfg.value)

    def ReadConfig(self):
        logger.Inf("Internals Configurations")
        IniFileParser.ReadFile("Internals.ini",
                               self.SetUpdateRate)

    def Update(self):
        self.ReadConfig()

    updateRate = 300


class FolderConfig:
    def __init__(self):
        self.ReadConfig()

    def AssignPathCallback(self, config: ConfigPair):
        if not config.key.lower().find("source") == -1:
            self.SetSource(config.value)
        else:
            self.SetDestination(config.value)

    def ReadConfig(self):
        logger.Inf("Folder Configurations")
        self.source = str()
        self.destination = str()
        IniFileParser.ReadFile("Paths.ini",
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
        logger.Inf("File redirect Configurations")
        try:
            elemTree = ET.parse(runData.configFolder + self.settingsFile)
            self.redirects = {}
            root = elemTree.getroot()
            for folderItem in root.findall("Folder"):
                extList = []
                folder = folderItem.attrib["name"]
                logger.Inf("Folder: " + folder)
                for extsItem in folderItem.findall("Extensions"):
                    for extItem in extsItem.findall("Extension"):
                        ext = extItem.text
                        extList.append(ext)
                        logger.Inf("  - Extension: " + ext)
                self.redirects[folder] = extList
        except:
            logger.Err("Problem reading file: " + self.settingsFile)

    def Update(self):
        self.ReadConfig()

    settingsFile = "/Redirects.xml"
    redirects = {}


class DirectoryEventHandler(FileSystemEventHandler):
    def MoveFile(self, src, dest, copyNum):
        try:
            if not os.path.exists(dest):
                shutil.move(src, dest)
                logger.Inf("  - New path file: '{}'".format(dest))
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
            logger.Err("[Error] unable to move file: '{}'\n".format(src))

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
                        destDirectory = os.path.join(
                            folderCfg.destination, path)
                        logger.Inf("Extension match: '{}'".format(ext))
                        logger.Inf("  - Path: '{}'".format(srcFilepath))

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
    logger.Dbg("Entering loop")

    iteration = np.uint64(0)
    while runData.running:
        runData.UpdateFlags()
        logger.Dbg("Run: {}".format(iteration))
        iteration = iteration + 1

        FolderManager.SantizeSubFolders()
        FolderManager.SanitizeDestDir()

        internalCfg.Update()
        redirCfg.Update()
        dirEventHandler.Organize()

        updateTime = internalCfg.updateRate
        logger.Inf("Thread sleep time: {}".format(updateTime))
        time.sleep(2)
    logger.Dbg("Loop terminated")


# Main
runData = RuntimeData()

logger = Logger("App", "Logs.txt")
dirEventHandler = DirectoryEventHandler()
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
    observer.stop()
    observer.join()


main()
