import bpy,bmesh,pathlib,json,math,hashlib
from mathutils import Vector
OUT=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/ArtSource/Stage3/Node1/Character/NewMesh_v001');bpy.ops.wm.open_mainfile(filepath=str(OUT/'SunboundWarden_NewMesh_v001.blend'));sc=bpy.context.scene;dg=bpy.context.evaluated_depsgraph_get();cols=[bpy.data.collections[n] for n in ['LAYER_1_Anatomical_Core','LAYER_2_Armor','LAYER_2_Cloth','LAYER_2_Armor_Detail']];models=[o for c in cols for o in c.objects];export=bpy.data.collections.new('EXPORT_Baked_Character');sc.collection.children.link(export);copies=[]
for o in models:
 me=bpy.data.meshes.new_from_object(o.evaluated_get(dg),preserve_all_data_layers=True,depsgraph=dg);c=bpy.data.objects.new('BakePart',me);export.objects.link(c);c.matrix_world=o.matrix_world.copy();copies.append(c)
bpy.ops.object.select_all(action='DESELECT')
for o in copies:o.select_set(True)
bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();baked=bpy.context.object;baked.name='SunboundWarden_NewMesh_Baked';bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
bm=bmesh.new();bm.from_mesh(baked.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(baked.data);bm.free()
# One complete new UV atlas; do not copy source UVs.
if not baked.data.uv_layers:baked.data.uv_layers.new(name='WardenAtlasUV')
baked.data.uv_layers.active.name='WardenAtlasUV';bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=1.151917,island_margin=.003,area_weight=.2,scale_to_bounds=True);bpy.ops.object.mode_set(mode='OBJECT')
materials=[];states=[]
for i,m in enumerate(baked.data.materials):
 new=m.copy();new.name=m.name+'_BakeOnly';baked.data.materials[i]=new;materials.append(new);nt=new.node_tree;out=nt.nodes.get('Material Output');bsdf=nt.nodes.get('Principled BSDF');emit=nt.nodes.new('ShaderNodeEmission');nt.links.new(emit.outputs[0],out.inputs['Surface']);bc=bsdf.inputs['Base Color'];states.append((new,out,bsdf,emit))
 if bc.is_linked:nt.links.new(bc.links[0].from_socket,emit.inputs['Color'])
 else:emit.inputs['Color'].default_value=bc.default_value
 imageNode=nt.nodes.new('ShaderNodeTexImage');imageNode.name='AtlasBakeTarget';nt.nodes.active=imageNode
sc.render.engine='CYCLES';sc.cycles.samples=8;sc.render.bake.margin=8;sc.render.bake.use_clear=True
for o in models:o.hide_render=True
maps=[]
def bake_image(name,kind,space):
 im=bpy.data.images.new(name,width=2048,height=2048,alpha=False);im.colorspace_settings.name=space
 for m in materials:m.node_tree.nodes['AtlasBakeTarget'].image=im;m.node_tree.nodes.active=m.node_tree.nodes['AtlasBakeTarget']
 bpy.context.view_layer.objects.active=baked;bpy.ops.object.bake(type=kind,use_clear=True);im.filepath_raw=str(OUT/(name+'.png'));im.file_format='PNG';im.save();im.pack();maps.append(im);return im
albedo=bake_image('Warden_BaseColor','EMIT','sRGB')
for m,out,bsdf,emit in states:
 nt=m.node_tree
 for link in list(emit.inputs['Color'].links):nt.links.remove(link)
 emit.inputs['Color'].default_value=(1,float(m.get('rough',.5)),float(m.get('metal',0)),1)
orm=bake_image('Warden_OcclusionRoughnessMetallic','EMIT','Non-Color')
for m,out,bsdf,emit in states:m.node_tree.links.new(bsdf.outputs['BSDF'],out.inputs['Surface'])
normal=bake_image('Warden_Normal','NORMAL','Non-Color')
mat=bpy.data.materials.new('Warden_Baked_PBR');mat.use_nodes=True;nt=mat.node_tree;p=nt.nodes.get('Principled BSDF')
color=nt.nodes.new('ShaderNodeTexImage');color.image=albedo;nt.links.new(color.outputs['Color'],p.inputs['Base Color'])
mr=nt.nodes.new('ShaderNodeTexImage');mr.image=orm;sep=nt.nodes.new('ShaderNodeSeparateColor');nt.links.new(mr.outputs['Color'],sep.inputs['Color']);nt.links.new(sep.outputs['Green'],p.inputs['Roughness']);nt.links.new(sep.outputs['Blue'],p.inputs['Metallic'])
nm=nt.nodes.new('ShaderNodeTexImage');nm.image=normal;normalnode=nt.nodes.new('ShaderNodeNormalMap');nt.links.new(nm.outputs['Color'],normalnode.inputs['Color']);nt.links.new(normalnode.outputs['Normal'],p.inputs['Normal'])
baked.data.materials.clear();baked.data.materials.append(mat)
for poly in baked.data.polygons:poly.material_index=0
baked.data.calc_loop_triangles();coords=[baked.matrix_world@v.co for v in baked.data.vertices];qa={'blender':bpy.app.version_string,'authoring_objects':len(models),'export_mesh_count':1,'export_material_count':1,'triangles':len(baked.data.loop_triangles),'vertices':len(baked.data.vertices),'bounds':[[min(v[i] for v in coords) for i in range(3)],[max(v[i] for v in coords) for i in range(3)]],'height':max(v.z for v in coords)-min(v.z for v in coords),'texture_sizes':[list(i.size) for i in maps],'source_mesh_or_texture_reused':False,'rigged':False,'finite_coordinates':all(math.isfinite(v) for p in coords for v in p),'original_source_sha256':hashlib.sha256(pathlib.Path('C:/Users/David/OneDrive/Desktop/STAGE 3 CHARACTER NO. 1.blend').read_bytes()).hexdigest()}
# Export only the new baked character; no floor, cameras or native duplicate meshes.
bpy.ops.object.select_all(action='DESELECT');baked.select_set(True);bpy.context.view_layer.objects.active=baked
bpy.ops.export_scene.gltf(filepath=str(OUT/'SunboundWarden_NewMesh_v001.glb'),export_format='GLB',use_selection=True,export_animations=False,export_apply=True,export_yup=True)
# Native editable meshes remain visible. Baked export copy is stored hidden.
for o in models:o.hide_render=False
baked.hide_render=True;baked.hide_set(True);export.hide_render=True
notes=bpy.data.texts.get('README_New_Character');notes.write('\nEXPORT_Baked_Character holds the consolidated mesh with newly unwrapped UVs and packed 2048px base color, roughness/metallic and normal atlases. Enable that collection and hide native layers to inspect it. Original maps were not copied.\n')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'SunboundWarden_NewMesh_v001.blend'));(OUT/'verification.json').write_text(json.dumps(qa,indent=2));print('BAKED_EXPORTED',qa)
