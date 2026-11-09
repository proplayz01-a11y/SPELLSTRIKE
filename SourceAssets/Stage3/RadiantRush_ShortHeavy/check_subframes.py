import bpy,pathlib,json
O=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/SourceAssets/Stage3/RadiantRush_ShortHeavy')
poses={};out={}
for tag,fn in [('nla','SunboundWarden_RadiantRush_ShortHeavy_NLA.blend'),('bake','SunboundWarden_RadiantRush_ShortHeavy_Baked.blend'),('fbx','SunboundWarden_RadiantRush_ShortHeavy.fbx')]:
 if tag=='fbx':bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(O/fn),anim_offset=0)
 else:bpy.ops.wm.open_mainfile(filepath=str(O/fn))
 rig=bpy.data.objects['Armature'];sc=bpy.context.scene;poses[tag]={};out[tag]=[]
 for f in [17.5,18.5,19.5,20.5,33.5,34.5,35.5,36.5,67.5,68.5,69.5,70.5]:
  sc.frame_set(int(f),subframe=f%1);poses[tag][f]={b.name:rig.matrix_world@b.matrix for b in rig.pose.bones}
  if tag!='nla':
   diffs=[(max(abs(m[i][j]-poses['nla'][f][n][i][j]) for i in range(4) for j in range(4)),n) for n,m in poses[tag][f].items()]
   out[tag].append((f,max(diffs)))
 print(tag,out[tag])
(O/'subframe_diagnostic.json').write_text(json.dumps(out,indent=2))
