import bpy,pathlib,json,math
R=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE');O=R/'SourceAssets/Stage3/SunSlash_NLA/Full158';F=R/'Assets/Stage 3/Enemies/STAGE 3 ENEMY NO. 1/Animations/SunboundWarden_SunSlash_NLAMerged.fbx'
bpy.ops.wm.open_mainfile(filepath=str(O/'SunboundWarden_SunSlash_Full158_Baked.blend'));rig=bpy.data.objects['Armature'];sc=bpy.context.scene
rests={b.name:b.matrix_local.copy() for b in rig.data.bones};parents={b.name:b.parent.name if b.parent else None for b in rig.data.bones};poses={}
for f in range(1,159):
 sc.frame_set(f);poses[f]={b.name:(rig.matrix_world@b.matrix).copy() for b in rig.pose.bones}
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(F));rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');sc=bpy.context.scene;err=0
for f in range(1,159):
 sc.frame_set(f+1)
 for b in rig.pose.bones:
  m=rig.matrix_world@b.matrix;err=max(err,max(abs(m[i][j]-poses[f][b.name][i][j]) for i in range(4) for j in range(4)))
r={'bone_count':len(rig.data.bones),'names_and_parents_match':parents=={b.name:b.parent.name if b.parent else None for b in rig.data.bones},'world_pose_max_matrix_error':err,'rest_max_matrix_error':max(abs(b.matrix_local[i][j]-rests[b.name][i][j]) for b in rig.data.bones for i in range(4) for j in range(4)),'actions':[(a.name,list(a.frame_range)) for a in bpy.data.actions],'armature_scale':list(rig.scale),'armature_rotation':list(rig.rotation_euler)}
(O/'fbx_roundtrip.json').write_text(json.dumps(r,indent=2));print(json.dumps(r))


