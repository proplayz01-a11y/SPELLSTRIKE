import bpy,pathlib,json,math
R=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE');O=R/'SourceAssets/Stage3/SunSlash_NLA';F=R/'Assets/Stage 3/Enemies/STAGE 3 ENEMY NO. 1/Animations/SunboundWarden_SunSlash_NLAMerged.fbx'
assert not F.exists(),'Output already exists'
bpy.ops.wm.open_mainfile(filepath=str(O/'SunboundWarden_SunSlash_NLA_Final.blend'));sc=bpy.context.scene;rig=bpy.data.objects['Armature'];baseline={};samples=[]
for f in range(1,52):
 sc.frame_set(f);baseline[f]={b.name:b.matrix.copy() for b in rig.pose.bones};samples.append({'frame':f,**{n:list(rig.matrix_world@rig.pose.bones['mixamorig:'+n].head) for n in ['Hips','LeftFoot','RightFoot']}})
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.nla.bake(frame_start=1,frame_end=51,step=1,only_selected=False,visual_keying=True,clear_constraints=True,clear_parents=False,use_current_action=False,clean_curves=False,bake_types={'POSE'})
a=rig.animation_data.action;a.name='SunboundWarden_SunSlash_NLAMerged';a.use_fake_user=True
for t in rig.animation_data.nla_tracks:t.mute=True
err=0
for f in range(1,52):
 sc.frame_set(f)
 for b in rig.pose.bones:err=max(err,max(abs(b.matrix[i][j]-baseline[f][b.name][i][j]) for i in range(4) for j in range(4)))
report={'blender':bpy.app.version_string,'frames':[1,51],'fps':30,'bones':len(rig.data.bones),'max_bake_matrix_error':err,'samples':samples,'strips':[{ 'track':t.name,'start':s.frame_start,'end':s.frame_end,'source_start':s.action_frame_start,'source_end':s.action_frame_end,'blend_in':s.blend_in,'blend_out':s.blend_out,'scale':s.scale} for t in rig.animation_data.nla_tracks for s in t.strips]}
assert err<.01,err
sc.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(O/'SunboundWarden_SunSlash_Baked.blend'))
bpy.ops.export_scene.fbx(filepath=str(F),use_selection=True,object_types={'ARMATURE'},add_leaf_bones=False,use_armature_deform_only=False,bake_anim=True,bake_anim_use_all_bones=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=False,bake_anim_force_startend_keying=True,bake_anim_step=1,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_NONE')
(O/'validation.json').write_text(json.dumps(report,indent=2));print('EXPORT_DONE',err)
