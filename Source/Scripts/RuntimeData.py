import os
import Logger

class FlagResolver(object):
    def __init__(self, folderpath):
        self._flagsFolder = folderpath

    def Update(self):
        if not os.path.exists(self._flagsFolder):
            msg = "No flags folder found"
            Logger.Crt(msg)
            raise FileExistsError(msg)

        for flag in os.listdir(self._flagsFolder):
            path = os.path.join(self._flagsFolder, flag)
            
            if os.path.isdir(path):
                continue

            flag = flag.upper()
            if flag in self._flagsCallbacks:
                Logger.Inf("{} flag found".format(flag))
                self._flagsCallbacks[flag]()
                path = os.path.join(self._flagsFolder, flag)
                os.remove(path)

    def AddFlag(self, flag, action):
        self._flagsCallbacks[flag] = action

    _flagsCallbacks = {}
    

class RuntimeData:
    def __init__(self):
        realpath = os.path.realpath(__file__)
        self.cwd = os.path.dirname(realpath)

        self.configFolder = self.AssignFolderPath("../../Configurations")
        self.flagsFolder = self.AssignFolderPath("../../Flags")
        self.logFolder = self.AssignFolderPath("../../Logs")

        self.InitializeFlags()

    def AssignFolderPath(self, folderStr):
        path = os.path.realpath(
            os.path.join(self.cwd, folderStr))
        
        if not os.path.exists(path):
            os.makedirs(path)
        return path

    def InitializeFlags(self):
        self._flagResolver = FlagResolver(self.flagsFolder)
        self._flagResolver.AddFlag("TERMINATE", self.TerminateApp)

    def TerminateApp(self):
        self.runningFlag = False

    def UpdateFlags(self):
        self._flagResolver.Update()

    cwd = str()
    configFolder = str()
    logFolder = str()

    runningFlag = True
