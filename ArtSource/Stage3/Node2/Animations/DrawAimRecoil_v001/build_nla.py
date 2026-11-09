import bpy,pathlib,json,math
O=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/ArtSource/Stage3/Node2/Animations/DrawAimRecoil_v001')
bpy.ops.wm.open_mainfile(filepath=str(O/'Draw_Review.blend'));sc=bpy.context.scene;rig=bpy.data.objects['Armature'];ad=rig.animation_data;draw=ad.action;draw.name='SOURCE_Standing_Draw_Arrow_FULL_1_32';draw.use_fake_user=True
with bpy.data.libraries.load(str(O/'Recoil_Inspection.blend'),link=False) as (src,dst):dst.actions=list(src.actions)
recoil=dst.actions[0];recoil.name='SOURCE_Standing_Aim_Recoil_FULL_1_22';recoil.use_fake_user=True
# Bind supplied matching rig action without retargeting, root edits or foot IK.
ad.action=None
for name,a,start in [('01 FULL Standing Draw Arrow',draw,1),('02 FULL Standing Aim Recoil',recoil,33)]:
 t=ad.nla_tracks.new();t.name=name;s=t.strips.new(name,start,a);s.action_slot=a.slots[0];s.extrapolation='HOLD_FORWARD';s.blend_type='REPLACE';s.use_auto_blend=False;s.blend_in=0;s.blend_out=0;s.scale=1
sc.frame_start=1;sc.frame_end=54;sc.render.fps=30;sc.name='S3N2_DesertArcher_DrawAimRecoil'
for name,f in [('DRAW START',1),('SHARED AIM POSE - DRAW END',32),('SHARED AIM POSE - RECOIL START',33),('RECOVERY END',54)]:sc.timeline_markers.new(name,frame=f)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='DOPESHEET_EDITOR':area.type='NLA_EDITOR'
  if area.type=='VIEW_3D':
   area.spaces.active.region_3d.view_perspective='CAMERA';area.spaces.active.overlay.show_overlays=False
sc.frame_set(1);bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(O/'S3N2_DesertArcher_DrawAimRecoil_NLA_v001.blend'))
for f in range(1,55):
 sc.frame_set(f);sc.render.filepath=str(O/('Merged_%03d.png'%f));bpy.ops.render.render(write_still=True)
# Additional side view around the join, to inspect hand depth and shoulder transitions.
cam=sc.camera;target=(cam.location+cam.rotation_euler.to_quaternion()@__import__('mathutils').Vector((0,0,-1))*0) # overwritten below
from mathutils import Vector
cam.location=(3.8,0,1.65);target=Vector((0,0,.95));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
for f in range(27,41):
 sc.frame_set(f);sc.render.filepath=str(O/('JoinSide_%03d.png'%f));bpy.ops.render.render(write_still=True)
print('NLA_FULL54_READY')

