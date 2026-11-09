import bpy,pathlib,json
from mathutils import Vector
O=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/ArtSource/Stage3/Node2/Animations/DrawAimRecoil_v001');out={}
for tag in ['Draw','Recoil']:
 bpy.ops.wm.open_mainfile(filepath=str(O/(tag+'_Inspection.blend')));sc=bpy.context.scene;rig=bpy.data.objects['Armature'];a=rig.animation_data.action;end=int(a.frame_range[1]);sc.frame_set(1)
 points=[o.matrix_world@Vector(v) for o in sc.objects if o.type=='MESH' for v in o.bound_box];low=Vector(tuple(min(v[i] for v in points) for i in range(3)));high=Vector(tuple(max(v[i] for v in points) for i in range(3)));h=high.z-low.z;center=(low+high)/2
 print(tag,'bounds',list(low),list(high))
 sc.render.engine='BLENDER_WORKBENCH';sc.render.resolution_x=600;sc.render.resolution_y=600;sc.render.resolution_percentage=100;sc.world=bpy.data.worlds.new('PreviewWorld');sc.world.color=(.08,.08,.08);sc.display.shading.light='STUDIO';sc.display.shading.color_type='TEXTURE';sc.display.shading.show_shadows=True;sc.display.shading.show_cavity=True
 bpy.ops.object.camera_add(location=center+Vector((2.6,-4,1.5))*h);cam=bpy.context.object;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=h*1.5;sc.camera=cam
 for i in range(-4,5):
  for axis in [0,1]:
   bpy.ops.mesh.primitive_cube_add(size=1,location=(i*h/4 if axis==0 else 0,i*h/4 if axis==1 else 0,low.z-.01));o=bpy.context.object;o.name='PREVIEW_Grid';o.scale=(.005,h*2,.003) if axis==0 else (h*2,.005,.003)
 out[tag]=[]
 for f in range(1,end+1):
  sc.frame_set(f);out[tag].append({'frame':f,'joints':{b.name:{'pos':list(rig.matrix_world@b.head),'q':list(b.matrix.to_quaternion())} for b in rig.pose.bones}})
  sc.render.filepath=str(O/(tag+'_%03d.png'%f));bpy.ops.render.render(write_still=True)
 bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(O/(tag+'_Review.blend')))
(O/'source_motion.json').write_text(json.dumps(out,indent=2))
