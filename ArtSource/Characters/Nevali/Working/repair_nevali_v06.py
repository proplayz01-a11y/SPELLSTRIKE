"""Nondestructive Rodin Nevali cosmetic repair.

Keep original unchanged. Remove selected disconnected, bar-like hair fragments
above scalp and add two editable contoured eyebrow ribbons. Produces Blend,
glTF and clear head close-up renders. No auto-rig or face reshaping.
"""
import bpy
import bmesh
import collections
import json
from pathlib import Path
from mathutils import Vector

ROOT=Path.cwd()
SOURCE=ROOT/"ArtSource/Characters/Nevali/Original/base_basic_pbr.glb"
OUT=ROOT/"ArtSource/Characters/Nevali/Working"
QA=ROOT/"Library/NevaliModelRepair_20261023/After_v06"
OUT.mkdir(parents=True,exist_ok=True);QA.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(SOURCE))
actor=next(o for o in bpy.data.objects if o.type=="MESH")
actor.name="NEVALI__RODIN_ORIGINAL_BODY__FACE_HAIR_DRESS"
mesh=actor.data
total_before=len(mesh.vertices)

# The Rodin reconstruction has >4000 independent islands inside one mesh;
# identify only top-of-head horizontal ribbons (not large scalp, ribbon bows).
parent=list(range(len(mesh.vertices)))
def root(i):
    while parent[i]!=i:
        parent[i]=parent[parent[i]];i=parent[i]
    return i
for ed in mesh.edges:
    a,b=root(ed.vertices[0]),root(ed.vertices[1])
    if a!=b:parent[b]=a
components=collections.defaultdict(list)
for i in range(len(parent)):components[root(i)].append(i)
remove=[]
island_report=[]
for ids in components.values():
    xs=[mesh.vertices[i].co.x for i in ids]
    ys=[mesh.vertices[i].co.y for i in ids]
    zs=[mesh.vertices[i].co.z for i in ids]
    xx=max(xs)-min(xs);yy=max(ys)-min(ys);zz=max(zs)-min(zs)
    # Flat disconnected horizontal plate slivers on the outer scalp
    # Silhouette. Limit to <200 vertices and the upper 9 cm of the model.
    # If a sliver is part of a connected brow/fringe or bow, this keeps it.
    flat_bar=(min(zs)>1.80 and max(zs)<1.897 and
              max(ys)<0.15 and len(ids)<170 and
              xx>0.05 and yy<0.14 and zz<0.065)
    if flat_bar:
        remove.extend(ids)
        island_report.append({
            "vertices":len(ids),"x":(round(min(xs),3),round(max(xs),3)),
            "y":(round(min(ys),3),round(max(ys),3)),
            "z":(round(min(zs),3),round(max(zs),3))})

# Instead of creating scalp holes by deleting slivers, bury these
# disconnected hair islands slightly into the existing hair volume.
# This preserves all topology and the existing scalp / bow silhouette.
bm=bmesh.new();bm.from_mesh(mesh);bm.verts.ensure_lookup_table()
for i in remove:
    v=bm.verts[i]
    v.co.y+=.060
    v.co.z-=.020
    v.co.x*=.91
bm.to_mesh(mesh);bm.free();mesh.update()
print("HAIR_REPOSITION",len(island_report),"islands,",len(remove),"vertices.")

# Conform eyebrows to the *actual facial surface* using the imported model
# raycast, so the additions do not levitate in front of her forehead.
mat=bpy.data.materials.new("NEVALI_BROWS_SOFT_CHESTNUT_EDITABLE")
mat.diffuse_color=(.26,.13,.075,1)
mat.use_nodes=True
bs=mat.node_tree.nodes.get("Principled BSDF")
bs.inputs["Base Color"].default_value=(.26,.13,.075,1)
bs.inputs["Roughness"].default_value=.88

def front_depth(x,z):
    ok,location,normal,face=actor.ray_cast(Vector((x,-.6,z)),
                                             Vector((0,1,0)),distance=1.0)
    # Surfaces near the face are expected y ~ -0.24 to -0.28.
    if ok and -0.335<location.y<-0.18:
        return location.y-.001,True
    return -.247,False

