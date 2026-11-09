import bpy,pathlib,json,hashlib
from mathutils import Vector
O=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/ArtSource/Stage3/Node1/Character/TargetedRework_v001');P=pathlib.Path('C:/Users/David/OneDrive/Desktop/STAGE 3 CHARACTER NO. 1.blend')
bpy.ops.wm.open_mainfile(filepath=str(P));out={'source_sha256':hashlib.sha256(P.read_bytes()).hexdigest(),'objects':[],'images':[]}
for o in bpy.context.scene.objects:
 r={'name':o.name,'type':o.type,'matrix':[list(r) for r in o.matrix_world]}
 if o.type=='MESH':
  me=o.data;vs=[o.matrix_world@v.co for v in me.vertices];r.update(verts=len(vs),faces=len(me.polygons),bounds=[[min(v[i] for v in vs) for i in range(3)],[max(v[i] for v in vs) for i in range(3)]],uv=[u.name for u in me.uv_layers],materials=[m.name for m in me.materials if m],modifiers=[m.type for m in o.modifiers])
  parent=list(range(len(vs)))
  def find(a):
   while parent[a]!=a:parent[a]=parent[parent[a]];a=parent[a]
   return a
  for e in me.edges:
   a,b=map(find,e.vertices);parent[a]=b
  groups={}
  for i in range(len(vs)):groups.setdefault(find(i),[]).append(i)
  r['components']=[{'count':len(ids),'bounds':[[min(vs[j][i] for j in ids) for i in range(3)],[max(vs[j][i] for j in ids) for i in range(3)]]} for ids in sorted(groups.values(),key=len,reverse=True)]
 out['objects'].append(r)
for i in bpy.data.images:out['images'].append({'name':i.name,'size':list(i.size),'packed':bool(i.packed_file),'filepath':i.filepath,'has_data':i.has_data})
(O/'inspection.json').write_text(json.dumps(out,indent=2));print(json.dumps(out,indent=2))
# Save inspection scene only into new workspace folder.
sc=bpy.context.scene;sc.render.engine='BLENDER_WORKBENCH';sc.render.resolution_x=900;sc.render.resolution_y=900;sc.render.resolution_percentage=100;sc.world=sc.world or bpy.data.worlds.new('ReviewWorld');sc.world.color=(.10,.10,.10);sc.display.shading.light='STUDIO';sc.display.shading.color_type='TEXTURE';sc.display.shading.show_shadows=True;sc.display.shading.show_cavity=True
obj=next(o for o in sc.objects if o.type=='MESH');vs=[obj.matrix_world@v.co for v in obj.data.vertices];lo=Vector([min(v[i] for v in vs) for i in range(3)]);hi=Vector([max(v[i] for v in vs) for i in range(3)]);center=(lo+hi)/2;h=hi.z-lo.z
bpy.ops.object.camera_add();cam=bpy.context.object;cam.data.type='ORTHO';cam.data.ortho_scale=max(hi.x-lo.x,h)*1.15;sc.camera=cam
for name,vec in [('Front',(0,-4,0)),('Back',(0,4,0)),('Side',(4,0,0))]:
 cam.location=center+Vector(vec)*h;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();sc.render.filepath=str(O/('Source_'+name+'.png'));bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(O/'Source_Inspection.blend'))
