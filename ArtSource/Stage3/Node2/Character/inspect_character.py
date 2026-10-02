import bpy,json,bmesh,hashlib
from pathlib import Path
from mathutils import Vector
SRC=Path(r'C:\Users\David\Downloads\fantasy hooded character 3d model.glb')
OUT=Path(r'C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Stage3\Node2\Character');OUT.mkdir(exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(SRC))
data={'source':str(SRC),'sha256':hashlib.sha256(SRC.read_bytes()).hexdigest(),'meshes':[]}
allpts=[]
for o in bpy.context.scene.objects:
    if o.type!='MESH':continue
    pts=[o.matrix_world@v.co for v in o.data.vertices];allpts+=pts;o.data.calc_loop_triangles()
    adjacency=[[] for _ in pts]
    for e in o.data.edges:
        a,b=e.vertices;adjacency[a].append(b);adjacency[b].append(a)
    seen=set();components=[]
    for i in range(len(pts)):
        if i in seen:continue
        stack=[i];seen.add(i);ids=[]
        while stack:
            x=stack.pop();ids.append(x)
            for j in adjacency[x]:
                if j not in seen:seen.add(j);stack.append(j)
        components.append({'vertices':len(ids),'bounds':[[f(pts[j][k] for j in ids) for k in range(3)] for f in (min,max)],'sample_vertex':i})
    data['meshes'].append({'name':o.name,'vertices':len(pts),'triangles':len(o.data.loop_triangles),'materials':[m.name for m in o.data.materials], 'components':sorted(components,key=lambda c:-c['vertices'])})
lo=Vector([min(p[k] for p in allpts) for k in range(3)]);hi=Vector([max(p[k] for p in allpts) for k in range(3)])
data['bounds']=[list(lo),list(hi)];data['images']=[{'name':i.name,'size':list(i.size)} for i in bpy.data.images]
(OUT/'source_inspection.json').write_text(json.dumps(data,indent=2))
print(json.dumps({**data,'meshes':[{**m,'components':m['components'][:20]} for m in data['meshes']]},indent=2),flush=True)
sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.samples=16;sc.cycles.device='CPU';sc.cycles.use_denoising=True;sc.render.threads_mode='FIXED';sc.render.threads=4
sc.render.resolution_x=900;sc.render.resolution_y=1000;sc.render.resolution_percentage=100
sc.world=bpy.data.worlds.new('ReviewWorld');sc.world.use_nodes=True;sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.12,.12,.12,1);sc.world.node_tree.nodes['Background'].inputs[1].default_value=.5
center=(lo+hi)/2;h=hi.z-lo.z
cam=bpy.data.objects.new('ReviewCamera',bpy.data.cameras.new('ReviewCamera'));sc.collection.objects.link(cam);sc.camera=cam;cam.data.type='ORTHO';cam.data.ortho_scale=max(h,(hi.x-lo.x)/.9)*1.12
for name,offset,power in [('Key',(2,-3,3),450),('Fill',(-3,-1,2),220),('Rim',(1,3,2),300)]:
    ld=bpy.data.lights.new(name,'AREA');ld.energy=power;ld.size=3;ob=bpy.data.objects.new(name,ld);sc.collection.objects.link(ob);ob.location=center+Vector(offset)*h/2;ob.rotation_euler=(center-ob.location).to_track_quat('-Z','Y').to_euler()
for name,offset in [('Source_Front',(0,-4,0)),('Source_Back',(0,4,0)),('Source_ThreeQuarter',(3,-4,1))]:
    cam.location=center+Vector(offset)*h;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();sc.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Source_Inspection.blend'))
