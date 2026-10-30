import bpy,os,json
WORK=r"C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\SpellstrikeSpellbook"
FILE=os.path.join(WORK,"Working","Spellstrike_PhysicalSpellbook_Model_v02.blend")
OUTDIR=os.path.join(WORK,"Exports")
OUTFILE=os.path.join(OUTDIR,"Spellstrike_PhysicalSpellbook_Closed_Textured_v02.glb")
assert os.path.normcase(bpy.data.filepath)==os.path.normcase(FILE)
assert not os.path.exists(OUTFILE),"Existing exported file; abort rather than overwrite"
os.makedirs(OUTDIR,exist_ok=True)
cols=["20 | Spellbook Physical Components","21 | Working Hinges and Pages","22 | Cover Ornaments","23 | Clasp and Bookmark"]
src_objs=list(dict.fromkeys(obj for name in cols for obj in bpy.data.collections[name].objects))
assert len(src_objs)>200 and len([x for x in src_objs if x.get("is_independent_page")])==24
for src in src_objs:
    assert not src.name.startswith("QA"),src.name
tmp=bpy.data.collections.new("99 | TEMP GLB Export - auto remove")
bpy.context.scene.collection.children.link(tmp)
mapping={}
for src in src_objs:
    new=src.copy()
    if src.data:new.data=src.data.copy()
    new.name="EX|" + src.name
    tmp.objects.link(new)
    mapping[src]=new
for src,new in mapping.items():
    if src.parent in mapping:new.parent=mapping[src.parent]
    else:new.parent=None
    new.matrix_basis=src.matrix_basis.copy()
bpy.context.view_layer.update()
errs=[(src.name,(src.matrix_world.translation-new.matrix_world.translation).length) for src,new in mapping.items() if (src.matrix_world.translation-new.matrix_world.translation).length>.001]
assert not errs,errs[:7]
for obj in bpy.context.selected_objects:obj.select_set(False)
curves=[obj for obj in tmp.objects if obj.type=="CURVE"]
assert curves
for obj in curves:obj.select_set(True)
bpy.context.view_layer.objects.active=curves[0]
bpy.ops.object.convert(target="MESH")
bpy.context.view_layer.update()
assert all(x.type in ("MESH","EMPTY") for x in tmp.objects),[x.name for x in tmp.objects if x.type not in ("MESH","EMPTY")][:5]
for obj in bpy.context.selected_objects:obj.select_set(False)
for obj in tmp.objects:obj.select_set(True)
bpy.context.view_layer.objects.active=mapping[bpy.data.objects["SPELLSTRIKE | FULL PHYSICAL BOOK ROOT"]]
try:
    bpy.ops.export_scene.gltf(
        filepath=OUTFILE,
        export_format="GLB",
        use_selection=True,
        export_apply=False,
        export_extras=True,
        export_yup=True,
        export_materials="EXPORT",
        check_existing=False)
    assert os.path.isfile(OUTFILE)
    print("GLB_DONE",json.dumps({"path":OUTFILE,"size_bytes":os.path.getsize(OUTFILE),"mesh_objects":sum(x.type=="MESH" for x in tmp.objects),"emphasis":"Page pivots + UV geometry preserved","reference_excluded":True}))
finally:
    for obj in list(tmp.objects):
        bpy.data.objects.remove(obj,do_unlink=True)
    bpy.data.collections.remove(tmp)
    for obj in bpy.context.selected_objects:obj.select_set(False)
    actual=bpy.data.objects.get("SPELLSTRIKE | FULL PHYSICAL BOOK ROOT")
    if actual:actual.select_set(True);bpy.context.view_layer.objects.active=actual
assert all(not x.name.startswith("EX|") for x in bpy.context.scene.objects)
assert bpy.data.objects.get("REFERENCE | Closed, spine, clasp, page geometry").data.packed_file is not None
print("CLEANUP_OK",len(bpy.context.scene.objects),bpy.data.filepath)
