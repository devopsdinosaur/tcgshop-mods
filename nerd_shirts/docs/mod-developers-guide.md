# Nerd Shirts Texture Mod - Developers Guide

## Legal

Before you get started, please note the following:

**This mod is a dependency.**  Do **not** include any of the files from this mod in your own.  All you will need to do is create a sub-folder of the *\<tcgshop-home\>/BepInEx/plugins/nerd_shirts* and make this mod a dependency when you release on Nexus.  (I also plan to add mesh support and an API if there is enough interest).

TL;DR for the rest of this mumbo jumbo.  Just be sure to keep my code as a dependency and don't cut/paste it into your stuff.  You are legally allowed to do exactly that with all of my stuff, since it is covered by the GPL and totally open, but if you do then you absolutely **must** 1) read the rest of this section, 2) make your stuff open source, and 3) adopt the GPL (you do this implicitly once you absorb the code).

With that said, this mod and all of my source code is licensed under the GNU Public License (GPL).  As such, you are free to download, modify, distribute the source code and generated artifacts as long as your end-product also adopts the GPL.  To put it simply, if you use any intellectual product (source or idea) covered by the GPL then your source must become open-source, and you may not release "black box" artifacts (binaries) which contain the intellectual property covered by the GPL.

However, this does **not** apply if you treat the GPL-covered tool/library as a dependency.  Black box and "commercial off the shelf" (COTS) software can legally (and very frequently do) use open-source software covered by the GPL and other open-source licenses.

That's the short(ish) version.  Read the GPL (the LICENSE file in this repo) for full info.

## Quick Unity Primer

A *very* brief overview of Unity objects to explain the terms to the neophytes:
* Pretty much everything visual in Unity is made up of **GameObject**s, which are just a generic thing that has a **Transform** which defines the object's parent and its place/rotation in space.
* GameObjects don't have any functionality on their own.  The real work is done by **Components** which are specialized workhorses that belong to a specific GameObject.
* There are thousands of Components that define how GameObjects are shaped, move, and relate.  But we only need to worry about one (and not even much).
* **MeshRenderer** - the component that represents the *mesh*, or the set of verteces and triangle 'faces' that draw the visual object in 3D space.
* It's pretty simple.  MeshRenderers draw the faces and vertices using any number of **Materials**.  Materials can, in turn, be colors, images, metallic shine, tons of things.
* The meat and power behind materials is in the **Shader** it uses.  The shader defines a set of textures and the values that determine how it behaves.
* That is the (very) basic backbone of how the Unity engine renders a 3D object.

## How this Mod Works

A quick explanation of how this mod makes it a little easier to understand how to make your own texture mods.

Broad strokes:
1. When the game first launches, the mod scans the *\<game\>/BepInEx/plugins/nerd_shirts* folder for all sub-folders, which it will treat as mods (with the exception of those beginning with '_', i.e. special dirs [explained later in the Advanced section]).
1. Each *mod* dir must have the same directory structure as the *__dump__* dir (explained later in the Creating a Mod sections).  The *__dump__* dir is just what it seems: a dump of the tree structure of the Unity objects in the game.
1. If a mod wants to say change the design on the "crop top" shirt for the female customers then the *mod* dir would include the ```Female\Crop_Top_01\Modular\Upper_Body\Crop_Top_01\Crop_Top_01_LOD0\renderer_000\material_000``` directory tree and contain the PNG image files that are modified to change the texture on the shirt.
1. The textures (and shader config JSON files) are loaded at game launch to be used when customer/worker models are instantiated.
1. As soon as customers are spawned (currently workers are not affected by this mod--coming soon) the mod checks the replaced texture tree against the Unity object tree within the model.  All matches of the texture are replaced.
1. This mod will also add materials to renderers (like the decals packaged with the mod) if they are defined under a renderer directory as such: *renderer_000/\_\_add\_\_*.  The properties for these materials are defined in \_\_shader\_\_.json config files.

## Required Tools / Knowledge

(All of these are free)

* **GIMP**: This is probably the only tool you really need if you're just modifying or creating decal PNGs.  I am terrible at anything visual, but GIMP is pretty easy to use for the simple stuff and really good for dealing with PNG layers.
* **Visual Studio Code**: This one is not needed, but it is really nice to have for versatility in navigating the deep directory trees, editing JSON files, and a variety of other personalizable components.
* **Unity**: A cursory knowledge of the workings of Unity will really help.  You definitely don't need to follow the tutorials that show you how to use the editor and all that.  It's more the concept of GameObjects, Meshes, Renderers, Materials, and Shaders that really helps.  It is worth the effort to download and install Unity to just play around with making a GameObject and changing the materials/shaders, etc.

## Creating a Mod - Overview

A general overview of the process of creating a texture mod for this game is as follows:

1. Dump the game textures using this mod.
1. Create a mod sub-folder under the plugins/nerd_shirts directory.
1. Copy one of the dumped directory trees into your mod directory.  Delete all but the textures you wish to modify.
1. Add materials / textures using the \_\_add\_\_ and \_\_shader\_\_.json, if desired.
1. Test out in the game by using the *Force Wear* setting and specifying the modified apparel type(s).
1. Zip up your mod sub-folder and share with the world!

## Creating a Mod - Getting Started

More coming soon...