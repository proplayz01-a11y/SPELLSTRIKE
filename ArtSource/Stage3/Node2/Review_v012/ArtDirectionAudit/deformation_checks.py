import bpy,json,math
from pathlib import Path
from mathutils import Vector
p=Path(r'C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Stage3\Node2')
bpy.ops.wm.open_mainfile(filepath=str(p/'S3N2_DesertWeapons_v012.blend'),load_ui=False)
out={'rigid_distance_change_m':{},'string_centers':{},'grip_bounds_center':[0,-.03,0]}
for o in bpy.data.objects:
    if o.type!='MESH' or not o.data.shape_keys: continue
    keys=o.data.shape_keys.key_blocks
    if not keys.get('Draw'): continue
    if o.name.startswith(('Bow_Border','Bow_Shoulder','Bow_TipCap','Bow_Guard','Bow_Nock','Bow_RunePlate','Bow_Collar')):
        b=[v.co.copy() for v in keys['Basis'].data]; d=[v.co.copy() for v in keys['Draw'].data]
        out['rigid_distance_change_m'][o.name]=max(abs((b[i]-b[j]).length-(d[i]-d[j]).length) for i in range(len(b)) for j in range(i))
    if o.name=='Bow_String_Brace':
        for kn in ('Basis','Draw'):
            v=[o.matrix_world @ k.co for k in keys[kn].data]
            out['string_centers'][kn]=[list(sum(v[i:i+4],Vector())/4) for i in (0,4,8)]
(p/'Review_v012'/'ArtDirectionAudit'/'deformation_checks.json').write_text(json.dumps(out,indent=2))
print(json.dumps({'max_rigid_distance_change_m':max(out['rigid_distance_change_m'].values()),'string_centers':out['string_centers']}))
