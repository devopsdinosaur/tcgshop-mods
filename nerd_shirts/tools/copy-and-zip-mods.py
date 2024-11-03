#!/bin/env python3

import os
import sys
import re
import shutil

THIS_DIR = os.path.realpath(os.path.dirname(__file__))
BASE_DIR = os.path.realpath(os.path.join(THIS_DIR, ".."))
MODS_DIR = os.path.join(BASE_DIR, "files/nerd_shirts/mods")
f = open(os.path.join(THIS_DIR, "..", "..", "solution_private.targets"), "r")
data = f.read()
f.close()
DEST_DIR = os.path.join(re.compile("<GamePath>([^<]+)</GamePath>").search(data).group(1), "BepInEx/plugins/nerd_shirts")
BIN_DIR = os.path.join(BASE_DIR, "bin")
RX_MOD_KEYED_FILE = re.compile("^__mod_(.*?)__(.*?)$")
VALID_EXTENSIONS = (".json", ".txt", ".png", "")

def log(text):
    sys.stdout.write(text + "\n")
    sys.stdout.flush()

def copy_and_zip(name, mod_dir):
    dest_dir = os.path.join(DEST_DIR, name)
    
    return 0

def add_path_to_file_lists(directory):
    
    def __add_path_to_file_lists__(src_parts, dst_parts, file, current_key, file_lists):
        
        def parts_to_path(parts, plus_part):
            return os.path.join("/".join(parts), plus_part).replace("\\", "/").replace("//", "/")
        
        if (os.path.splitext(file)[1] not in VALID_EXTENSIONS):
            return file_lists
        full_path = parts_to_path(src_parts, file)
        match = RX_MOD_KEYED_FILE.match(file)
        if (match is not None):
            extracted_key = match.group(1)
            extracted_file = match.group(2)
        else:
            extracted_key = current_key
            extracted_file = file
        if (os.path.isdir(full_path)):
            src_parts.append(file)
            if (extracted_file):
                dst_parts.append(extracted_file)
            for check_file in os.listdir(full_path):
                file_lists = __add_path_to_file_lists__(src_parts, dst_parts, check_file, extracted_key, file_lists)
            if (extracted_file):
                dst_parts.pop(-1)
            src_parts.pop(-1)
        else:
            if (extracted_key not in file_lists.keys()):
                file_lists[extracted_key] = list()
            file_lists[extracted_key].append({'src': full_path, 'dst': parts_to_path(dst_parts, extracted_file)})
        return file_lists
    
    src_directory = list()
    src_directory.append(directory)
    return __add_path_to_file_lists__(src_directory, list(), "", '__all__', dict())

def main(argv):
    file_lists = add_path_to_file_lists(MODS_DIR)
    for name, files in file_lists.items():
        if (name == "__all__"):
            continue
        dest_root = os.path.join(DEST_DIR, name)
        shutil.rmtree(dest_root, ignore_errors = True)
        complete_files = list(file_lists['__all__'])
        complete_files.extend(files)
        for file in complete_files:
            dst_path = os.path.join(dest_root, file['dst'])
            os.makedirs(os.path.dirname(dst_path), exist_ok = True)
            shutil.copyfile(file['src'], dst_path)
    return 0

if (__name__ == "__main__"):
    sys.exit(main(sys.argv))