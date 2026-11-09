import bpy,pathlib,json
from mathutils import Vector
O=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/ArtSource/Stage3/Node1/Character/TargetedRework_v001');bpy.ops.wm.open_mainfile(filepath=str(O/'Source_Inspection.blend'));sc=bpy.context.scene;o=bpy.data.objects['model'];vs=[o.matrix_world@v.co for v in o.data.vertices if v.co.x>.77];lo=Vector([min(v[i] for v in vs) for i in range(3)]);hi=Vector([max(v[i] for v in vs) for i in range(3)]);print('HAND_BOUNDS',list(lo),list(hi));center=(lo+hi)/2;cam=sc.camera;cam.data.ortho_scale=.30
for name,d in [('HandTop',(0,0,2)),('HandPalm',(0,0,-2)),('HandFront',(0,-2,.35)),('HandOblique',(1,-1,2))]:
 cam.location=center+Vector(d);cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();sc.render.filepath=str(O/('Source_'+name+'.png'));bpy.ops.render.render(write_still=True)
(O/'hand_vertices.json').write_text(json.dumps([list(v) for v in vs]))
