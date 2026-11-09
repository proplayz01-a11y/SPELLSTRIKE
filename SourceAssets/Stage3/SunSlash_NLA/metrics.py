import bpy,json,pathlib
O=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/SourceAssets/Stage3/SunSlash_NLA');bpy.ops.wm.open_mainfile(filepath=str(O/'SunboundWarden_SunSlash_NLA_Aligned.blend'));rig=bpy.data.objects['Armature'];out=[]
for f in [1,5,9,20,30,38,40,42,46,51]:
 bpy.context.scene.frame_set(f);out.append({'f':f,**{n:list(rig.matrix_world@rig.pose.bones['mixamorig:'+n].head) for n in ['Hips','LeftFoot','RightFoot']}})
print(json.dumps(out,indent=2))
