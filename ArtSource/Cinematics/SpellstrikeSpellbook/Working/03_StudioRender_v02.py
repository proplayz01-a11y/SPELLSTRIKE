import bpy, os, math, importlib.util, json
from mathutils import Vector
WORK=r"C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\SpellstrikeSpellbook\Working"
FILE=os.path.join(WORK,"Spellstrike_PhysicalSpellbook_Model_v02.blend")
QA=os.path.join(WORK,"QA")
os.makedirs(QA,exist_ok=True)
assert os.path.normcase(bpy.data.filepath)==os.path.normcase(FILE)
assert bpy.data.objects.get("CLASP | Cobalt sapphire gemstone")
spec=importlib.util.spec_from_file_location("SpellbookTools_v02",os.path.join(WORK,"SpellbookTools_v02.py"))
T=importlib.util.module_from_spec(spec);spec.loader.exec_module(T)
coll=bpy.data.collections["24 | Studio for QA Renders"]
# A failed setup can leave unsaved QA-only objects. Rebuild only this collection.
for stale in list(coll.objects):
    assert stale.name.startswith("QA ONLY |") or stale.name.startswith("QA |"), "Unknown object in QA studio collection"
    bpy.data.objects.remove(stale, do_unlink=True)
root=bpy.data.objects["SPELLSTRIKE | FULL PHYSICAL BOOK ROOT"]
reference=bpy.data.objects["REFERENCE | Closed, spine, clasp, page geometry"]
reference.hide_render=True
floorMat=T.material("SSB | Studio charcoal backdrop",(.014,.017,.022),metal=.0,rough=.83)
floor=T.cube("QA ONLY | Matte studio tabletop",(0,0,-.53),(200,200,.065),floorMat,coll,None,.022)
floor.hide_select=True
def aim(obj,goal):
    vec=Vector(goal)-obj.location
    obj.rotation_euler=vec.to_track_quat("-Z","Y").to_euler()
camera=bpy.data.cameras.new("QA | Hero book camera")
cam=bpy.data.objects.new("QA | Hero camera",camera);coll.objects.link(cam)
cam.location=(-5.4,-6.0,7.6);aim(cam,(-.05,0,.05))
camera.type="ORTHO";camera.ortho_scale=6.85
bpy.context.scene.camera=cam
def light(name,position,energy,color,size):
    dat=bpy.data.lights.new(name,"AREA");dat.energy=energy;dat.color=color
    dat.shape="DISK";dat.size=size
    obj=bpy.data.objects.new(name,dat);coll.objects.link(obj)
    obj.location=position;aim(obj,(0,0,0))
    return obj
light("QA | Large warm softbox",(-4.0,-5.0,7.5),930,(1,.78,.52),5.0)
light("QA | Blue shadow fill",(4.0,-1.2,5.5),650,(.63,.77,1.),5.0)
light("QA | Antique gold rim light",(1.0,5.3,7.0),950,(1,.79,.49),4.5)
world=bpy.context.scene.world or bpy.data.worlds.new("SSB | Soft studio world")
bpy.context.scene.world=world;world.use_nodes=True
background=world.node_tree.nodes.get("Background")
background.inputs["Color"].default_value=(.038,.049,.064,1)
background.inputs["Strength"].default_value=.55
scene=bpy.context.scene
scene.render.engine="BLENDER_EEVEE"
scene.render.resolution_x=1280;scene.render.resolution_y=840;scene.render.resolution_percentage=85
scene.render.image_settings.file_format="PNG"
scene.render.filepath=os.path.join(QA,"Spellstrike_PhysicalSpellbook_v02_hero.png")
scene.render.film_transparent=False
scene.camera.data.lens=50
scene.view_settings.view_transform="AgX"
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=="VIEW_3D":
            space=area.spaces.active
            space.region_3d.view_location=Vector((.1,0,.3))
            space.region_3d.view_distance=9.5
            space.region_3d.view_rotation=cam.rotation_euler.to_quaternion()
            space.region_3d.view_perspective="ORTHO"
# Active target stays the physical book; non-exporting stage in separate collection.
for obj in bpy.context.selected_objects:obj.select_set(False)
root.select_set(True);bpy.context.view_layer.objects.active=root
bpy.ops.wm.save_as_mainfile(filepath=FILE)
print(json.dumps({"render_path":scene.render.filepath,"camera":list(cam.location),"scene_objects":len(scene.objects),"model_hinge_count":len([o for o in scene.objects if o.name.startswith("PAGE HINGE")]),"stage_collection":coll.name}))
