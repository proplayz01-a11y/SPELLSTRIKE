"""Preserve the supplied character; remove its arrow cluster and open the existing quiver."""
import bpy,bmesh,json,math,hashlib,struct
from pathlib import Path
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
P=Path(r'C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Stage3\Node2\Character')
SOURCE=Path(r'C:\Users\David\Downloads\fantasy hooded character 3d model.glb')
EXPECTED='92944cbf5cf4769de144769601f54c9f74ad97346dff41b9f4ab604ebce9a685'
assert hashlib.sha256(SOURCE.read_bytes()).hexdigest()==EXPECTED
bpy.ops.wm.open_mainfile(filepath=str(P/'Source_Inspection.blend'),load_ui=False)
# Restore the exact embedded source texture before saving a self-contained delivery.
raw=SOURCE.read_bytes();jsonlen=struct.unpack_from('<I',raw,12)[0];gltf=json.loads(raw[20:20+jsonlen]);binstart=20+jsonlen+8
image_info=gltf['images'][0];view=gltf['bufferViews'][image_info['bufferView']]
texture_bytes=raw[binstart+view.get('byteOffset',0):binstart+view.get('byteOffset',0)+view['byteLength']]
texture_path=P/'Source_BaseColor.jpg';texture_path.write_bytes(texture_bytes)
for image in bpy.data.images:
    if image.source=='FILE':
        if image.packed_file:image.unpack(method='REMOVE')
        image.filepath=str(texture_path);image.reload();image.pack()
original=next(o for o in bpy.context.scene.objects if o.type=='MESH')
src=original.data;world=original.matrix_world.copy();normal_matrix=world.to_3x3().inverted().transposed()
groups=json.loads((P/'welded_components.json').read_text())
quiver_group=next(g for g in groups if g['id']==9775)
quiver_faces=set(quiver_group['face_ids'])
arrows=[g for g in groups if g['bounds'][0][2]>.075 and g['bounds'][1][2]<.19 and g['bounds'][1][1]<-.115]
arrow_faces=set(i for g in arrows for i in g['face_ids'])
cap_faces=set(range(6543,6579));assert cap_faces<=quiver_faces
assert len(arrow_faces)==353 and len(quiver_faces)==314

height=1.90;zmin=-.4990234375;scale=height/.998046875
rotation=Matrix.Rotation(-math.pi/2,3,'Z')
def position(p):return Vector((p.y,-p.x,p.z-zmin))*scale
def normal(n):return (rotation @ normal_matrix @ n).normalized()

asset=bpy.data.collections.new('S3N2_DesertArcher');bpy.context.scene.collection.children.link(asset)
archive=bpy.data.collections.new('REFERENCE_Original_Unedited');bpy.context.scene.collection.children.link(archive)
for c in list(original.users_collection):c.objects.unlink(original)
archive.objects.link(original);archive.hide_render=True;archive.hide_viewport=True
root=bpy.data.objects.new('S3N2_DesertArcher_ROOT',None);asset.objects.link(root)

def subset(name,ids):
    ids=sorted(ids);mapping={};vertices=[];faces=[];corner_normals=[]
    for fi in ids:
        f=src.polygons[fi];face=[]
        for vi,li in zip(f.vertices,f.loop_indices):
            if vi not in mapping:mapping[vi]=len(vertices);vertices.append(position(world@src.vertices[vi].co))
            face.append(mapping[vi]);corner_normals.append(normal(src.corner_normals[li].vector))
        faces.append(face)
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],faces);mesh.update()
    for material in src.materials:mesh.materials.append(material)
    for uvsrc in src.uv_layers:
        uv=mesh.uv_layers.new(name=uvsrc.name)
        for newface,fi in zip(mesh.polygons,ids):
            for newli,oldli in zip(newface.loop_indices,src.polygons[fi].loop_indices):uv.data[newli].uv=uvsrc.data[oldli].uv
    for newface,fi in zip(mesh.polygons,ids):newface.material_index=src.polygons[fi].material_index;newface.use_smooth=src.polygons[fi].use_smooth
    mesh.normals_split_custom_set(corner_normals)
    ob=bpy.data.objects.new(name,mesh);asset.objects.link(ob);ob.parent=root
    return ob,ids

