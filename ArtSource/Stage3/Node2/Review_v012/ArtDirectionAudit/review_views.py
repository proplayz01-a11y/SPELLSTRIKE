import bpy, json, math
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

base=Path(r'C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Stage3\Node2')
out=base/'Review_v012'/'ArtDirectionAudit'
bpy.ops.wm.open_mainfile(filepath=str(base/'S3N2_DesertWeapons_v012.blend'),load_ui=False)
sc=bpy.context.scene
dg=bpy.context.evaluated_depsgraph_get()
bow=[o for o in bpy.data.objects if o.type=='MESH' and any(c.name=='SRC_Bow' for c in o.users_collection)]
arrow=[o for o in bpy.data.objects if o.type=='MESH' and any(c.name=='SRC_Arrow' for c in o.users_collection)]
evidence={'bow_meshes':len(bow),'arrow_meshes':len(arrow),'draw':{},'mesh_issues':{}}
def state(v):
    for o in bow:
        if o.data.shape_keys and o.data.shape_keys.key_blocks.get('Draw'):
            o.data.shape_keys.key_blocks['Draw'].value=v
    bpy.context.view_layer.update()
def evaluated(o):
    e=o.evaluated_get(dg); m=e.to_mesh(); m.calc_loop_triangles()
    p=[e.matrix_world @ v.co for v in m.vertices]; faces=[tuple(t.vertices) for t in m.loop_triangles]
    e.to_mesh_clear()
    return p,faces
for v in [0,.25,.5,.75,1]:
    state(v)
    nock=Vector((0,.28+.4*v,.12))
    d={'axis_hits':[],'string_anchor_to_nock_mesh':{}}
    for o in bow:
        pts,faces=evaluated(o)
        if not faces: continue
        tree=BVHTree.FromPolygons(pts,faces,all_triangles=True)
        if o.name!='Bow_String_Brace':
            hit=tree.ray_cast(nock,Vector((0,-1,0)),.95)
            if hit[0] is not None: d['axis_hits'].append({'object':o.name,'position':list(hit[0]),'distance':hit[3]})
        if o.name in ('Bow_Nock_U','Bow_Nock_L'):
            z=(.7+(.628-.7)*v) if o.name.endswith('_U') else (-.7+(-.651+.7)*v)
            near=tree.find_nearest(Vector((0,.28+.12*v,z)))
            d['string_anchor_to_nock_mesh'][o.name]=near[3]
    evidence['draw'][str(v)]=d
state(0)
for o in bow+arrow:
    pts,faces=evaluated(o)
    small=sum(1 for a,b,c in faces if ((pts[b]-pts[a]).cross(pts[c]-pts[a])).length<1e-10)
    edges={}
    for f in o.data.polygons:
        vi=list(f.vertices)
        for a,b in zip(vi,vi[1:]+vi[:1]):
            k=tuple(sorted((a,b))); edges[k]=edges.get(k,0)+1
    if small or o.name in ('Bow_String_Brace','Arrow_Feather_1','Arrow_Feather_2','Arrow_Feather_3','Arrow_NockCollar'):
        evidence['mesh_issues'][o.name]={'zero_area_evaluated_triangles':small,'base_boundary_edges':sum(c==1 for c in edges.values())}
(out/'functional_checks.json').write_text(json.dumps(evidence,indent=2))
print('FUNCTIONAL_CHECKS '+json.dumps(evidence))

# Review cameras and lights are temporary, in-memory only; source meshes/materials unchanged.
for o in bpy.data.objects:
    if o.type in ('MESH','CURVE'):
        o.hide_render = o not in bow+arrow
sc.render.engine='CYCLES'; sc.cycles.device='CPU'; sc.cycles.samples=24; sc.cycles.use_denoising=True
sc.render.threads_mode='FIXED'; sc.render.threads=4
sc.render.resolution_x=800; sc.render.resolution_y=1000; sc.render.resolution_percentage=100
sc.render.image_settings.file_format='PNG'; sc.render.film_transparent=False
sc.world=bpy.data.worlds.new('AuditWorld'); sc.world.use_nodes=True
sc.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.12,.12,.12,1)
sc.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.5
camdata=bpy.data.cameras.new('AuditCamera'); cam=bpy.data.objects.new('AuditCamera',camdata); sc.collection.objects.link(cam); sc.camera=cam
camdata.type='ORTHO'; camdata.lens=50
for name,loc,energy,size in [('Key',(2,-3,3),450,3),('Fill',(-2,-1,1),250,3),('Rim',(1,2,2),350,2)]:
    data=bpy.data.lights.new('Audit'+name,'AREA'); data.energy=energy; data.shape='DISK'; data.size=size
    obj=bpy.data.objects.new('Audit'+name,data); sc.collection.objects.link(obj); obj.location=loc
    obj.rotation_euler=(Vector((0,0,0))-obj.location).to_track_quat('-Z','Y').to_euler()
def render(name,location,target,scale,which='bow'):
    for o in bow: o.hide_render=which!='bow'
    for o in arrow: o.hide_render=which!='arrow'
    cam.location=location; cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler(); camdata.ortho_scale=scale
    sc.render.filepath=str(out/(name+'.png')); bpy.ops.render.render(write_still=True)
    print('AUDIT_RENDER '+name,flush=True)
render('Bow_Profile_Material',(3,.1,0),(0,.1,0),1.85)
render('Bow_Front_Material',(0,-3,0),(0,0,0),1.85)
render('Bow_Center_Material',(2,-2,.4),(0,-.02,.02),.72)
render('Arrow_Broadside_Material',(0,-.46,3),(0,-.46,0),1.10,'arrow')
render('Arrow_Nock_Closeup',(.18,.18,.14),(0,-.008,0),.095,'arrow')
print('REVIEW_RENDERING_DONE; no blend save operation executed',flush=True)
