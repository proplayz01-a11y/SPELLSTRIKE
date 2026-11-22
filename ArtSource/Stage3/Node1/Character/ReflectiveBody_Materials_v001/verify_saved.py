import bpy,pathlib,hashlib,json,numpy as np
OUT=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/ArtSource/Stage3/Node1/Character/ReflectiveBody_Materials_v001');SOURCE=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/ArtSource/Stage3/Node1/Character/TargetedRework_v001/SunboundWarden_TargetedRework_FINAL.blend');P=OUT/'SunboundWarden_ReflectiveBody_MaterialOnly_v002.blend';bpy.ops.wm.open_mainfile(filepath=str(P));sc=bpy.context.scene;qa=json.loads((OUT/'verification_v002.json').read_text());signatures={}
for o in sc.objects:
 if o.type not in {'MESH','CURVE','ARMATURE'}:continue
 h=hashlib.sha256();h.update(np.array(o.matrix_world,dtype=np.float32).tobytes())
 if o.type=='MESH':
  p=np.empty(len(o.data.vertices)*3,dtype=np.float32);o.data.vertices.foreach_get('co',p);h.update(p.tobytes());h.update(json.dumps([list(f.vertices) for f in o.data.polygons]).encode())
  for uv in o.data.uv_layers:a=np.empty(len(uv.data)*2,dtype=np.float32);uv.data.foreach_get('uv',a);h.update(a.tobytes())
 elif o.type=='CURVE':h.update(json.dumps([{'points':[list(p.co) for p in s.points],'bezier':[list(p.co) for p in s.bezier_points]} for s in o.data.splines]).encode())
 signatures[o.name]=h.hexdigest()
assert signatures==qa['character_geometry'];actual={im.name:hashlib.sha256(bytes(im.packed_file.data)).hexdigest() for im in bpy.data.images if im.packed_file};assert actual==qa['packed_texture_hashes'];assert hashlib.sha256(SOURCE.read_bytes()).hexdigest()==qa['source_sha256'];qa['saved_v002_geometry_uv_transform_verified']=True;qa['saved_v002_source_texture_bytes_verified']=True
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':area.spaces.active.shading.type='MATERIAL';area.spaces.active.shading.use_scene_lights=True;area.spaces.active.shading.use_scene_world=True
sc.frame_set(31);bpy.ops.wm.save_as_mainfile(filepath=str(P));(OUT/'verification_v002.json').write_text(json.dumps(qa,indent=2));print('SAVED_FILE_GEOMETRY_TEXTURE_PASS')