body,body_ids=subset('DesertArcher_Body',set(range(len(src.polygons)))-quiver_faces-arrow_faces)
quiver,qids=subset('DesertArcher_Quiver_Hollow',quiver_faces)
bm=bmesh.new();bm.from_mesh(quiver.data);bm.faces.ensure_lookup_table();layer=bm.faces.layers.int.new('original_face_id')
for face,fi in zip(bm.faces,qids):face[layer]=fi
bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-6)
bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
closed_before=sum(e.is_boundary for e in bm.edges)
bmesh.ops.delete(bm,geom=[f for f in bm.faces if f[layer] in cap_faces],context='FACES')
bmesh.ops.delete(bm,geom=[e for e in bm.edges if not e.link_faces],context='EDGES')
bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS')
# Remove tiny residual cap wedges that would make the opening boundary touch itself.
rim_cleanup=[]
for _ in range(8):
    bad=[v for v in bm.verts if sum(e.is_boundary for e in v.link_edges)>2]
    if not bad:break
    candidates={f for v in bad for f in v.link_faces if sum(e.is_boundary for e in f.edges)>=2}
    assert candidates, 'Opening boundary needs a manual review rather than a broad mesh repair'
    f=min(candidates,key=lambda f:f.calc_area());rim_cleanup.append(f[layer])
    bmesh.ops.delete(bm,geom=[f],context='FACES')
    bmesh.ops.delete(bm,geom=[e for e in bm.edges if not e.link_faces],context='EDGES')
    bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS')
assert not [v for v in bm.verts if sum(e.is_boundary for e in v.link_edges)>2]
opening_edges=sum(e.is_boundary for e in bm.edges)
bm.to_mesh(quiver.data);bm.free();quiver.data.update()
for poly in quiver.data.polygons:poly.use_smooth=True

def simple_material(name,color,roughness,metal=0):
    mat=bpy.data.materials.new(name);mat.use_nodes=True;p=mat.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=roughness;p.inputs['Metallic'].default_value=metal;mat.diffuse_color=(*color,1)
    return mat
inner=simple_material('Quiver_Interior_DarkLeather',(.035,.017,.009),.90)
rim=simple_material('Quiver_Rim_WornLeather',(.18,.105,.047),.80)
quiver.data.materials.append(inner);quiver.data.materials.append(rim)
solid=quiver.modifiers.new('Real_InnerWalls_6p7mm','SOLIDIFY');solid.thickness=.0035*scale;solid.offset=-1;solid.use_even_offset=True;solid.use_rim=True;solid.material_offset=1;solid.material_offset_rim=2

# Exact preservation checks for body vertex positions and UV corners.
max_uv_error=0.0
for newface,fi in zip(body.data.polygons,body_ids):
    for nl,ol in zip(newface.loop_indices,src.polygons[fi].loop_indices):
        max_uv_error=max(max_uv_error,(body.data.uv_layers[0].data[nl].uv-src.uv_layers[0].data[ol].uv).length)

mouth_source=Vector((-.019748263888888888,-.1457248263888889,.07033058449074074))
axis_source=Vector((.4140702106703271,-.23366247887413905,.8797429775802833))
mouth=position(mouth_source);axis=(rotation@axis_source).normalized()
mouthmarker=bpy.data.objects.new('Quiver_Opening',None);asset.objects.link(mouthmarker);mouthmarker.parent=root;mouthmarker.location=mouth;mouthmarker.rotation_euler=axis.to_track_quat('Z','Y').to_euler();mouthmarker.empty_display_size=.035
bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();ev=quiver.evaluated_get(dg);mesh=ev.to_mesh();mesh.calc_loop_triangles()
points=[ev.matrix_world@v.co for v in mesh.vertices];faces=[tuple(t.vertices) for t in mesh.loop_triangles];tree=BVHTree.FromPolygons(points,faces,all_triangles=True)
test=[];u=axis.cross(Vector((1,0,0))).normalized();v=axis.cross(u).normalized()
for a,b in [(0,0),(.012,0),(-.012,0),(0,.012),(0,-.012)]:
    origin=mouth+axis*.025+u*a+v*b;hit=tree.ray_cast(origin,-axis,1.0)
    test.append({'offset':[a,b],'first_hit_from_opening_m':hit[3]-.025 if hit[0] is not None else None})
