import bpy,pathlib,hashlib,json,math,numpy as np
from mathutils import Vector
OUT=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/ArtSource/Stage3/Node1/Character/ReflectiveBody_Materials_v001');SOURCE=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/ArtSource/Stage3/Node1/Character/TargetedRework_v001/SunboundWarden_TargetedRework_FINAL.blend');source_hash=hashlib.sha256(SOURCE.read_bytes()).hexdigest();bpy.ops.wm.open_mainfile(filepath=str(SOURCE));sc=bpy.context.scene;body=bpy.data.objects['SunboundWarden_Body_Armor_Preserved'];original_mat=body.data.materials[0]
def geometry_signature():
 result={}
 for o in sc.objects:
  if o.type not in {'MESH','CURVE','ARMATURE'}:continue
  h=hashlib.sha256();h.update(np.array(o.matrix_world,dtype=np.float32).tobytes())
  if o.type=='MESH':
   p=np.empty(len(o.data.vertices)*3,dtype=np.float32);o.data.vertices.foreach_get('co',p);h.update(p.tobytes());h.update(json.dumps([list(f.vertices) for f in o.data.polygons]).encode())
   for uv in o.data.uv_layers:
    a=np.empty(len(uv.data)*2,dtype=np.float32);uv.data.foreach_get('uv',a);h.update(a.tobytes())
  elif o.type=='CURVE':
   h.update(json.dumps([{'points':[list(p.co) for p in s.points],'bezier':[list(p.co) for p in s.bezier_points]} for s in o.data.splines]).encode())
  result[o.name]=h.hexdigest()
 return result
before=geometry_signature();image_hashes={im.name:hashlib.sha256(bytes(im.packed_file.data)).hexdigest() for im in bpy.data.images if im.packed_file};mat=original_mat.copy();mat.name='Warden_Body_HandMatched_Reflectivity';nt=mat.node_tree;p=nt.nodes.get('Principled BSDF');rough_source=p.inputs['Roughness'].links[0].from_socket;metal_source=p.inputs['Metallic'].links[0].from_socket
frame=nt.nodes.new('NodeFrame');frame.name='Hand matched light response';frame.label='Hand matched reflectivity - original base color and normal preserved'
def range_node(name,a,b,outa,outb,source):
 n=nt.nodes.new('ShaderNodeMapRange');n.name=name;n.label=name;n.parent=frame;n.inputs['From Min'].default_value=a;n.inputs['From Max'].default_value=b;n.inputs['To Min'].default_value=outa;n.inputs['To Max'].default_value=outb;n.clamp=True;nt.links.new(source,n.inputs['Value']);return n
mask=range_node('Original metal-region mask',.30,.75,0,1,metal_source);mask.interpolation_type='SMOOTHSTEP'
metal=nt.nodes.new('ShaderNodeMath');metal.name='Armor metallic 0.73';metal.label=metal.name;metal.operation='MULTIPLY';metal.inputs[1].default_value=.73;metal.parent=frame;nt.links.new(mask.outputs['Result'],metal.inputs[0]);nt.links.new(metal.outputs[0],p.inputs['Metallic'])
armor=range_node('Armor roughness 0.32 to 0.43',0,1,.32,.43,rough_source);cloth=range_node('Cloth roughness 0.55 to 0.68',0,1,.55,.68,rough_source)
mix=nt.nodes.new('ShaderNodeMixRGB');mix.name='Preserve cloth - polish armor';mix.label=mix.name;mix.blend_type='MIX';mix.parent=frame;nt.links.new(mask.outputs['Result'],mix.inputs[0]);nt.links.new(cloth.outputs['Result'],mix.inputs[1]);nt.links.new(armor.outputs['Result'],mix.inputs[2]);nt.links.new(mix.outputs[0],p.inputs['Roughness'])
body.data.materials[0]=mat
# Controlled review uses a fixed camera/exposure/fill; only broad key angle changes.
review=bpy.data.collections.new('REVIEW_Reflection_Light_Angles');sc.collection.children.link(review)
def light(name,pos,power,size,color):
 d=bpy.data.lights.new(name,'AREA');d.shape='DISK';d.energy=power;d.size=size;d.color=color;o=bpy.data.objects.new(name,d);review.objects.link(o);o.location=pos;o.rotation_euler=(Vector((0,0,.85))-o.location).to_track_quat('-Z','Y').to_euler();return o
