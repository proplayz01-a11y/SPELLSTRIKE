import bpy,pathlib,json,hashlib,math
R=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE');O=R/'SourceAssets/Stage3/RadiantRush_ShortHeavy'
name='SunboundWarden_RadiantRush_ShortHeavy';F=O/(name+'.fbx');B=O/(name+'_Baked.blend')
# Outputs here belong exclusively to this task; original sources are hash-guarded.
bpy.ops.wm.open_mainfile(filepath=str(O/(name+'_NLA.blend')))
sc=bpy.context.scene;sc.name=name;rig=bpy.data.objects['Armature'];mesh=bpy.data.objects['model'];base={};world={};subframes={}
parents={b.name:b.parent.name if b.parent else None for b in rig.data.bones};rest={b.name:b.matrix_local.copy() for b in rig.data.bones}
for f in range(1,76):
 sc.frame_set(f);base[f]={b.name:b.matrix.copy() for b in rig.pose.bones};world[f]={b.name:(rig.matrix_world@b.matrix).copy() for b in rig.pose.bones}
for f in [17.5,18.5,19.5,20.5,33.5,34.5,35.5,36.5,67.5,68.5,69.5,70.5]:
 sc.frame_set(int(f),subframe=f%1);subframes[f]={b.name:(rig.matrix_world@b.matrix).copy() for b in rig.pose.bones}
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
dense={}
for index in range(4,301):
 f=index/4;sc.frame_set(int(f),subframe=f%1);dense[f]={b.name:b.matrix_basis.decompose() for b in rig.pose.bones}
a=bpy.data.actions.new(name);a.use_fake_user=True;rig.animation_data.action=a
for t in rig.animation_data.nla_tracks:t.mute=True
previous={}
for f,pose in dense.items():
 for b in rig.pose.bones:
  loc,q,scale=pose[b.name]
  if b.name in previous and q.dot(previous[b.name])<0:q.negate()
  previous[b.name]=q.copy();b.rotation_mode='QUATERNION';b.location=loc;b.rotation_quaternion=q;b.scale=scale
  for prop in ['location','rotation_quaternion','scale']:b.keyframe_insert(data_path=prop,frame=f,group=b.name)
for layer in a.layers:
 for strip in layer.strips:
  for bag in strip.channelbags:
   for fc in bag.fcurves:
    for key in fc.keyframe_points:key.interpolation='LINEAR'
err=0;hip=[]
for f in range(1,76):
 sc.frame_set(f)
 for b in rig.pose.bones:err=max(err,max(abs(b.matrix[i][j]-base[f][b.name][i][j]) for i in range(4) for j in range(4)))
 hip.append(list(rig.matrix_world@rig.pose.bones['mixamorig:Hips'].head))
assert err<.001,err
# Verify unchanged opening outside the deliberate NLA entry overlap.
opening_error=0
rig.animation_data.action=bpy.data.actions['PREVIEW_ADAPTED_Crouch'];rig.animation_data.action_slot=rig.animation_data.action.slots[0]
for f in range(1,18):
 sc.frame_set(f-1)
 for b in rig.pose.bones:opening_error=max(opening_error,max(abs(b.matrix[i][j]-base[f][b.name][i][j]) for i in range(4) for j in range(4)))
rig.animation_data.action=a;rig.animation_data.action_slot=a.slots[0];sc.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(B))
mesh.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(F),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,use_armature_deform_only=False,bake_anim=True,bake_anim_use_all_bones=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=False,bake_anim_force_startend_keying=True,bake_anim_step=.25,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_NONE',path_mode='STRIP',use_mesh_modifiers=False)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(F),anim_offset=0)
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');sc=bpy.context.scene
rounderr=0
for f in range(1,76):
 sc.frame_set(f)
 for b in rig.pose.bones:
  m=rig.matrix_world@b.matrix;rounderr=max(rounderr,max(abs(m[i][j]-world[f][b.name][i][j]) for i in range(4) for j in range(4)))
suberr=0
for f in subframes:
 sc.frame_set(int(f),subframe=f%1)
 for b in rig.pose.bones:
  m=rig.matrix_world@b.matrix;suberr=max(suberr,max(abs(m[i][j]-subframes[f][b.name][i][j]) for i in range(4) for j in range(4)))
resterr=max(abs(b.matrix_local[i][j]-rest[b.name][i][j]) for b in rig.data.bones for i in range(4) for j in range(4))
same=parents=={b.name:b.parent.name if b.parent else None for b in rig.data.bones}
manifest=json.loads((O/'preservation_manifest.json').read_text());changed=[p for p,h in manifest.items() if not pathlib.Path(p).exists() or hashlib.sha256(pathlib.Path(p).read_bytes()).hexdigest()!=h]
result={'blender':bpy.app.version_string,'action':name,'frames':[1,75],'fps':30,'duration_seconds':74/30,'bake_max_local_matrix_error':err,'opening_frames_1_17_max_matrix_error':opening_error,'fbx_roundtrip_max_world_matrix_error':rounderr,'boundary_half_frame_max_world_matrix_error':suberr,'rest_matrix_error':resterr,'bone_count':len(rig.data.bones),'names_and_parents_match':same,'object_name':rig.name,'object_scale':list(rig.scale),'object_rotation':list(rig.rotation_euler),'hip_horizontal_range_m':[max(x[i] for x in hip)-min(x[i] for x in hip) for i in [0,1]],'imported_actions':[(x.name,list(x.frame_range)) for x in bpy.data.actions],'preserved_file_count':len(manifest),'changed_original_files':changed,'fbx_sha256':hashlib.sha256(F.read_bytes()).hexdigest()}
(O/'export_validation.json').write_text(json.dumps(result,indent=2));print(json.dumps(result,indent=2))
assert same and rounderr<.0001 and resterr<.001 and suberr<.04 and not changed,result
print('EXPORT_VALIDATED')