bm=bmesh.new();bm.from_mesh(mesh);evaluated_boundary=sum(e.is_boundary for e in bm.edges);evaluated_nonmanifold=sum(not e.is_manifold for e in bm.edges);bm.free()
qtris=len(mesh.loop_triangles);ev.to_mesh_clear();body.data.calc_loop_triangles()
qa={'source_sha256':EXPECTED,'source_triangles':len(src.polygons),'source_arrow_components_removed':len(arrows),'source_arrow_triangles_removed':len(arrow_faces),'solid_cap_triangles_removed':len(cap_faces),'rim_cleanup_faces':rim_cleanup,'body_triangles':len(body.data.loop_triangles),'quiver_evaluated_triangles':qtris,'total_triangles':len(body.data.loop_triangles)+qtris,'character_height_m':height,'quiver_wall_thickness_m':solid.thickness,'body_uv_max_error':max_uv_error,'quiver_boundary_before_cap_removal':closed_before,'opening_boundary_edges':opening_edges,'evaluated_quiver_boundary_edges':evaluated_boundary,'evaluated_quiver_nonmanifold_edges':evaluated_nonmanifold,'cavity_rays':test,'rigged':False,'source_arrow_component_ids':[g['id'] for g in arrows]}
(P/'empty_quiver_qa.json').write_text(json.dumps(qa,indent=2));print('EMPTY_QUIVER_QA '+json.dumps(qa),flush=True)

sc=bpy.context.scene;sc.name='S3N2_DesertArcher_Review';center=Vector((0,0,.95))
for ob in sc.objects:
    if ob.type=='LIGHT':
        # Move lights to match the normalized, rotated, human-size model.
        if ob.name=='Key':ob.location=(2,-3,3);ob.data.energy=350
        elif ob.name=='Fill':ob.location=(-3,-1,2);ob.data.energy=200
        else:ob.location=(1,3,2);ob.data.energy=280
        ob.rotation_euler=(center-ob.location).to_track_quat('-Z','Y').to_euler();ob.data.size=3
sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.07,.07,.07,1)
cam=sc.camera;cam.location=(2.4,-4,2.3);cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=2.25
sc.render.resolution_x=1200;sc.render.resolution_y=1200;sc.cycles.samples=24
for o in bpy.context.selected_objects:o.select_set(False)
body.select_set(True);quiver.select_set(True);bpy.context.view_layer.objects.active=quiver
for image in bpy.data.images:
    if image.source=='FILE':image.pack()
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':area.spaces.active.region_3d.view_location=(0,0,1);area.spaces.active.region_3d.view_distance=3;area.spaces.active.shading.type='MATERIAL'
readme=bpy.data.texts.new('READ_ME_EmptyQuiver');readme.write('Source character matched by Tripo node ID to the supplied screenshot. Original detail/UVs retained. Seven arrow components removed; solid cap removed; inward Solidify creates real walls/rim/bottom. Active character is 1.90 m tall, feet at Z=0, front -Y. No rig or animation added. Hidden reference is unedited source; exclude it from exports. GLB contains only the active body and hollow quiver. See empty_quiver_qa.json and character report.\n')
bpy.ops.wm.save_as_mainfile(filepath=str(P/'S3N2_DesertArcher_EmptyQuiver_v001.blend'))
bpy.ops.export_scene.gltf(filepath=str(P/'S3N2_DesertArcher_EmptyQuiver_v001.glb'),export_format='GLB',use_selection=True,export_apply=True,export_animations=False)
assert hashlib.sha256(SOURCE.read_bytes()).hexdigest()==EXPECTED

def render(name,loc,target,scale):
    cam.location=loc;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=scale;sc.render.filepath=str(P/(name+'.png'));bpy.ops.render.render(write_still=True)
render('Character_Front',(0,-4,.95),center,2.25)
render('Character_ThreeQuarter',(2.4,-4,2.3),center,2.25)
render('Character_Back',(0,4,1),center,2.25)
render('Quiver_Open_Closeup',mouth+axis*.50+Vector((-.16,-.12,.03)),mouth-axis*.08,.56)
render('Quiver_Interior_Top',mouth+axis*.65,mouth-axis*.10,.34)
print('CHARACTER_AND_EMPTY_QUIVER_SAVED',flush=True)
