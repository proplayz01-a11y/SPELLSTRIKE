import bpy,json,pathlib,math
from mathutils import Vector
R=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE'); O=R/'SourceAssets/Stage3/SunSlash_NLA'
bpy.ops.wm.open_mainfile(filepath=str(R/'SourceAssets/Stage3/SunboundWarden_Rig_HELMET_FIXED.blend'))
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE'); rig.animation_data_clear()
base={b.name:b.matrix_local.copy() for b in rig.data.bones}
report={}
for tag,fn in [('Inward','Stable Sword Inward Slash'),('Slash','Sword And Shield Slash')]:
 before=set(bpy.data.objects)
 bpy.ops.import_scene.fbx(filepath='C:/Users/David/Downloads/STAGE 3 ENEMY NO 1 FINAL@'+fn+'.fbx')
 src=next(o for o in set(bpy.data.objects)-before if o.type=='ARMATURE')
 a=src.animation_data.action;a.name='SOURCE_'+tag;a.use_fake_user=True
 report[tag]={'max_rest_difference':max(abs(base[b.name][i][j]-b.matrix_local[i][j]) for b in src.data.bones for i in range(4) for j in range(4)),'bones_match':set(base)==set(b.name for b in src.data.bones)}
 bpy.data.objects.remove(src,do_unlink=True)
rig.animation_data_create()
sc=bpy.context.scene;sc.render.engine='BLENDER_WORKBENCH';sc.render.resolution_x=600;sc.render.resolution_y=650;sc.render.resolution_percentage=100
sc.world=bpy.data.worlds.new('PreviewWorld');sc.world.color=(0.15,0.15,0.15);sc.display.shading.light='STUDIO';sc.display.shading.color_type='SINGLE';sc.display.shading.single_color=(0.65,0.48,0.2);sc.display.shading.show_shadows=True;sc.display.shading.show_cavity=True
bpy.ops.object.camera_add(location=(3,-5,2.6));cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,1))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=3.2;sc.camera=cam
for tag,frames in [('Inward',[1,6,12,18,24,30,36,42,48]),('Slash',[1,12,24,36,48,60,72,90,107])]:
 rig.animation_data.action=bpy.data.actions['SOURCE_'+tag];rig.animation_data.action_slot=rig.animation_data.action.slots[0]
 report[tag]['samples']=[]
 for f in frames:
  sc.frame_set(f)
  report[tag]['samples'].append({'frame':f,'hips':list(rig.pose.bones['mixamorig:Hips'].location) if 'mixamorig:Hips' in rig.pose.bones else [(b.name,list(b.location)) for b in rig.pose.bones if 'Hips' in b.name]})
  sc.render.filepath=str(O/(tag+'_%03d.png'%f));bpy.ops.render.render(write_still=True)
(O/'motion_inspection.json').write_text(json.dumps(report,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(O/'SourceInspection.blend'))

