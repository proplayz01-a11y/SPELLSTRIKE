import bpy,pathlib,json,hashlib
from mathutils import Vector
D=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/ArtSource/Stage3/Node1/Character/TargetedRework_v001');P=D/'SunboundWarden_TargetedRework_FINAL.blend';R=D/'Review_2026-10-05';h=hashlib.sha256(P.read_bytes()).hexdigest();bpy.ops.wm.open_mainfile(filepath=str(P));sc=bpy.context.scene;dg=bpy.context.evaluated_depsgraph_get();rows=[]
for o in sc.objects:
 if o.type not in {'MESH','CURVE'} or o.hide_render:continue
 e=o.evaluated_get(dg);me=e.to_mesh();me.calc_loop_triangles();rows.append({'name':o.name,'type':o.type,'triangles':len(me.loop_triangles),'uv_layers':list(u.name for u in me.uv_layers),'modifiers':[m.type for m in o.modifiers],'materials':[{'name':m.name,'images':[n.image.name for n in m.node_tree.nodes if n.type=='TEX_IMAGE' and n.image] if m.use_nodes else []} for m in o.data.materials if m]});e.to_mesh_clear()
qa={'source_sha256':h,'blender':bpy.app.version_string,'rigs':[o.name for o in sc.objects if o.type=='ARMATURE'],'active_parts':rows,'active_triangles':sum(x['triangles'] for x in rows)};(R/'inspection.json').write_text(json.dumps(qa,indent=2))
sc.render.engine='BLENDER_WORKBENCH';sc.render.resolution_x=1000;sc.render.resolution_y=1000;sc.render.resolution_percentage=100;sc.world=sc.world or bpy.data.worlds.new('ReviewWorld');sc.world.color=(.12,.12,.12);sc.display.shading.color_type='TEXTURE';sc.display.shading.light='STUDIO';sc.display.shading.show_cavity=True;sc.display.shading.show_shadows=True
cam=sc.camera
if not cam:bpy.ops.object.camera_add();cam=bpy.context.object;sc.camera=cam
cam.data.type='ORTHO';cam.data.ortho_scale=2.18;center=Vector((0,0,.847));cam.location=(0,-5,.847);cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();sc.render.filepath=str(R/'Full_Front.png');bpy.ops.render.render(write_still=True)
cam.data.ortho_scale=.33;center=Vector((.868,.105,1.292))
for name,d in [('Hand_Top',(0,0,2)),('Hand_Oblique',(1,-1,2)),('Hand_Palm',(0,0,-2))]:
 cam.location=center+Vector(d);cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();sc.render.filepath=str(R/(name+'.png'));bpy.ops.render.render(write_still=True)
assert hashlib.sha256(P.read_bytes()).hexdigest()==h
print('READ_ONLY_REVIEW',qa['active_triangles'],len(rows),qa['rigs'])
