import bpy,os,json,collections
ROOT=r"C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\SpellstrikeSpellbook"
SOURCE=os.path.join(ROOT,"Working","Spellstrike_PhysicalSpellbook_Model_v02.blend")
DEST=os.path.join(ROOT,"Exports","Spellstrike_PhysicalSpellbook_MobileLOD1_v02.glb")
assert os.path.normcase(bpy.data.filepath)==os.path.normcase(SOURCE)
assert not os.path.exists(DEST),"Mobile LOD exists"
cols=["20 | Spellbook Physical Components","21 | Working Hinges and Pages","22 | Cover Ornaments","23 | Clasp and Bookmark"]
excl=("AGE | Cover hairline", "AGE | Spine antique stitch","AGE | Compass age rivet", "COVER | Stipple gold fleck", "RUNE | Side", "RUNE | Upper/lower", "GUARD | Hammered brass rivet")
source=list(dict.fromkeys(o for c in cols for o in bpy.data.collections[c].objects if not o.name.startswith(excl)))
assert len([o for o in source if o.get("is_independent_page")])==24
coll=bpy.data.collections.new("99 | MOBILE EXPORT STAGING")
bpy.context.scene.collection.children.link(coll)
d={}
try:
    for o in source:
        q=o.copy()
        if o.data:q.data=o.data.copy()
        q.name="LOD1 | "+o.name
        coll.objects.link(q);d[o]=q
    for o,q in d.items():
        q.parent=d.get(o.parent)
        q.matrix_basis=o.matrix_basis.copy()
    bpy.context.view_layer.update()
    assert max((o.matrix_world.translation-q.matrix_world.translation).length for o,q in d.items())<.001
    for q in coll.objects:
        if q.type=="CURVE":
            q.data.bevel_resolution=1
            for sp in list(q.data.splines):
                if sp.type=="POLY" and len(sp.points)>60:
                    samples=[v.co.copy() for k,v in enumerate(sp.points) if k%2==0]
                    ns=q.data.splines.new("POLY");ns.points.add(len(samples)-1)
                    for pt,val in zip(ns.points,samples):pt.co=val
                    ns.use_cyclic_u=sp.use_cyclic_u
                    q.data.splines.remove(sp)
    bpy.ops.object.select_all(action="DESELECT")
    curves=[q for q in coll.objects if q.type=="CURVE"]
    for q in curves:q.select_set(True)
    if curves:
        bpy.context.view_layer.objects.active=curves[0]
        bpy.ops.object.convert(target="MESH")
    # Apply sculptural bevels so joining solids cannot strip their silhouettes.
    for q in list(coll.objects):
        if q.type!="MESH" or not q.modifiers:continue
        bpy.ops.object.select_all(action="DESELECT")
        q.select_set(True);bpy.context.view_layer.objects.active=q
        for mod in list(q.modifiers):
            try:bpy.ops.object.modifier_apply(modifier=mod.name)
            except RuntimeError:pass
    grouped=collections.defaultdict(list)
    for q in list(coll.objects):
        if q.type!="MESH" or "PAGE SHEET |" in q.name:continue
        key=(q.parent.as_pointer() if q.parent else 0,tuple(mat.name for mat in q.data.materials))
        grouped[key].append(q)
    merged_groups=0
    for key,objects in grouped.items():
        if len(objects)<2:continue
        bpy.ops.object.select_all(action="DESELECT")
        for obj in objects:obj.select_set(True)
        bpy.context.view_layer.objects.active=objects[0]
        bpy.ops.object.join()
        merged_groups+=1
    bpy.ops.object.select_all(action="DESELECT")
    for q in coll.objects:q.select_set(True)
    root=d[bpy.data.objects["SPELLSTRIKE | FULL PHYSICAL BOOK ROOT"]]
    bpy.context.view_layer.objects.active=root
    import contextlib,io
    log=io.StringIO()
    with contextlib.redirect_stdout(log):
        bpy.ops.export_scene.gltf(filepath=DEST, export_format="GLB",use_selection=True,export_extras=True,export_yup=True,export_materials="EXPORT",check_existing=False)
    assert os.path.isfile(DEST)
    print("MOBILE_GLTF",json.dumps({"file":DEST,"size":os.path.getsize(DEST),"mesh_objects":sum(q.type=="MESH" for q in coll.objects),"page_sheets":sum("PAGE SHEET |" in q.name and q.type=="MESH" for q in coll.objects),"merged_groups":merged_groups}))
finally:
    for q in list(coll.objects):bpy.data.objects.remove(q,do_unlink=True)
    bpy.data.collections.remove(coll)
    bpy.ops.object.select_all(action="DESELECT")
    source_root=bpy.data.objects.get("SPELLSTRIKE | FULL PHYSICAL BOOK ROOT")
    if source_root:source_root.select_set(True);bpy.context.view_layer.objects.active=source_root
print("SOURCE_INTACT",bpy.data.filepath,len(bpy.context.scene.objects))
