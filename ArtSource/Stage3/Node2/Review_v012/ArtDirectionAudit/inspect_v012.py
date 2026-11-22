import bpy, json, math
from pathlib import Path
from mathutils import Vector

base = Path(r'C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Stage3\Node2')
bpy.ops.wm.open_mainfile(filepath=str(base / 'S3N2_DesertWeapons_v012.blend'), load_ui=False)
out = base / 'Review_v012' / 'ArtDirectionAudit'
out.mkdir(exist_ok=True)
dg = bpy.context.evaluated_depsgraph_get()

def xyz(v): return [round(float(n), 6) for n in v]

def mesh_state(o):
    ev = o.evaluated_get(dg)
    mesh = ev.to_mesh()
    mesh.calc_loop_triangles()
    pts = [ev.matrix_world @ v.co for v in mesh.vertices]
    bounds = [[round(f(p[k] for p in pts),6) for k in range(3)] for f in (min,max)] if pts else []
    ret = {'triangles':len(mesh.loop_triangles), 'vertices':len(mesh.vertices), 'bounds':bounds}
    ev.to_mesh_clear()
    return ret

report={'blender':bpy.app.version_string, 'scene':bpy.context.scene.name,
        'engine':bpy.context.scene.render.engine, 'camera':bpy.context.scene.camera.name if bpy.context.scene.camera else None,
        'unit_scale':bpy.context.scene.unit_settings.scale_length,
        'objects':[], 'materials':[], 'images':[{'name':i.name,'path':i.filepath,'size':list(i.size)} for i in bpy.data.images]}
for o in bpy.data.objects:
    d={'name':o.name,'type':o.type,'collections':[c.name for c in o.users_collection],
       'parent':o.parent.name if o.parent else None, 'location':xyz(o.location), 'world_location':xyz(o.matrix_world.translation),
       'rotation':xyz(o.rotation_euler),'scale':xyz(o.scale),'hide_render':o.hide_render,'hide_viewport':o.hide_viewport,
       'modifiers':[{'name':m.name,'type':m.type} for m in o.modifiers]}
    if o.type=='MESH':
        d.update(mesh_state(o))
        d['materials']=[m.name if m else None for m in o.data.materials]
        d['uv_layers']=[u.name for u in o.data.uv_layers]
        d['shape_keys']=[{'name':k.name,'value':k.value} for k in o.data.shape_keys.key_blocks] if o.data.shape_keys else []
        d['base_vertices']=[xyz(v.co) for v in o.data.vertices]
        d['base_polygons']=[list(p.vertices) for p in o.data.polygons]
    report['objects'].append(d)
for m in bpy.data.materials:
    d={'name':m.name,'diffuse':list(m.diffuse_color),'nodes':[]}
    if m.node_tree:
        for n in m.node_tree.nodes:
            nd={'name':n.name,'type':n.type,'inputs':{}}
            for inp in n.inputs:
                if hasattr(inp,'default_value') and not inp.is_linked:
                    v=inp.default_value
                    try: v=list(v)
                    except TypeError: pass
                    if isinstance(v,(int,float,str,list,bool)): nd['inputs'][inp.name]=v
            d['nodes'].append(nd)
    report['materials'].append(d)
report['draw_states']={}
for value in (0.0,0.5,1.0):
    for o in bpy.data.objects:
        if o.type=='MESH' and o.data.shape_keys and o.data.shape_keys.key_blocks.get('Draw'):
            o.data.shape_keys.key_blocks['Draw'].value=value
    bpy.context.view_layer.update()
    report['draw_states'][str(value)]={o.name:mesh_state(o) for o in bpy.data.objects if o.type=='MESH'}
(out/'inspection.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('INSPECTION_SUMMARY')
print(json.dumps({k:v for k,v in report.items() if k not in ('objects','draw_states')},indent=2))
for o in report['objects']:
    print(json.dumps({k:v for k,v in o.items() if k not in ('base_vertices','base_polygons','rotation','scale','uv_layers','hide_viewport','modifiers')}))
print('READ_ONLY_INSPECTION_DONE; no blend save operation executed')
