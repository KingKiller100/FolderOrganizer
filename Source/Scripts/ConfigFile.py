import Logger    

class ConfigPair:
    def __init__(self, k: str, v):
        self.key = k.lower()
        self.value = v

    key: str
    value: str


class IniFileParser:
    @staticmethod
    def ReadFile(path, cb):
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
            Logger.Inf("  - [{}, {}]".format(key, value))
            cb(config)
        file.close()
