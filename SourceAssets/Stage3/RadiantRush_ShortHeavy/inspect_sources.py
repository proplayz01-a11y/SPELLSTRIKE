import bpy, json, pathlib, hashlib
R=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE')
O=R/'SourceAssets/Stage3/RadiantRush_ShortHeavy'
A=R/'Assets/Stage 3/Enemies/STAGE 3 ENEMY NO. 1'
sources={
 'Crouch':pathlib.Path('C:/Users/David/Downloads/STAGE 3 ENEMY NO 1 FINAL@Crouched To Sprinting (1).fbx'),
 'Run':pathlib.Path('C:/Users/David/Downloads/STAGE 3 ENEMY NO 1 FINAL@Sword And Shield Run.fbx'),
 'CurrentRush':A/'Animations/SunboundWarden_RadiantRush.fbx',
 'Canonical':A/'SunboundWarden_Rig.fbx',
 'ReferenceMesh':R/'SourceAssets/Stage3/SunboundWarden_Rig_HELMET_FIXED.blend'}
out={}
for tag,p in sources.items():
 bpy.ops.wm.read_factory_settings(use_empty=True)
 if p.suffix=='.blend':bpy.ops.wm.open_mainfile(filepath=str(p))
 else:bpy.ops.import_scene.fbx(filepath=str(p),anim_offset=0)
 sc=bpy.context.scene
 row={'path':str(p),'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'fps':sc.render.fps,'objects':[],'actions':[]}
 for obj in sc.objects:
  d={'name':obj.name,'type':obj.type,'location':list(obj.location),'rotation':list(obj.rotation_euler),'scale':list(obj.scale)}
  if obj.type=='ARMATURE':
   d['bones']={b.name:{'parent':b.parent.name if b.parent else None,'matrix':[list(r) for r in b.matrix_local]} for b in obj.data.bones}
   d['samples']=[]
   if obj.animation_data and obj.animation_data.action:
    for f in range(int(obj.animation_data.action.frame_range.x),int(obj.animation_data.action.frame_range.y)+1):
     sc.frame_set(f)
     d['samples'].append({'f':f,'obj':list(obj.location),'objq':list(obj.matrix_world.to_quaternion()),'bones':{b.name:{'head':list(obj.matrix_world@b.head),'q':list(b.matrix.to_quaternion()),'loc':list(b.location)} for b in obj.pose.bones if any(x in b.name for x in ('Hips','Foot','Hand','Spine2'))}})
  row['objects'].append(d)
 for act in bpy.data.actions:row['actions'].append({'name':act.name,'range':list(act.frame_range),'slots':[s.identifier for s in act.slots]})
 out[tag]=row
 print(tag, 'FPS',row['fps'],'ACTIONS',row['actions'],'OBJECTS',[(x['name'],x['type']) for x in row['objects']])
(O/'source_inspection.json').write_text(json.dumps(out,indent=2))
files=list(A.rglob('*'))+[sources['Crouch'],sources['Run'],sources['ReferenceMesh']]
(O/'preservation_manifest.json').write_text(json.dumps({str(p):hashlib.sha256(p.read_bytes()).hexdigest() for p in files if p.is_file()},indent=2))
print('INSPECTION_DONE')