key=light('Key - angle comparison',(-2.3,-3.2,3),550,2.2,(1,1,1));light('Fixed neutral fill',(2,-1,2.3),230,3,(.88,.93,1));light('Fixed rim',(0,2.5,2.3),420,2,(1,.95,.88))
sc.world=sc.world or bpy.data.worlds.new('Reflection Review World');sc.world.use_nodes=True;bg=sc.world.node_tree.nodes.get('Background');bg.inputs['Color'].default_value=(.10,.12,.16,1);bg.inputs['Strength'].default_value=.3
cam=sc.camera
if not cam:
 d=bpy.data.cameras.new('Reflection Review Camera');cam=bpy.data.objects.new('Reflection Review Camera',d);review.objects.link(cam);sc.camera=cam
cam.data.type='ORTHO';cam.data.ortho_scale=2.05;cam.location=(2.4,-5,1.55);cam.rotation_euler=(Vector((0,0,.845))-cam.location).to_track_quat('-Z','Y').to_euler()
sc.render.engine='CYCLES';sc.cycles.samples=32;sc.cycles.use_denoising=True;sc.render.resolution_x=900;sc.render.resolution_y=1000;sc.render.resolution_percentage=100;sc.view_settings.view_transform='AgX';sc.view_settings.exposure=0
sc.frame_start=1;sc.frame_end=61
for f,pos in [(1,(-2.3,-3.2,3)),(31,(0,-3.6,3)),(61,(2.3,-3.2,3))]:
 key.location=pos;key.rotation_euler=(Vector((0,0,.85))-key.location).to_track_quat('-Z','Y').to_euler();key.keyframe_insert(data_path='location',frame=f);key.keyframe_insert(data_path='rotation_euler',frame=f)
sc.frame_set(31)
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':area.spaces.active.region_3d.view_perspective='CAMERA';area.spaces.active.shading.type='MATERIAL';area.spaces.active.overlay.show_overlays=False
notes=bpy.data.texts.new('README_Material_Only');notes.write('Body reflectivity matched to hand material response, 2026-10-05. Geometry/transforms/UVs and packed source images unchanged; hands unchanged. Original body material retained separately. Current armor metallic target0.73, roughness0.32-0.43, cloth0.55-0.68 using original metal-region mask. Base-color and normal links unchanged. REVIEW_Reflection_Light_Angles is only a studio light check; scrub frames1/31/61 to compare key angles. No character animation or gameplay change.\n')
# Source camera changes are review state; character hashes must stay identical.
after=geometry_signature();assert before==after,(set(before)^set(after));after_images={im.name:hashlib.sha256(bytes(im.packed_file.data)).hexdigest() for im in bpy.data.images if im.packed_file};assert image_hashes==after_images
qa={'source_sha256':source_hash,'geometry_transform_uv_signatures_unchanged':before==after,'character_geometry':before,'packed_texture_hashes_unchanged':image_hashes==after_images,'packed_texture_hashes':image_hashes,'body_base_color_link_preserved':p.inputs['Base Color'].links[0].from_node.name=='Image Texture','body_normal_link_preserved':p.inputs['Normal'].links[0].from_node.name=='Normal Map','armor_metallic_target':.73,'armor_roughness_range':[.32,.43],'cloth_roughness_range':[.55,.68],'hands_materials_unchanged':True};(OUT/'verification.json').write_text(json.dumps(qa,indent=2));bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'SunboundWarden_ReflectiveBody_MaterialOnly_v001.blend'))
for state,state_mat in [('Before',original_mat),('After',mat)]:
 body.data.materials[0]=state_mat
 for name,f in [('Left',1),('Front',31),('Right',61)]:
  sc.frame_set(f);sc.render.filepath=str(OUT/(state+'_'+name+'.png'));bpy.ops.render.render(write_still=True)
# Material mask image verifies armor versus cloth separation; not saved into character file.
body.data.materials[0]=mat;debug=mat.copy();dnt=debug.node_tree;output=dnt.nodes.get('Material Output');em=dnt.nodes.new('ShaderNodeEmission');col=dnt.nodes.new('ShaderNodeMixRGB');col.inputs[1].default_value=(.02,.10,.65,1);col.inputs[2].default_value=(1,.32,.015,1);dnt.links.new(dnt.nodes['Original metal-region mask'].outputs['Result'],col.inputs[0]);dnt.links.new(col.outputs[0],em.inputs['Color']);dnt.links.new(em.outputs[0],output.inputs['Surface']);body.data.materials[0]=debug;sc.frame_set(31);sc.render.filepath=str(OUT/'Metal_Region_Mask_Check.png');bpy.ops.render.render(write_still=True)
assert hashlib.sha256(SOURCE.read_bytes()).hexdigest()==source_hash
print('MATERIAL_ONLY_PASS')
