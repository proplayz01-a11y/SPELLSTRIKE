import bpy,pathlib,json,math
from mathutils import Vector
O=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/SourceAssets/Stage3/Recovery_CrouchIdle_NLA')
bpy.ops.wm.open_mainfile(filepath=str(O/'SourceInspection.blend'));sc=bpy.context.scene;rig=bpy.data.objects['Armature'];ad=rig.animation_data;ad.action=None
spec=[('01 FULL crouch entry','Crouch',1,0,21,0),('02 FULL crouch idle','CrouchIdle',18,0,73,4)]
for name,tag,start,lo,hi,blend in spec:
 t=ad.nla_tracks.new();t.name=name;a=bpy.data.actions['ADAPTED_'+tag+'_Canonical_InPlace'];s=t.strips.new(name,start,a);s.action_slot=a.slots[0]
 s.action_frame_start=lo;s.action_frame_end=hi;s.frame_start=start;s.scale=1;s.repeat=1;s.extrapolation='HOLD';s.blend_type='REPLACE';s.use_auto_blend=False;s.blend_in=blend;s.blend_out=0
sc.frame_start=1;sc.frame_end=91;sc.render.fps=30;sc.render.fps_base=1
for label,f in [('Standing guard - full crouch source begins',1),('Crouch-idle blend starts',18),('Crouch-idle fully weighted',22),('End - remains crouched',91)]:sc.timeline_markers.new(label,frame=f)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='DOPESHEET_EDITOR':area.type='NLA_EDITOR'
  if area.type=='VIEW_3D':area.spaces.active.region_3d.view_perspective='CAMERA'
sc.frame_set(1);path=O/'SunboundWarden_Recovery_CrouchIdle_NLA.blend';assert not path.exists();bpy.ops.wm.save_as_mainfile(filepath=str(path))
for tag in ['Final','Side']:
 if tag=='Side':
  cam=sc.camera;cam.location=(4,-.4,1.6);cam.rotation_euler=(Vector((0,0,.95))-cam.location).to_track_quat('-Z','Y').to_euler()
 for f in range(1,92):
  sc.frame_set(f);sc.render.filepath=str(O/(tag+'_%03d.png'%f));bpy.ops.render.render(write_still=True)
report={'frames':[1,91],'fps':30,'duration':3,'strips':[{'track':t.name,'action':s.action.name,'source':[s.action_frame_start,s.action_frame_end],'timeline':[s.frame_start,s.frame_end],'scale':s.scale,'repeat':s.repeat,'blend_in':s.blend_in,'blend_out':s.blend_out,'mode':s.blend_type,'extrapolation':s.extrapolation} for t in ad.nla_tracks for s in t.strips]}
(O/'nla_settings.json').write_text(json.dumps(report,indent=2));print('NLA_PREVIEW_DONE')
