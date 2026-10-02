import bpy,bmesh,json,math,sys
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

BASE=Path(r'C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Stage3\Node2');OUT=BASE/'Revision_v013'
bpy.ops.wm.open_mainfile(filepath=str(BASE/'S3N2_DesertWeapons_v013.blend'),load_ui=False)
sc=bpy.data.scenes['S3N2_Review_v013'];bpy.context.window.scene=sc;dg=bpy.context.evaluated_depsgraph_get()
bow=[o for o in bpy.data.collections['SRC_Bow'].objects if o.type=='MESH']
arrow=[o for o in bpy.data.collections['SRC_Arrow'].objects if o.type=='MESH']
root=bpy.data.objects['S3N2_Bow_ROOT'];held=bpy.data.objects['PREVIEW_HeldArrow_ROOT'];cam=sc.camera
report={'bow_triangles':0,'arrow_triangles':0,'objects':{},'draw_checks':{},'release_overlaps':[],'missing_uv':[],'non_unit_scale':[],'outward_rune_normals':{},'native_timeline':{}}
def getmesh(o):
    # Source arrow objects are not in the review scene; they have no modifiers or keys.
    if o in arrow:
        m=o.data;m.calc_loop_triangles();pts=[o.matrix_world @ v.co for v in m.vertices];faces=[tuple(t.vertices) for t in m.loop_triangles]
    else:
        e=o.evaluated_get(dg);m=e.to_mesh();m.calc_loop_triangles();pts=[e.matrix_world @ v.co for v in m.vertices];faces=[tuple(t.vertices) for t in m.loop_triangles];e.to_mesh_clear()
    return pts,faces
def state(d,release=0):
    root['Draw']=float(d);held['Release']=float(release);root.update_tag();held.update_tag();bpy.context.view_layer.update()
def tree(o,offset=None):
    pts,faces=getmesh(o)
    if offset is not None:pts=[p+offset for p in pts]
    return BVHTree.FromPolygons(pts,faces,all_triangles=True)
state(0)
for group,objects in [('bow',bow),('arrow',arrow)]:
    for o in objects:
        pts,faces=getmesh(o);report[group+'_triangles']+=len(faces)
        bm=bmesh.new();bm.from_mesh(o.data)
        report['objects'][o.name]={'triangles':len(faces),'boundary_edges':sum(e.is_boundary for e in bm.edges),'nonmanifold_edges':sum(not e.is_manifold for e in bm.edges),'zero_area_faces':sum(f.calc_area()<1e-12 for f in bm.faces),'materials':[m.name for m in o.data.materials]};bm.free()
        if not o.data.uv_layers:report['missing_uv'].append(o.name)
        if any(abs(s-1)>1e-6 for s in o.scale):report['non_unit_scale'].append(o.name)
        if o.name in ('Bow_RunePlate_U','Bow_RunePlate_L'):
            report['outward_rune_normals'][o.name]=all(p.center.x*p.normal.x>0 for p in o.data.polygons)
        if o.name.startswith(('Arrow_Rune_A','Arrow_Rune_B')):
            report['outward_rune_normals'][o.name]=all(p.center.z*p.normal.z>0 for p in o.data.polygons)
for d in (0,.25,.5,.75,1):
    state(d);p=Vector((0,.28+.4*d,.12));hits=[];overlaps=[];bt={o.name:tree(o) for o in bow}
    for name,t in bt.items():
        if name=='Bow_String_Brace':continue
        h=t.ray_cast(p,Vector((0,-1,0)),.95)
        if h[0] is not None:hits.append({'object':name,'position':list(h[0])})
    for a in arrow:
        at=tree(a,p)
        for name,t in bt.items():
            if name=='Bow_String_Brace' and a.name=='Arrow_NockCollar':continue
            pairs=at.overlap(t)
            if pairs:overlaps.append([a.name,name,len(pairs)])
    string=bpy.data.objects['Bow_String_Brace'].evaluated_get(dg).data
    centers=[sum((v.co for v in string.vertices[i:i+4]),Vector())/4 for i in (0,4,8)]
    report['draw_checks'][str(d)]={'axis_hits':hits,'arrow_bow_surface_overlaps':overlaps,'string_centers':[list(p) for p in centers],'held_arrow_world':list(held.evaluated_get(dg).matrix_world.translation)}
