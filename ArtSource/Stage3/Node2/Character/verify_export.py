import bpy,json,hashlib,bmesh
from pathlib import Path
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
P=Path(r'C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Stage3\Node2\Character')
bpy.ops.wm.open_mainfile(filepath=str(P/'S3N2_DesertArcher_EmptyQuiver_v001.blend'),load_ui=False)
packed=[{'name':i.name,'packed':bool(i.packed_file),'size':list(i.size),'has_data':i.has_data} for i in bpy.data.images if i.source=='FILE']
saved_root=bpy.data.objects['S3N2_DesertArcher_ROOT'];mouth=bpy.data.objects['Quiver_Opening'].matrix_world.translation.copy();axis=bpy.data.objects['Quiver_Opening'].matrix_world.to_3x3()@Vector((0,0,1))
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(P/'S3N2_DesertArcher_EmptyQuiver_v001.glb'))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];result={'blend_images':packed,'export_meshes':[],'export_images':[]}
points=[]
for o in meshes:
    o.data.calc_loop_triangles();ps=[o.matrix_world@v.co for v in o.data.vertices];points+=ps
    entry={'name':o.name,'triangles':len(o.data.loop_triangles),'materials':[m.name for m in o.data.materials]}
    if 'Quiver' in o.name:
        tree=BVHTree.FromPolygons(ps,[tuple(t.vertices) for t in o.data.loop_triangles],all_triangles=True)
        hit=tree.ray_cast(mouth+axis*.025,-axis,1)
        entry['cavity_depth_m']=hit[3]-.025 if hit[0] is not None else None
    result['export_meshes'].append(entry)
result['height_m']=max(p.z for p in points)-min(p.z for p in points)
result['export_images']=[{'name':i.name,'size':list(i.size),'has_data':i.has_data,'packed':bool(i.packed_file)} for i in bpy.data.images if i.source=='FILE']
result['export_total_triangles']=sum(e['triangles'] for e in result['export_meshes'])
assert len(meshes)==2, 'Export contains unwanted reference/review geometry'
assert all(i['has_data'] for i in result['export_images']), 'Export texture missing'
assert all(i['packed'] for i in packed), 'Blend not self-contained'
assert any(e.get('cavity_depth_m',0)>.25 for e in result['export_meshes']), 'Export opening blocked'
(P/'export_verification.json').write_text(json.dumps(result,indent=2));print(json.dumps(result,indent=2))
