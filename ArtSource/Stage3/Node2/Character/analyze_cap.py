import bpy,json,numpy as np
from pathlib import Path
P=Path(r'C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Stage3\Node2\Character')
bpy.ops.wm.open_mainfile(filepath=str(P/'Source_Inspection.blend'),load_ui=False)
o=next(o for o in bpy.context.scene.objects if o.type=='MESH');m=o.matrix_world;nm=m.to_3x3().inverted().transposed();rows=[]
for f in o.data.polygons:
    c=m@f.center;n=(nm@f.normal).normalized()
    if -.08<c.x<.07 and c.y<-.11 and -.03<c.z<.085 and n.z>.6:
        rows.append({'id':f.index,'center':list(c),'normal':list(n),'area':f.area,'vertices':[list(m@o.data.vertices[i].co) for i in f.vertices]})
(P/'cap_faces.json').write_text(json.dumps(rows,indent=2))
print(json.dumps([{k:v for k,v in r.items() if k!='vertices'} for r in rows],indent=2))
