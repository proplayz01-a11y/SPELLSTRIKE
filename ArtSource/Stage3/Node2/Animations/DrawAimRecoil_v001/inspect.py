import bpy,pathlib,json,hashlib
R=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE');O=R/'ArtSource/Stage3/Node2/Animations/DrawAimRecoil_v001';out=[]
for tag,fn in [('Draw','Standing Draw Arrow'),('Recoil','Standing Aim Recoil')]:
 p=pathlib.Path('C:/Users/David/Downloads/S3N2_DesertArcher_Mixamo@'+fn+'.fbx');bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(p))
 rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
 row={'source':str(p),'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'blender':bpy.app.version_string,'fps':bpy.context.scene.render.fps,'rig':rig.name,'scale':list(rig.scale),'rotation':list(rig.rotation_euler),'bones':{b.name:{'parent':b.parent.name if b.parent else None,'matrix':[list(r) for r in b.matrix_local]} for b in rig.data.bones},'actions':[(a.name,list(a.frame_range),[s.identifier for s in a.slots]) for a in bpy.data.actions],'meshes':[{'name':o.name,'verts':len(o.data.vertices),'materials':[m.name for m in o.data.materials if m]} for o in bpy.context.scene.objects if o.type=='MESH'],'images':[{'name':i.name,'size':list(i.size),'path':i.filepath} for i in bpy.data.images]}
 out.append(row)
 bpy.ops.wm.save_as_mainfile(filepath=str(O/(tag+'_Inspection.blend')))
(O/'source_inspection.json').write_text(json.dumps(out,indent=2));print(json.dumps([{k:v for k,v in r.items() if k!='bones'} for r in out],indent=2))