brow_report=[]
for sign,side in ((-1,"RIGHT_ON_SCREEN"),(1,"LEFT_ON_SCREEN")):
    # Positive/negative halves: inner brow near nose, outer near temples.
    # Stylized arches in meters; keep thin and natural for a young child.
    xpoints=[.045,.055,.065,.075,.085,.096,.107]
    zpoints=[1.652,1.656,1.659,1.661,1.661,1.659,1.656]
    thickness=[.0015,.0028,.0040,.0045,.0040,.0028,.0015]
    vertices=[]
    cast_success=0
    for j,(ax,z,t) in enumerate(zip(xpoints,zpoints,thickness)):
        x=sign*ax
        dep,was_cast=front_depth(x,z)
        cast_success+=int(was_cast)
        # A light tapered, curved strip above the lash line.
        vertices.extend([(x,dep,z+t/2),(x,dep,z-t/2)])
    faces=[]
    for j in range(len(xpoints)-1):
        faces.append((2*j,2*j+1,2*j+3,2*j+2))
    # Two sided so it survives whichever direction the eye/face normal faces.
    # A thin closed volume exports reliably to glTF; avoid coplanar
    # duplicate faces that can produce invalid/nonmanifold primitives.
    front_faces=list(faces)
    count=len(vertices)
    vertices += [(x,y+.0015,z) for x,y,z in vertices]
    faces += [tuple(count+k for k in reversed(f)) for f in front_faces]
    faces += [(2*j,2*j+2,count+2*j+2,count+2*j) for j in range(len(xpoints)-1)]
    faces += [(2*j+1,count+2*j+1,count+2*j+3,2*j+3) for j in range(len(xpoints)-1)]
    faces += [(0,count, count+1,1)]
    end=2*(len(xpoints)-1)
    faces += [(end,end+1,count+end+1,count+end)]
    made=bpy.data.meshes.new("BROW_MESH_"+side)
    made.from_pydata(vertices,[],faces);made.update()
    ob=bpy.data.objects.new("NEVALI_EYEBROW_"+side+"__EDITABLE",made)
    bpy.context.scene.collection.objects.link(ob)
    made.materials.append(mat)
    brow_report.append({"name":ob.name,"vertices":len(vertices),
                        "triangles":len(faces)*2,
                        "face_ray_hits":cast_success})
    print("BROW",side,"surface hits",cast_success,"/",len(xpoints),
          "depth first",round(vertices[0][1],4))

scene=bpy.context.scene
scene.render.engine='BLENDER_EEVEE'
scene.render.resolution_x=1100;scene.render.resolution_y=950
scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.view_settings.view_transform='Standard'
scene.view_settings.look='Medium High Contrast'
world=bpy.data.worlds.new('QA_DiffuseStudio')
scene.world=world;world.use_nodes=True
world.node_tree.nodes['Background'].inputs['Color'].default_value=(.70,.72,.74,1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value=.55
for name,loc,power,size in [
    ('Front softbox',(3,-4,5),140,5),('Back softbox',(-2,2,4),110,5),
    ('Face fill',(0,-5,1.8),80,4)]:
    data=bpy.data.lights.new(name,'AREA')
    data.energy=power;data.shape='DISK';data.size=size
    obj=bpy.data.objects.new(name,data)
    scene.collection.objects.link(obj)
    obj.location=loc
    obj.rotation_euler=(Vector((0,0,1.55))-obj.location).to_track_quat('-Z','Y').to_euler()
cd=bpy.data.cameras.new("Nevali_REPAIR_QA_Ortho")
cam=bpy.data.objects.new("Nevali_REPAIR_QA_Ortho",cd)
scene.collection.objects.link(cam);scene.camera=cam;cd.type='ORTHO'

def render(name,at,target,scale):
    cam.location=at
    cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler()
    cd.ortho_scale=scale
    scene.render.filepath=str(QA/name)
    bpy.ops.render.render(write_still=True)
    print("RENDER",name)
render("face_after_front.png",(0,-2.5,1.65),(0,-.07,1.655),.59)
render("face_after_three_quarter.png",(.8,-2.1,1.78),(0,-.07,1.65),.62)
render("hair_after_top.png",(0,-1,3),(0,-.03,1.82),.63)
render("body_after_front.png",(0,-3.7,.95),(0,0,.95),2.45)

scene.render.filepath=''
save=OUT/"Nevali_FaceHair_Repair_v06.blend"
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(save),compress=True)

# Export ONLY actor and the two eyebrow meshes, not render camera/lights.
bpy.ops.object.select_all(action='DESELECT')
for o in bpy.data.objects:
    if o.type=='MESH':
        o.select_set(True)
glb=OUT/"Nevali_FaceHair_Repair_v06.glb"
bpy.ops.export_scene.gltf(filepath=str(glb),export_format='GLB',
                          use_selection=True,export_apply=True)
report={"source":str(SOURCE),"blend":str(save),"glb":str(glb),
        "original_source_untouched":True,
        "original_vertices":total_before,"post_cleanup_vertices":len(mesh.vertices),
        "repositioned_hair_fragment_count":len(island_report),
        "repositioned_vertex_count":len(remove),
        "repositioned_fragment_bounds":island_report,
        "eyebrows":brow_report,"rigged":False,"animated":False,
        "facial_texture_modified":False}
(QA/"RepairQA.json").write_text(json.dumps(report,indent=2))
print("REPAIR_DONE",json.dumps({k:v for k,v in report.items()
      if k in ("original_vertices","post_cleanup_vertices",
               "repositioned_hair_fragment_count","repositioned_vertex_count")}))
