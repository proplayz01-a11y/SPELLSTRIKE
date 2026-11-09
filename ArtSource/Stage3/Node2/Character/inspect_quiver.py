import bpy,json,math
from pathlib import Path
from mathutils import Vector
OUT=Path(r'C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Stage3\Node2\Character')
bpy.ops.wm.open_mainfile(filepath=str(OUT/'Source_Inspection.blend'),load_ui=False)
ob=next(o for o in bpy.context.scene.objects if o.type=='MESH')
pts=[ob.matrix_world@v.co for v in ob.data.vertices]
roi=[(i,p) for i,p in enumerate(pts) if p.y<-.095 and -.22<p.z<.13]
rows=[]
for j in range(-22,13,2):
    ps=[p for i,p in roi if j/100<=p.z<(j+2)/100]
    if ps:rows.append({'z':j/100,'n':len(ps),'bounds':[[round(f(p[k] for p in ps),5) for k in range(3)] for f in (min,max)]})
(OUT/'quiver_slices.json').write_text(json.dumps(rows,indent=2));print(json.dumps(rows),flush=True)
sc=bpy.context.scene;cam=sc.camera;target=Vector((.025,-.14,-.045));cam.data.ortho_scale=.38
sc.render.resolution_x=900;sc.render.resolution_y=1000
for name,d in [('Quiver_Source_Front',(3,0,.5)),('Quiver_Source_Side',(0,-3,.5)),('Quiver_Source_Top',(1,-1,3))]:
    cam.location=target+Vector(d);cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();sc.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
