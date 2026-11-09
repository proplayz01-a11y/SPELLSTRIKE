import bpy,bmesh,pathlib,json,math,random
from mathutils import Vector
OUT=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/ArtSource/Characters/StoneGolem/VolumetricStone_v001');bpy.ops.wm.open_mainfile(filepath=str(OUT/'StoneGolem_ActualStone_v001.blend'));sc=bpy.context.scene;rocks=bpy.data.collections['GOLEM_Actual_Stone_Pieces'];source=bpy.data.objects['REFERENCE_Source_Golem'];core=bpy.data.objects['Golem_Dark_Joint_Core'];core.data=bpy.data.objects['AUTHOR_Watertight_Fracture_Base'].data.copy();core.data.materials.clear();core.data.materials.append(bpy.data.materials['Stone_Deep_Crevices']);bm=bmesh.new();bm.from_mesh(core.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.normal_update()
for v in bm.verts:v.co-=v.normal*.035
bm.to_mesh(core.data);bm.free()
random.seed(77);tree=bpy.context.evaluated_depsgraph_get();srcme=source.data
from mathutils.bvhtree import BVHTree
srcme.calc_loop_triangles();bv=BVHTree.FromPolygons([v.co.copy() for v in srcme.vertices],[tuple(t.vertices) for t in srcme.loop_triangles],all_triangles=True)
for o in rocks.objects:
 if not o.name.startswith('Golem_Rock_'):continue
 c=sum((v.co for v in o.data.vertices),Vector())/len(o.data.vertices);p,n,idx,dist=bv.find_nearest(c);n=n.normalized();shift=n*random.uniform(.012,.024)
 for v in o.data.vertices:v.co+=shift
 # Solid non-metallic rough stone, not polished armor.
 for mat in o.data.materials:
  if mat and mat.use_nodes:mat.node_tree.nodes.get('Principled BSDF').inputs['Specular IOR Level'].default_value=.25
head=bpy.data.objects['Golem_Carved_Head'];head.data.materials[0].diffuse_color=(.105,.115,.098,1)
# Ground the new raised geometry and keep a 2m authoring height.
dg=bpy.context.evaluated_depsgraph_get();points=[]
for o in rocks.objects:
 e=o.evaluated_get(dg);m=e.to_mesh();points.extend(o.matrix_world@v.co for v in m.vertices);e.to_mesh_clear()
lo=min(v.z for v in points);hi=max(v.z for v in points);factor=2/(hi-lo)
for o in rocks.objects:o.location.z-=lo;o.location*=factor;o.scale*=factor
# Actual geometric piece check and count (distinct stones remain editable).
qa=[]
for o in rocks.objects:
 bm=bmesh.new();bm.from_mesh(o.data);qa.append({'name':o.name,'base_faces':len(bm.faces),'boundary_edges':sum(e.is_boundary for e in bm.edges),'nonmanifold_edges':sum(not e.is_manifold for e in bm.edges)});bm.free()
(OUT/'geometry_qa.json').write_text(json.dumps({'height_normalization':factor,'pieces':qa},indent=2))
review=bpy.data.collections.new('REVIEW_Stone_Studio');sc.collection.children.link(review)
def light(name,pos,energy,size):
 d=bpy.data.lights.new(name,'AREA');d.energy=energy;d.shape='DISK';d.size=size;o=bpy.data.objects.new(name,d);review.objects.link(o);o.location=pos;o.rotation_euler=(Vector((0,0,1))-o.location).to_track_quat('-Z','Y').to_euler()
light('Key',(3,-4,4.5),750,3);light('Fill',(-3,-1.7,2.7),380,3);light('Rim',(1,3,3.5),800,2)
sc.world.use_nodes=True;bg=sc.world.node_tree.nodes.get('Background');bg.inputs['Color'].default_value=(.13,.15,.17,1);bg.inputs['Strength'].default_value=.3
floor=bpy.data.materials.new('Review_Floor');floor.diffuse_color=(.035,.038,.044,1);floor.use_nodes=True;floor.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(.035,.038,.044,1);floor.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.85;bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.004));o=bpy.context.object;o.name='Review_Floor'
for c in list(o.users_collection):c.objects.unlink(o)
review.objects.link(o);o.data.materials.append(floor)
cam=sc.camera;cam.location=(3,-5,2);cam.rotation_euler=(Vector((0,0,1))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=2.85;sc.render.engine='CYCLES';sc.cycles.samples=40;sc.cycles.use_denoising=True;sc.render.resolution_x=1100;sc.render.resolution_y=1100;sc.view_settings.view_transform='AgX'
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':area.spaces.active.shading.type='MATERIAL';area.spaces.active.shading.use_scene_lights=True;area.spaces.active.shading.use_scene_world=True
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'StoneGolem_ActualStone_v002.blend'))
for name,pos,target,scale in [('ThreeQuarter',(3,-5,2),(0,0,1),2.85),('Front',(0,-6,1),(0,0,1),2.85),('Chest_Close',(1.2,-3,1.9),(0,0,1.36),1.18)]:
 cam.location=pos;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=scale;sc.render.filepath=str(OUT/('Stone_'+name+'.png'));bpy.ops.render.render(write_still=True)
print('STONE_MATERIAL_REVIEW_READY')
