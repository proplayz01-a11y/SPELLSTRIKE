"""Non-destructive diagnostics of Rodin Nevali mesh connectivity and head region."""
import bpy, json, collections
from pathlib import Path
from mathutils import Vector
PROJECT=Path(bpy.path.abspath("//"))
model=PROJECT/"ArtSource/Characters/Nevali/Original/base_basic_pbr.glb"
output=PROJECT/"Library/NevaliModelRepair_20261023/mesh_diagnostic.json"
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(model))
obj=next(o for o in bpy.context.scene.objects if o.type=="MESH")
mesh=obj.data; n=len(mesh.vertices);parent=list(range(n))
def find(x):
    while x!=parent[x]:
        parent[x]=parent[parent[x]]
        x=parent[x]
    return x
def union(a,b):
    a=find(a);b=find(b)
    if a!=b:parent[b]=a
for edge in mesh.edges:union(edge.vertices[0],edge.vertices[1])
comp=collections.defaultdict(list)
for i in range(n):comp[find(i)].append(i)
result=[]
for ids in comp.values():
    xyz=[mesh.vertices[i].co for i in ids]
    mn=[min(v[k] for v in xyz) for k in range(3)]
    mx=[max(v[k] for v in xyz) for k in range(3)]
    ctr=[sum(v[k] for v in xyz)/len(xyz) for k in range(3)]
    result.append({"count":len(ids),"min":mn,"max":mx,"center":ctr,"sample_vertex":ids[0]})
result.sort(key=lambda x:x["count"],reverse=True)
head=[dict(x) for x in result if x["max"][2]>1.64]
stats={"vertices":n,"edges":len(mesh.edges),"polygons":len(mesh.polygons),
       "components":len(result),"components_largest":result[:20],
       "head_components":head,
       "mesh_min":[min(v.co[k] for v in mesh.vertices) for k in range(3)],
       "mesh_max":[max(v.co[k] for v in mesh.vertices) for k in range(3)],
       "material_names":[m.name for m in mesh.materials]}
output.parent.mkdir(parents=True,exist_ok=True)
output.write_text(json.dumps(stats,indent=2))
print("DIAGNOSTIC",json.dumps({"verts":n,"components":len(result),
       "head_components":len(head),"top_counts":[x["count"] for x in result[:25]]}))