# Discrete surface-overlap checks over release; not a continuous collision proof.
for step in range(121):
    release=step/120;d=max(0,1-release*4.4);state(d,release)
    offset=Vector((0,.68-1.76*release,.12));bt={o.name:tree(o) for o in bow}
    for a in arrow:
        at=tree(a,offset)
        for name,t in bt.items():
            if name=='Bow_String_Brace' and a.name=='Arrow_NockCollar':continue
            pairs=at.overlap(t)
            if pairs:report['release_overlaps'].append({'sample':step,'arrow':a.name,'bow':name,'triangle_pairs':len(pairs)})
for frame in (1,16,31,38,39,43,48,60):
    sc.frame_set(frame);bpy.context.view_layer.update()
    report['native_timeline'][str(frame)]={'draw':root['Draw'],'release':held['Release'],'held_arrow_world':list(held.evaluated_get(dg).matrix_world.translation),'string_draw':bpy.data.objects['Bow_String_Brace'].data.shape_keys.key_blocks['Draw'].value}
(OUT/'qa.json').write_text(json.dumps(report,indent=2))
print('QA_SUMMARY '+json.dumps({k:v for k,v in report.items() if k!='objects'}),flush=True)
if '--no-render' in sys.argv:sys.exit(0)
def render(name,location,target,scale,d=0,mode='bow',clay=False,frame=None):
    f=1+30*d if frame is None else frame
    sc.frame_set(int(f),subframe=f-int(f))
    state(d)
    for o in bow:o.hide_render=mode not in ('bow','both')
    for o in bpy.data.collections['PREVIEW_HeldArrow'].objects:
        if o.type=='MESH':o.hide_render=mode not in ('arrow','both')
    cam.location=location;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=scale
    sc.view_layers[0].material_override=claymat if clay else None
    sc.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True);print('RENDERED '+name,flush=True)
claymat=bpy.data.materials.new('ReviewOnly_Clay');claymat.diffuse_color=(.35,.35,.35,1);claymat.use_nodes=True;claymat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.35,.35,.35,1)
release_only='--release-only' in sys.argv
motion_only='--motion-only' in sys.argv or release_only
if not motion_only:
    render('Bow_Brace_Material',(3,-1.65,.85),(0,.06,0),1.94,mode='both')
    render('Bow_Profile_Material',(3,.10,0),(0,.10,0),1.90,mode='both')
    render('Bow_Front_Material',(0,-3,0),(0,0,0),1.90)
    render('Bow_Opposite_Material',(-3,-1.4,.5),(0,.08,0),1.94)
    render('Bow_Grip_Window',(3,-1,.65),(0,0,.11),.58,mode='both')
for d,n in [(0,'000'),(.25,'025'),(.5,'050'),(.75,'075'),(1,'100')]:
    if release_only:continue
    if motion_only and d in (0,1):continue
    render('Bow_Draw_'+n,(3,-1.65,.85),(0,.06,0),1.94,d,mode='both',clay=True)
if not motion_only:
    render('Arrow_Broadside',(0,-.195,3),(0,-.195,.12),1.10,mode='arrow')
    render('Arrow_Edge',(3,-.195,.12),(0,-.195,.12),1.10,mode='arrow')
    render('Arrow_Nock',(.13,.45,.23),(0,.28,.12),.085,mode='both')
for frame in (39,43,48):
    # Read native keyframe values rather than manually posing the render.
    sc.frame_set(frame);d=root['Draw'];r=held['Release']
    for o in bow:o.hide_render=False
    for o in bpy.data.collections['PREVIEW_HeldArrow'].objects:
        if o.type=='MESH':o.hide_render=False
    cam.location=(3,-1.65,.85);cam.rotation_euler=(Vector((0,.06,0))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=2.30
    sc.view_layers[0].material_override=None;sc.render.filepath=str(OUT/('Release_Frame_'+str(frame)+'.png'));bpy.ops.render.render(write_still=True)
print('QA_AND_REVIEW_RENDERS_COMPLETE',flush=True)
