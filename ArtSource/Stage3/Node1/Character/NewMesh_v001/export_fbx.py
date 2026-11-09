import bpy,pathlib,json
OUT=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/ArtSource/Stage3/Node1/Character/NewMesh_v001');bpy.ops.wm.open_mainfile(filepath=str(OUT/'SunboundWarden_NewMesh_v001.blend'));baked=bpy.data.objects['SunboundWarden_NewMesh_Baked'];baked.hide_set(False);baked.hide_render=False;bpy.data.collections['EXPORT_Baked_Character'].hide_render=False;bpy.ops.object.select_all(action='DESELECT');baked.select_set(True);bpy.context.view_layer.objects.active=baked
bpy.ops.export_scene.fbx(filepath=str(OUT/'SunboundWarden_NewMesh_v001.fbx'),use_selection=True,object_types={'MESH'},bake_anim=False,axis_forward='-Z',axis_up='Y',global_scale=1,apply_unit_scale=True,path_mode='COPY',embed_textures=True)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(OUT/'SunboundWarden_NewMesh_v001.fbx'),use_image_search=False);meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];tris=0;vs=[]
for o in meshes:o.data.calc_loop_triangles();tris+=len(o.data.loop_triangles);vs.extend(o.matrix_world@v.co for v in o.data.vertices)

for img in bpy.data.images:
 if img.type=='IMAGE':
  try: first_pixel=img.pixels[0]
  except Exception: pass
report={'mesh_count':len(meshes),'triangles':tris,'height_m':max(v.z for v in vs)-min(v.z for v in vs),'loaded_images':[{'name':i.name,'has_data':i.has_data,'size':list(i.size)} for i in bpy.data.images if i.type=='IMAGE']};(OUT/'fbx_roundtrip.json').write_text(json.dumps(report,indent=2));print(report)

