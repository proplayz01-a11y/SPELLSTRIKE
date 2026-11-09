import bpy,pathlib,math,json,hashlib,shutil,zipfile
from mathutils import Vector,Matrix
OUT=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/ArtSource/Characters/StoneGolem/Mixamo_Package_v001');P=pathlib.Path('C:/Users/David/Downloads/stone golem 3d model (1).glb');src_hash=hashlib.sha256(P.read_bytes()).hexdigest();bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(P));sc=bpy.context.scene;o=next(o for o in sc.objects if o.type=='MESH');assert '79325208-e312-4f26-a11d-91acbd0df204' in o.name
uv_before=[tuple(x.uv) for x in o.data.uv_layers.active.data];o.data.calc_loop_triangles();triangles=len(o.data.loop_triangles);rot=Matrix.Rotation(-math.pi/2,4,'Z');coords=[rot@o.matrix_world@v.co for v in o.data.vertices];lo=Vector([min(v[i] for v in coords) for i in range(3)]);hi=Vector([max(v[i] for v in coords) for i in range(3)]);scale=2/(hi.z-lo.z);offset=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z))
o.parent=None;o.matrix_world=Matrix.Identity(4)
for v,p in zip(o.data.vertices,coords):v.co=(p-offset)*scale
o.data.update();o.name='StoneGolem_Mixamo';o.data.name='StoneGolem_Mixamo_Mesh'
assert uv_before==[tuple(x.uv) for x in o.data.uv_layers.active.data]
texdir=OUT/'Textures';texdir.mkdir(exist_ok=True);tex=[]
for mat in o.data.materials:
 for n in mat.node_tree.nodes:
  if n.type=='TEX_IMAGE' and n.image:
   im=n.image;path=texdir/'StoneGolem_BaseColor.jpg';assert im.packed_file;data=bytes(im.packed_file.data);path.write_bytes(data);im.filepath=str(path);im.pack();tex.append({'name':im.name,'file':str(path),'sha256':hashlib.sha256(data).hexdigest(),'size':list(im.size)})
# Export only the character, no imported helpers or review objects.
bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o;sc.unit_settings.system='METRIC';sc.unit_settings.scale_length=1;sc.frame_start=1;sc.frame_end=1;sc.frame_set(1)
bpy.ops.export_scene.fbx(filepath=str(OUT/'StoneGolem_Mixamo.fbx'),use_selection=True,object_types={'MESH'},bake_anim=False,use_mesh_modifiers=True,axis_forward='-Z',axis_up='Y',global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_NONE',path_mode='COPY',embed_textures=True)
# Native source copy opens framed in a useful front review.
sc.render.engine='BLENDER_WORKBENCH';sc.render.resolution_x=1000;sc.render.resolution_y=1000;sc.render.resolution_percentage=100;sc.world=bpy.data.worlds.new('ReviewWorld');sc.world.color=(.12,.12,.12);sc.display.shading.color_type='TEXTURE';sc.display.shading.light='STUDIO';sc.display.shading.show_cavity=True
bpy.ops.object.camera_add(location=(0,-6,1));cam=bpy.context.object;cam.name='ReviewCamera';cam.rotation_euler=(Vector((0,0,1))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=max(2,(hi.x-lo.x)*scale)*1.12;sc.camera=cam;bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':area.spaces.active.region_3d.view_perspective='CAMERA';area.spaces.active.shading.color_type='TEXTURE';area.spaces.active.overlay.show_overlays=False
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'StoneGolem_Mixamo_Copy.blend'));sc.render.filepath=str(OUT/'StoneGolem_Mixamo_Front.png');bpy.ops.render.render(write_still=True)
# Isolated verification ensures FBX texture data is self-contained.
verify=OUT/'Verification';verify.mkdir(exist_ok=True);shutil.copy2(OUT/'StoneGolem_Mixamo.fbx',verify/'StoneGolem_Mixamo.fbx');bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(verify/'StoneGolem_Mixamo.fbx'),use_image_search=False);meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];points=[];count=0
for ob in meshes:ob.data.calc_loop_triangles();count+=len(ob.data.loop_triangles);points.extend(ob.matrix_world@v.co for v in ob.data.vertices)
imgs=[]
for im in bpy.data.images:
 if im.source!='FILE':continue
 try:_=im.pixels[0]
 except Exception:pass
 imgs.append({'name':im.name,'loaded':im.has_data,'size':list(im.size),'path':bpy.path.abspath(im.filepath)})
qa={'source_file':str(P),'matched_screenshot_object_id':'79325208-e312-4f26-a11d-91acbd0df204','source_sha256':src_hash,'source_unchanged':hashlib.sha256(P.read_bytes()).hexdigest()==src_hash,'blender':bpy.app.version_string,'mesh_count':len(meshes),'triangles':count,'source_triangles':triangles,'height_m':max(p.z for p in points)-min(p.z for p in points),'feet_z':min(p.z for p in points),'rotation_applied_degrees_z':-90,'uniform_scale_from_source':scale,'native_uvs_unchanged':True,'texture':tex,'isolated_reimport_images':imgs,'mixamo_upload_tested':False}
assert len(meshes)==1 and count==triangles and abs(qa['height_m']-2)<1e-5;assert imgs and all(i['loaded'] and pathlib.Path(i['path']).is_relative_to(verify) for i in imgs);assert qa['source_unchanged'];(OUT/'Export_Verification.json').write_text(json.dumps(qa,indent=2));print('MIXAMO_COPY_VERIFIED',qa)
