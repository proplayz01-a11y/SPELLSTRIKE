import bpy, math, os, zipfile
from mathutils import Vector
from pathlib import Path
source=Path.home()/'Downloads'/'bc32551b-3b3f-4f0f-8229-a901e8229a7a.zip'
out=Path('Library/NevaliModelRepair_20261023/FaceBefore').resolve()
out.mkdir(exist_ok=True,parents=True)
with zipfile.ZipFile(source) as z:
    (out/'nevali_pbr_model.glb').write_bytes(z.read('base_basic_pbr.glb'))
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(out/'nevali_pbr_model.glb'))
meshobjs=[o for o in bpy.context.scene.objects if o.type=='MESH']
print('LOADED_MODELS',[(o.name,len(o.data.vertices),len(o.data.polygons)) for o in meshobjs])
verts=[o.matrix_world@v.co for o in meshobjs for v in o.data.vertices]
mi=Vector(tuple(min(v[i] for v in verts) for i in range(3)))
ma=Vector(tuple(max(v[i] for v in verts) for i in range(3)))
center=(mi+ma)/2
print('BLENDER_WORLD_BOUNDS',tuple(round(x,3) for x in mi),tuple(round(x,3) for x in ma))
scene=bpy.context.scene
scene.render.engine='BLENDER_EEVEE'
scene.render.resolution_x=1100
scene.render.resolution_y=950
scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.render.film_transparent=False
scene.render.image_settings.color_mode='RGBA'
scene.render.engine='BLENDER_EEVEE'
scene.world=bpy.data.worlds.new('Soft neutral studio')
scene.world.color=(.42,.42,.42)
scene.view_settings.view_transform='Standard'
scene.view_settings.look='Medium High Contrast'
scene.render.film_transparent=False
# Plain world background for an uncomplicated 3D model check.
scene.render.image_settings.color_mode='RGB'
scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.70,.72,.74,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.55
for name,loc,power,size in [
    ('Front softbox',(3,-4,5),140,5.0),
    ('Back softbox',(-2,2,4),110,5.0),
    ('Face fill',(0,-5,1.8),80,4.0)]:
    lightdat=bpy.data.lights.new(name,'AREA')
    lightdat.energy=power
    lightdat.shape='DISK'
    lightdat.size=size
    light=bpy.data.objects.new(name,lightdat)
    scene.collection.objects.link(light)
    light.location=loc
    light.rotation_euler=(center-light.location).to_track_quat('-Z','Y').to_euler()
camdat=bpy.data.cameras.new('QA_ORTHO')
cam=bpy.data.objects.new('QA_ORTHO',camdat)
scene.collection.objects.link(cam)
scene.camera=cam
camdat.type='ORTHO'
camdat.lens=35
def render(name,pos,target,scale):
    cam.location=pos
    cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
    camdat.ortho_scale=scale
    scene.render.filepath=str(out/name)
    bpy.ops.render.render(write_still=True)
    print('RENDERED',name)
h=(ma.z-mi.z)
render('face_close_front.png',Vector((0,-2.5,1.65)),Vector((0,-.07,1.655)),.59)
render('face_close_angle.png',Vector((.8,-2.1,1.78)),Vector((0,-.07,1.65)),.62)
render('hairline_top.png',Vector((0,-1,3.0)),Vector((0,-.03,1.82)),.63)
