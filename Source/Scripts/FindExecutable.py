import os
import signal
import sys
import RuntimeData
import Logger

outputFile = "PyExeDir.ini"

if __name__ == '__main__':
    runData = RuntimeData.RuntimeData()
    pyExeKey = "PythonExe"
    pyExe = os.path.dirname(sys.executable)

    filepath = os.path.join(runData.configFolder, outputFile)
    file = open(filepath, "w")

    file.write("{}: {}".format(pyExeKey, pyExe))

    file.close()

    

