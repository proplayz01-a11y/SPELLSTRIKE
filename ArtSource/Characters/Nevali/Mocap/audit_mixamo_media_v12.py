import bpy
from pathlib import Path
root=Path.cwd()
src=root/'ArtSource/Characters/Nevali/Working/Nevali_Face_Reconstruction_v12.glb'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(src))
print("MEDIA_IMAGES",len(bpy.data.images))
for im in bpy.data.images:
    print("MEDIA_IMAGE",repr(im.name),"source",im.source,"filepath",repr(im.filepath),
          "packed",len(im.packed_files),"pixels",tuple(im.size),"has_data",im.has_data,
          "is_dirty",im.is_dirty)
for m in bpy.data.materials:
    if m.use_nodes:
        ns=[(n.name,n.type,
             (repr(n.image.name) if getattr(n,"image",None) else None))
            for n in m.node_tree.nodes if n.type=='TEX_IMAGE']
        print("MEDIA_MATERIAL",m.name,ns)
