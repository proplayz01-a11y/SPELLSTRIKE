import bpy,json,pathlib
root=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE')
paths=[root/'SourceAssets/Stage3/SunboundWarden_Rig_HELMET_FIXED.blend',root/'Assets/Stage 3/Enemies/STAGE 3 ENEMY NO. 1/SunboundWarden_Rig.fbx',pathlib.Path('C:/Users/David/Downloads/STAGE 3 ENEMY NO 1 FINAL@Stable Sword Inward Slash.fbx'),pathlib.Path('C:/Users/David/Downloads/STAGE 3 ENEMY NO 1 FINAL@Sword And Shield Slash.fbx')]
out=[]
for p in paths:
 bpy.ops.wm.read_factory_settings(use_empty=True)
 if p.suffix=='.blend': bpy.ops.wm.open_mainfile(filepath=str(p))
 else: bpy.ops.import_scene.fbx(filepath=str(p))
 row={'file':str(p),'version':bpy.app.version_string,'fps':bpy.context.scene.render.fps,'objects':[],'actions':[]}
 for o in bpy.context.scene.objects:
  x={'name':o.name,'type':o.type,'scale':list(o.scale),'rotation':list(o.rotation_euler)}
  if o.type=='ARMATURE':x['bones']=[b.name for b in o.data.bones];x['rest']={b.name:[list(r) for r in b.matrix_local] for b in o.data.bones}
  row['objects'].append(x)
 for a in bpy.data.actions:row['actions'].append({'name':a.name,'range':list(a.frame_range),'slots':[s.identifier for s in a.slots]})
 out.append(row)
(root/'SourceAssets/Stage3/SunSlash_NLA/inspection.json').write_text(json.dumps(out,indent=2))
print('INSPECTION_DONE')
