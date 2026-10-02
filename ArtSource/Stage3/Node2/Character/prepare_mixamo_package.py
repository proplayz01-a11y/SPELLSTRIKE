import bpy,json,hashlib,shutil,zipfile
from pathlib import Path
from mathutils.bvhtree import BVHTree
from mathutils import Vector

BASE=Path(r'C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Stage3\Node2\Character')
SOURCE=BASE/'S3N2_DesertArcher_EmptyQuiver_v001.blend'
OUT=BASE/'Mixamo_Package';TEX=OUT/'Textures';TEX.mkdir(parents=True,exist_ok=True)
before=hashlib.sha256(SOURCE.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(SOURCE),load_ui=False)
body=bpy.data.objects['DesertArcher_Body'];quiver=bpy.data.objects['DesertArcher_Quiver_Hollow']
mouth=bpy.data.objects['Quiver_Opening'].matrix_world.translation.copy();axis=bpy.data.objects['Quiver_Opening'].matrix_world.to_3x3()@Vector((0,0,1))
materials=list(dict.fromkeys(list(body.data.materials)+list(quiver.data.materials)))
images=list(dict.fromkeys(n.image for m in materials if m.use_nodes for n in m.node_tree.nodes if n.type=='TEX_IMAGE' and n.image))
assert len(images)==1, 'Inspect unexpected texture inputs before packaging'
im=images[0];assert im.packed_file
resolution=list(im.size)  # Resolve Blender's lazy packed-image buffer before checking it.
assert resolution==[4096,4096] and im.has_data
texture=TEX/'DesertArcher_BaseColor.jpg';texture.write_bytes(bytes(im.packed_file.data));im.filepath=str(texture)
settings=[]
for m in materials:
    p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    settings.append({'name':m.name,'base_color':list(p.inputs['Base Color'].default_value),'metallic':p.inputs['Metallic'].default_value,'roughness':p.inputs['Roughness'].default_value,'base_color_texture':'Textures/DesertArcher_BaseColor.jpg' if any(n.type=='TEX_IMAGE' for n in m.node_tree.nodes) else None})
(OUT/'Material_Settings.json').write_text(json.dumps({'materials':settings,'texture_resolution':list(im.size),'texture_sha256':hashlib.sha256(texture.read_bytes()).hexdigest(),'normal_roughness_metallic_texture_maps':'Not present in source; scalar material settings only.'},indent=2))
bpy.data.libraries.write(str(OUT/'DesertArcher_Materials.blend'),set(materials),path_remap='RELATIVE',fake_user=True,compress=True)
for o in bpy.context.selected_objects:o.select_set(False)
body.select_set(True);quiver.select_set(True);bpy.context.view_layer.objects.active=body
fbx=OUT/'S3N2_DesertArcher_Mixamo.fbx'
bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,object_types={'MESH'},global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',use_mesh_modifiers=True,mesh_smooth_type='FACE',use_tspace=True,add_leaf_bones=False,bake_anim=False,path_mode='COPY',embed_textures=True)
readme='''# Desert archer — Mixamo files

Upload **S3N2_DesertArcher_Mixamo.fbx** directly to Mixamo. It contains the unrigged character, empty hollow quiver, three materials, and embedded base-color image. No camera, lights, hidden original, rig, or animation is exported.

The ZIP is a delivery bundle: extract it and upload the FBX. The package has not been uploaded to or auto-rigged by Mixamo.

- `Textures/DesertArcher_BaseColor.jpg`: original 4096 x 4096 source atlas, extracted without resizing or repainting.
- `DesertArcher_Materials.blend`: Blender material library. File > Append > this file > Material to reuse the three materials. Its image is packed as well.
- `Material_Settings.json`: exact source material values for recreating assignments if needed.
- Body and quiver exterior share the textured character material. Quiver interior and rim use plain dark/worn leather colors; they do not have separate image textures.
- No separate normal, roughness, or metallic image maps exist in this source; none have been fabricated.

Blender material datablocks are not Unity `.mat` files. FBX carries exportable material information; advanced renderer settings may require reassignment after rigging/import. Preserve this texture and material library as the source for that step.

The editable character source remains separate and unchanged. Do not assume rigging/deformation is validated until the uploaded character has been checked in Mixamo and in the target game.

Adobe's supported formats and embedded-media instructions: https://helpx.adobe.com/creative-cloud/help/mixamo-rigging-animation.html
'''
(OUT/'README.md').write_text(readme,encoding='utf-8')

# Check that the FBX alone supplies its texture, rather than relying on its sibling texture folder.
VERIFY=BASE/'Mixamo_Export_Verification';VERIFY.mkdir(exist_ok=True)
isolated=VERIFY/fbx.name;shutil.copy2(fbx,isolated)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(isolated),use_image_search=False)
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];points=[];entries=[]
for o in meshes:
    o.data.calc_loop_triangles();pts=[o.matrix_world@v.co for v in o.data.vertices];points+=pts
    e={'name':o.name,'triangles':len(o.data.loop_triangles),'materials':[m.name for m in o.data.materials]}
    if 'Quiver' in o.name:
        tree=BVHTree.FromPolygons(pts,[tuple(t.vertices) for t in o.data.loop_triangles],all_triangles=True);hit=tree.ray_cast(mouth+axis*.025,-axis,1)
        e['cavity_depth_m']=hit[3]-.025 if hit[0] is not None else None
    entries.append(e)
imgs=[{'name':i.name,'size':list(i.size),'has_data':i.has_data,'filepath':bpy.path.abspath(i.filepath)} for i in bpy.data.images if i.source=='FILE']
qa={'meshes':entries,'triangles':sum(e['triangles'] for e in entries),'height_m':max(p.z for p in points)-min(p.z for p in points),'images':imgs,'source_blend_unchanged':hashlib.sha256(SOURCE.read_bytes()).hexdigest()==before,'mixamo_upload_tested':False}
assert len(meshes)==2 and qa['triangles']==13409
assert imgs and all(i['has_data'] and i['size']==[4096,4096] for i in imgs)
assert any(e.get('cavity_depth_m',0)>.30 for e in entries)
assert all(Path(i['filepath']).is_relative_to(VERIFY) for i in imgs), 'Importer relied on external source texture'
assert qa['source_blend_unchanged']
bpy.ops.wm.read_factory_settings(use_empty=True)
with bpy.data.libraries.load(str(OUT/'DesertArcher_Materials.blend'),link=False) as (available,loaded):
    loaded.materials=available.materials
qa['material_library_materials']=[m.name for m in loaded.materials]
qa['material_library_images']=[{'name':i.name,'size':list(i.size),'packed':bool(i.packed_file),'has_data':i.has_data} for i in bpy.data.images if i.source=='FILE']
assert len(loaded.materials)==3 and all(i['packed'] and i['has_data'] for i in qa['material_library_images'])
(OUT/'Export_Verification.json').write_text(json.dumps(qa,indent=2))
archive=BASE/'S3N2_DesertArcher_Mixamo_Package.zip'
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
    for name in ['S3N2_DesertArcher_Mixamo.fbx','DesertArcher_Materials.blend','Material_Settings.json','README.md','Export_Verification.json','Textures/DesertArcher_BaseColor.jpg']:
        z.write(OUT/name,arcname=name)
print('MIXAMO_PACKAGE_VERIFIED '+json.dumps(qa),flush=True)
print('ZIP '+str(archive),flush=True)
