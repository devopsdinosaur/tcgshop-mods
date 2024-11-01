import os
import sys
import re

THIS_DIR = os.path.realpath(os.path.dirname(__file__))
BASE_DIR = os.path.join(THIS_DIR, "nerd_shirts/files/nerd_shirts/example")
TEMPLATE_PARAMS = {
    'gender': "Female",
    'body_part': "Upper_Body",
    'apparel': "Crop_Top_01",
    'lod_index': 0,
}
f = open(os.path.join(THIS_DIR, "..", "..", "solution_private.targets"), "r")
data = f.read()
f.close()
DUMP_DIR = os.path.join(re.compile("<GamePath>([^<]+)</GamePath>").search(data).group(1), "BepInEx/plugins/nerd_shirts/__dump__")
DIRECTORY_FORMAT = "%(gender)s/%(apparel)s/Modular/%(body_part)s/%(apparel_root)s/%(apparel)s_LOD%(lod_index)s/renderer_000/__add__/decal/choices"

def find_matching_dirs(root_dir, body_part):
    def check_dir(directory, regex, matches):
        match = regex.match(directory.replace("\\", "/"))
        if (match is not None):
            matches.append(match.group(1))
        for file in os.listdir(directory):
            full_path = os.path.join(directory, file)
            if (os.path.isdir(full_path)):
                check_dir(full_path, regex, matches)
        return matches
    root_dir = root_dir.replace("\\", "/")
    return check_dir(    
        root_dir,
        re.compile("^%(root_dir)s/(.*?/%(body_part)s/.*?_LOD\\d+/renderer_000$)" % locals()),
        []
    )

print("\n".join(find_matching_dirs(DUMP_DIR, "Upper_?Body")))
        
        