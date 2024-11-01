#!/bin/env python3

# -- NOTE --
# This file was created for my personal use to copy template decal configs and GIMP
# projects to all of the Upper Body apparel slots.  It is *very* rough and not
# designed to be used by anyone but me.  As such, modify and use at your own risk.
# It won't destroy anything, but it might make a mess in the OUT_DIR =)

import os
import sys
import re
import shutil

THIS_DIR = os.path.realpath(os.path.dirname(__file__))
BASE_DIR = os.path.realpath(os.path.join(THIS_DIR, "../files/nerd_shirts/example"))
TEMPLATE_PARAMS = {
    'gender': "Female",
    'body_part': "Upper_Body",
    'apparel': "Crop_Top_01",
    'apparel_root': "Crop_Top_01",
    'lod_index': 0,
}
f = open(os.path.join(THIS_DIR, "..", "..", "solution_private.targets"), "r")
data = f.read()
f.close()
DUMP_DIR = os.path.join(re.compile("<GamePath>([^<]+)</GamePath>").search(data).group(1), "BepInEx/plugins/nerd_shirts/__dump__")
PACKED_SOURCE_OFFSET_FORMAT = "material_00%d/_Normal.png"
PROJECT_ROOT_OFFSET = "__add__/decal"
CHOICES_OFFSET = os.path.join(PROJECT_ROOT_OFFSET, "choices")
PACKED_DEST_OFFSET = os.path.join(CHOICES_OFFSET, "_Normal_delete_after_adding_to_xcf_as_layer.png")
CONFIG_FILE = "__shader__.json"
CONFIG_LINK = "__shader__.txt"
PROJECT_FILE = "Decal.xcf"
CONFIG_FILE_OFFSET = os.path.join(PROJECT_ROOT_OFFSET, CONFIG_FILE)
CONFIG_LINK_OFFSET = os.path.join(PROJECT_ROOT_OFFSET, CONFIG_LINK)
PROJECT_FILE_OFFSET = os.path.join(CHOICES_OFFSET, PROJECT_FILE)

OUT_DIR = os.path.realpath(os.path.join(THIS_DIR, "../tmp/decal_files"))

def log(text):
    sys.stdout.write(text + "\n")
    sys.stdout.flush()

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

def copy_file_if_not_present(template, key, dest):
    if (os.path.exists(dest['gold'][key]) or os.path.exists(dest[key])):
        return
    os.makedirs(os.path.dirname(dest[key]), exist_ok = True)
    if (key == 'config_link'):
        f = open(dest[key], "w")
        f.write(re.sub("_LOD\\d+", "_LOD0", dest['texture_keys']['config']))
        f.close()
        return
    if (not os.path.exists(template[key])):
        log("* warning: '%s' does not exist." % template[key])
        return
    shutil.copyfile(template[key], dest[key])

def main(argv):
    template_root = None
    dirs = find_matching_dirs(DUMP_DIR, "Upper_?Body")
    _template_dir = "%(gender)s/%(apparel)s/Modular/%(body_part)s/%(apparel_root)s/%(apparel)s_LOD%(lod_index)s/renderer_000" % TEMPLATE_PARAMS
    for directory in dirs:
        if (directory == _template_dir):
            template_root = directory
            break
    template = {}
    for key, offset in (('root', ""), ('config', CONFIG_FILE_OFFSET), ('project', PROJECT_FILE_OFFSET)):
        template[key] = os.path.join(BASE_DIR, template_root, offset)
        if (not os.path.exists(template[key])):
            log("** ERROR - template '%s' (%s) does not exist." % (key, template[key]))
            return 1
    for directory in dirs:
        if (directory == template_root):
            continue
        dest = {'texture_keys': {}, 'gold': {}}
        for key, offset in (
            ('root', ""), 
            ('config', CONFIG_FILE_OFFSET), 
            ('project', PROJECT_FILE_OFFSET), 
            ('config_link', CONFIG_LINK_OFFSET),
            ('packed', PACKED_DEST_OFFSET)
        ):
            dest['texture_keys'][key] = os.path.splitext((os.path.join(directory, offset) if (offset) else directory).replace("\\", "/"))[0]
            dest['gold'][key] = os.path.join(BASE_DIR, directory, offset)
            dest[key] = os.path.join(OUT_DIR, directory, offset)
        lod_index = int(directory.split("/")[-2][-1])
        if (lod_index == 0):
            copy_file_if_not_present(template, 'config', dest)
            copy_file_if_not_present(template, 'project', dest)
            for counter in range(9):
                template['packed'] = os.path.join(DUMP_DIR, directory, PACKED_SOURCE_OFFSET_FORMAT % counter)
                if (os.path.exists(template['packed'])):
                    copy_file_if_not_present(template, 'packed', dest)
                    break
        else:
            copy_file_if_not_present(template, 'config_link', dest)
    return 0

if (__name__ == "__main__"):
    sys.exit(main(sys.argv))

