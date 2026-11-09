import bpy,pathlib,json,bmesh,hashlib,collections
from mathutils import Vector
O=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/ArtSource/Stage3/Node1/Character/TargetedRework_v001');bpy.ops.wm.open_mainfile(filepath=str(O/'SunboundWarden_TargetedRework_v002.blend'));sc=bpy.context.scene;src=bpy.data.objects['model'];body=bpy.data.objects['SunboundWarden_Body_Armor_Preserved'];hands=bpy.data.collections['REWORK_New_Hands'];dg=bpy.context.evaluated_depsgraph_get();report={'parts':[]}
# Verify untouched body coordinates and UV corners away from the wrist operations.
def signature(o):
 me=o.data;uv=me.uv_layers.active.data;c=collections.Counter()
 for p in me.polygons:
  if all(-.77<me.vertices[i].co.x<.77 for i in p.vertices):
   corners=tuple(sorted(tuple(round(x,6) for x in me.vertices[me.loops[j].vertex_index].co)+tuple(round(x,6) for x in uv[j].uv) for j in p.loop_indices));c[corners]+=1
 return c
report['body_interior_geometry_uv_unchanged']=signature(src)==signature(body)
for o in [body]+list(hands.objects):
 e=o.evaluated_get(dg);me=e.to_mesh();me.calc_loop_triangles();r={'name':o.name,'triangles':len(me.loop_triangles),'verts':len(me.vertices)}
 if 'Glove' in o.name:
  bm=bmesh.new();bm.from_mesh(o.data);r.update(base_nonmanifold_edges=sum(not e.is_manifold for e in bm.edges),base_vertices=len(bm.verts));bm.free()
 report['parts'].append(r);e.to_mesh_clear()
report['total_triangles']=sum(x['triangles'] for x in report['parts']);report['source_unchanged']=hashlib.sha256(pathlib.Path('C:/Users/David/OneDrive/Desktop/STAGE 3 CHARACTER NO. 1.blend').read_bytes()).hexdigest()==json.loads((O/'inspection.json').read_text())['source_sha256']
# Unwrap only new glove and metal meshes for future artist texture work.
for o in hands.objects:
 if o.type!='MESH':continue
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
 if not o.data.uv_layers:o.data.uv_layers.new(name='ReworkUV')
 bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=1.151917,island_margin=.015);bpy.ops.object.mode_set(mode='OBJECT')
# Store authoring notes inside the blend.
notes=bpy.data.texts.new('READ_ME_Targeted_Rework');notes.write('Targeted hand rework, 2026-10-05. Original body/armor and UVs preserved away from wrist cuts. Both hands are new connected glove meshes with separate armor panels, cuffs and solar details. Original model remains hidden in REFERENCE_Original_Unedited. Not rigged: weights, gripping/deformation, Unity materials and PC/Android acceptance remain untested. GLB export consolidates the render meshes into body/right hand/left hand. See adjacent README.md and verification.json.\n')
# Ready-to-open framing.
cam=sc.camera;center=Vector((0,0,.847));cam.location=(0,-5,.847);cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=2.18
bpy.ops.object.select_all(action='DESELECT');body.select_set(True);bpy.context.view_layer.objects.active=body
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':area.spaces.active.region_3d.view_perspective='CAMERA';area.spaces.active.shading.color_type='TEXTURE';area.spaces.active.overlay.show_overlays=False
bpy.ops.wm.save_as_mainfile(filepath=str(O/'SunboundWarden_TargetedRework_FINAL.blend'))
# Evaluate and consolidate only active meshes into a temporary export collection.
export=bpy.data.collections.new('EXPORT_TEMP');sc.collection.children.link(export);groups={'Body':[body],'Hand_R':[o for o in hands.objects if '_R_' in o.name],'Hand_L':[o for o in hands.objects if '_L_' in o.name]};outs=[]
for tag,objs in groups.items():
 copies=[]
 for o in objs:
  me=bpy.data.meshes.new_from_object(o.evaluated_get(dg),preserve_all_data_layers=True,depsgraph=dg);cp=bpy.data.objects.new('ExportPart',me);export.objects.link(cp);cp.matrix_world=o.matrix_world.copy();copies.append(cp)
 bpy.ops.object.select_all(action='DESELECT')
 for cp in copies:cp.select_set(True)
 bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();out=bpy.context.object;out.name='SunboundWarden_'+tag;outs.append(out)
bpy.ops.object.select_all(action='DESELECT')
for o in outs:o.select_set(True)
bpy.ops.export_scene.gltf(filepath=str(O/'SunboundWarden_TargetedRework_FINAL.glb'),export_format='GLB',use_selection=True,export_animations=False,export_apply=True,export_yup=True)
(O/'verification.json').write_text(json.dumps(report,indent=2));print('VERIFIED',report['body_interior_geometry_uv_unchanged'],report['source_unchanged'],report['total_triangles']);print([p for p in report['parts'] if 'Glove' in p['name']])
