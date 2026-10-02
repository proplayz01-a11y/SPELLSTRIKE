import bpy,json,numpy as np
from pathlib import Path
P=Path(r'C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Stage3\Node2\Character')
bpy.ops.wm.open_mainfile(filepath=str(P/'Source_Inspection.blend'),load_ui=False)
o=next(o for o in bpy.context.scene.objects if o.type=='MESH');pts=[o.matrix_world@v.co for v in o.data.vertices]
parents=list(range(len(pts)))
def find(i):
    while parents[i]!=i:parents[i]=parents[parents[i]];i=parents[i]
    return i
def union(a,b):parents[find(a)]=find(b)
lookup={}
for i,p in enumerate(pts):
    key=tuple(round(c,6) for c in p)
    if key in lookup:union(i,lookup[key])
    else:lookup[key]=i
for e in o.data.edges:union(*e.vertices)
groups={}
for f in o.data.polygons:groups.setdefault(find(f.vertices[0]),[]).append(f.index)
rows=[]
for key,fs in groups.items():
    ids=set(i for fi in fs for i in o.data.polygons[fi].vertices)
    rows.append({'id':key,'faces':len(fs),'face_ids':fs,'bounds':[[min(pts[i][k] for i in ids) for k in range(3)],[max(pts[i][k] for i in ids) for k in range(3)]]})
rows.sort(key=lambda r:-r['faces']);(P/'welded_components.json').write_text(json.dumps(rows,indent=2))
print(json.dumps([{k:v for k,v in r.items() if k!='face_ids'} for r in rows[:40]],indent=2))
cap=json.loads((P/'cap_faces.json').read_text());q=np.array([p for r in cap if 6543<=r['id']<=6578 for p in r['vertices']]);center=q.mean(axis=0);_,_,v=np.linalg.svd(q-center);normal=v[-1];normal*=np.sign(normal[2]);print('CAP_FIT',center.tolist(),normal.tolist(),'range',q.min(axis=0).tolist(),q.max(axis=0).tolist())
