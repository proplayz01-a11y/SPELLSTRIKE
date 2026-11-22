import bpy,pathlib,json,math
from mathutils import Vector
OUT=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/ArtSource/Stage3/Node1/Character/NewMesh_v001');bpy.ops.wm.open_mainfile(filepath=str(OUT/'SunboundWarden_NewMesh_v001.blend'));sc=bpy.context.scene
for name in ['LAYER_1_Anatomical_Core','LAYER_2_Armor','LAYER_2_Cloth','LAYER_2_Armor_Detail','EXPORT_Baked_Character']:
 for o in bpy.data.collections[name].objects:o.hide_render=True
before=set(bpy.data.objects);bpy.ops.import_scene.gltf(filepath=str(OUT/'SunboundWarden_NewMesh_v001.glb'));new=list(set(bpy.data.objects)-before);meshes=[o for o in new if o.type=='MESH'];vs=[];tris=0
for o in meshes:o.data.calc_loop_triangles();tris+=len(o.data.loop_triangles);vs.extend(o.matrix_world@v.co for v in o.data.vertices)
qa={'mesh_count':len(meshes),'triangle_count':tris,'height_m':max(v.z for v in vs)-min(v.z for v in vs),'material_count':len({m.name for o in meshes for m in o.data.materials if m}),'imported_materials':[]}
for o in meshes:
 for mat in o.data.materials:
  imgs=[n.image for n in mat.node_tree.nodes if n.type=='TEX_IMAGE' and n.image];qa['imported_materials'].append({'name':mat.name,'images':[{'name':i.name,'size':list(i.size),'has_data':i.has_data} for i in imgs]})
expected=json.loads((OUT/'verification.json').read_text());assert tris==expected['triangles'];assert len(meshes)==1;assert abs(qa['height_m']-1.9)<.00001
(OUT/'export_roundtrip.json').write_text(json.dumps(qa,indent=2));sc.cycles.samples=32;sc.camera.location=(3,-5,1.98);sc.camera.rotation_euler=(Vector((0,0,.94))-sc.camera.location).to_track_quat('-Z','Y').to_euler();sc.camera.data.ortho_scale=2.35;sc.render.filepath=str(OUT/'NewMesh_Export_Verified.png');bpy.ops.render.render(write_still=True)
print('EXPORT_ROUNDTRIP_PASS',qa)
