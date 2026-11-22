import bpy,pathlib,json,hashlib,math
from mathutils import Vector
OUT=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/ArtSource/Stage3/Node1/Character/OriginalHands_Rework_v001');bpy.ops.wm.open_mainfile(filepath=str(OUT/'SunboundWarden_OriginalHands_Reworked_v002.blend'));o=bpy.data.objects['SunboundWarden_Original_RefinedHands'];sc=bpy.context.scene;bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
# Resolve the actual source material images to fresh output-local files for portable FBX embedding.
texdir=OUT/'Textures';texdir.mkdir(exist_ok=True);images={}
for mat in o.data.materials:
 for node in mat.node_tree.nodes:
  if node.type=='TEX_IMAGE' and node.image:
   im=node.image
   if im.name not in images:
    path=texdir/im.name
    if im.packed_file:path.write_bytes(bytes(im.packed_file.data))
    else:im.filepath_raw=str(path);im.file_format='PNG';im.save()
    im.filepath=str(path);images[im.name]={'has_data':im.has_data,'size':list(im.size),'sha256':hashlib.sha256(path.read_bytes()).hexdigest()}
# Save native copy with all original painted maps packed and locally resolved.
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'SunboundWarden_OriginalHands_Reworked_v002.blend'))
tri=o.modifiers.new('Temporary explicit export triangulation','TRIANGULATE');tri.quad_method='FIXED';tri.ngon_method='BEAUTY'
dg=bpy.context.evaluated_depsgraph_get();evalobj=o.evaluated_get(dg);em=evalobj.to_mesh();em.calc_loop_triangles();coords=[o.matrix_world@v.co for v in em.vertices];qa={'export_triangles':len(em.loop_triangles),'evaluated_vertices':len(em.vertices),'height_m':max(v.z for v in coords)-min(v.z for v in coords),'material_count':len(o.data.materials),'images':images,'source_sha256':hashlib.sha256(pathlib.Path('C:/Users/David/OneDrive/Desktop/STAGE 3 CHARACTER NO. 1.blend').read_bytes()).hexdigest()};evalobj.to_mesh_clear()
bpy.ops.export_scene.gltf(filepath=str(OUT/'SunboundWarden_OriginalHands_Reworked_v002.glb'),export_format='GLB',use_selection=True,export_animations=False,export_apply=True,export_yup=True)
bpy.ops.export_scene.fbx(filepath=str(OUT/'SunboundWarden_OriginalHands_Reworked_v002.fbx'),use_selection=True,object_types={'MESH'},bake_anim=False,axis_forward='-Z',axis_up='Y',global_scale=1,apply_unit_scale=True,path_mode='COPY',embed_textures=True)
(OUT/'export_qa.json').write_text(json.dumps(qa,indent=2))
# Reimport the GLB into an empty scene and verify mesh/texture data.
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(OUT/'SunboundWarden_OriginalHands_Reworked_v002.glb'));meshes=[a for a in bpy.context.scene.objects if a.type=='MESH'];tris=0;vs=[]
for a in meshes:a.data.calc_loop_triangles();tris+=len(a.data.loop_triangles);vs.extend(a.matrix_world@v.co for v in a.data.vertices)
roundtrip={'meshes':len(meshes),'triangles':tris,'height_m':max(v.z for v in vs)-min(v.z for v in vs),'images':[{'name':i.name,'size':list(i.size),'has_data':i.has_data} for i in bpy.data.images if i.type=='IMAGE']};print('TRIANGLE_COMPARE',qa['export_triangles'],roundtrip['triangles']);assert roundtrip['triangles']==qa['export_triangles'];assert abs(roundtrip['height_m']-qa['height_m'])<.00001
(OUT/'glb_roundtrip.json').write_text(json.dumps(roundtrip,indent=2));print('EXPORT_PASS',qa['export_triangles'],roundtrip)


