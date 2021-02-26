import os
import sys
import logging

def Dbg(msg):
    print(msg)
    logging.debug(msg)

def Inf(msg):
    print(msg)
    logging.info(msg)

def Wrn(msg):
    print(msg)
    logging.warning(msg)

def Err(msg):
    print(msg)
    logging.error(msg)

def Crt(msg):
    print(msg)
    logging.critical(msg)

def Ftl(msg):
    print(msg)
    logging.fatal(msg)

def Bnr(msg):
    msg = "***{}***".format(msg)
    Inf(msg)

def InitializeLogger(level, filepath):
        
    open(filepath, "a").close()
        
    msgFormat = "[%(asctime)s] - [%(levelname)s]: %(message)s"
    logging.basicConfig(filename=filepath, level=level, format=msgFormat)
    
    Dbg("***System paths***")
    for path in sys.path:
        Dbg("  - {}".format(path))
    Dbg("***System paths concluded***")

def Shutdown():
    logging.shutdown()