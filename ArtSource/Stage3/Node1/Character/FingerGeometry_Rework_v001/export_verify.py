import bpy,bmesh,pathlib,json,hashlib,collections
OUT=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/ArtSource/Stage3/Node1/Character/FingerGeometry_Rework_v001');bpy.ops.wm.open_mainfile(filepath=str(OUT/'SunboundWarden_RebuiltFingers_v005.blend'));o=bpy.data.objects['SunboundWarden_Original_RebuiltFingers'];sc=bpy.context.scene;me=o.data
bm=bmesh.new();bm.from_mesh(me);hand_boundaries=[e for e in bm.edges if e.is_boundary and all(abs(v.co.x-.001069)>.79 and 1.22<v.co.z<1.36 for v in e.verts)];assert len(hand_boundaries)==0,len(hand_boundaries);bm.free()
# Portable copies preserve original body maps and new per-pixel transferred finger maps.
texdir=OUT/'Textures';texdir.mkdir(exist_ok=True);images={}
for mat in me.materials:
 for node in mat.node_tree.nodes:
  if node.type=='TEX_IMAGE' and node.image:
   im=node.image
   if im.name in images:continue
   path=texdir/(im.name if im.name.lower().endswith('.png') else im.name+'.png')
   if im.packed_file:path.write_bytes(bytes(im.packed_file.data))
   else:im.filepath_raw=str(path);im.file_format='PNG';im.save()
   im.filepath=str(path)
   try:_=im.pixels[0]
   except Exception:pass
   images[im.name]={'size':list(im.size),'has_data':im.has_data,'sha256':hashlib.sha256(path.read_bytes()).hexdigest()}
notes=bpy.data.texts.new('README_Rebuilt_Fingers');notes.write('Meaningful original-character finger rework, 2026-10-05. Distal fingers replaced with connected quad joint loops and short rounded caps; thumb rebuilt with two-segment/opposed shape. Original body/cuff/palm are the reference; local finger-web joins refined. Original body material and maps remain intact. Finger appearance is transferred into local maps sampled per pixel from the source surface; new UVs only in remodeled digits. Unrigged/static candidate. Review Hand_Before_After.png and README.md.\n')
# Keep the native artist file free of a destructive triangulation step.
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'SunboundWarden_RebuiltFingers_v005.blend'))
bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o;tri=o.modifiers.new('Temporary export triangulation','TRIANGULATE');tri.quad_method='FIXED';tri.ngon_method='BEAUTY';e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());em=e.to_mesh();em.calc_loop_triangles();expected=len(em.loop_triangles);e.to_mesh_clear()
bpy.ops.export_scene.gltf(filepath=str(OUT/'SunboundWarden_RebuiltFingers_v005.glb'),export_format='GLB',use_selection=True,export_animations=False,export_apply=True,export_yup=True)
bpy.ops.export_scene.fbx(filepath=str(OUT/'SunboundWarden_RebuiltFingers_v005.fbx'),use_selection=True,object_types={'MESH'},bake_anim=False,axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=True)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(OUT/'SunboundWarden_RebuiltFingers_v005.glb'));meshes=[ob for ob in bpy.context.scene.objects if ob.type=='MESH'];tris=0;coords=[]
for ob in meshes:ob.data.calc_loop_triangles();tris+=len(ob.data.loop_triangles);coords.extend(ob.matrix_world@v.co for v in ob.data.vertices)
assert tris==expected,(expected,tris);assert len(meshes)==1
qa={'triangles':tris,'mesh_count':len(meshes),'material_count':len(meshes[0].data.materials),'height_m':max(v.z for v in coords)-min(v.z for v in coords),'hand_boundary_edges':len(hand_boundaries),'textures':images,'glb_loaded_images':[{'name':im.name,'size':list(im.size),'has_data':im.has_data} for im in bpy.data.images if im.type=='IMAGE'],'source_sha256':hashlib.sha256(pathlib.Path('C:/Users/David/OneDrive/Desktop/STAGE 3 CHARACTER NO. 1.blend').read_bytes()).hexdigest(),'rigged':False};(OUT/'export_qa.json').write_text(json.dumps(qa,indent=2));print('EXPORT_PASS',qa['triangles'],qa['height_m'],qa['material_count'])
