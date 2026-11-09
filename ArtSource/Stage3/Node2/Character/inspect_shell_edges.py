import bpy,bmesh,json
from pathlib import Path
P=Path(r'C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Stage3\Node2\Character')
bpy.ops.wm.open_mainfile(filepath=str(P/'S3N2_DesertArcher_EmptyQuiver_v001.blend'),load_ui=False)
o=bpy.data.objects['DesertArcher_Quiver_Hollow'];ev=o.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=ev.to_mesh();bm=bmesh.new();bm.from_mesh(mesh);bm.faces.ensure_lookup_table();bm.verts.ensure_lookup_table()
rows=[]
for e in bm.edges:
    if not e.is_manifold:
        rows.append({'count':len(e.link_faces),'vertices':[list(v.co) for v in e.verts],'faces':[{'id':f.index,'vertices':[list(v.co) for v in f.verts],'normal':list(f.normal),'area':f.calc_area()} for f in e.link_faces]})
print(json.dumps(rows,indent=2));(P/'shell_edges.json').write_text(json.dumps(rows,indent=2))
