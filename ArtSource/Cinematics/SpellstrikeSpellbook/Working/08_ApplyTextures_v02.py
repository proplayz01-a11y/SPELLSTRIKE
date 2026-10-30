import bpy,os,json
BASE=r"C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\SpellstrikeSpellbook\Working"
TARGET=os.path.join(BASE,"Spellstrike_PhysicalSpellbook_Model_v02.blend")
assert os.path.normcase(bpy.data.filepath)==os.path.normcase(TARGET)
texmap={
    "SSB | Aged midnight leather":"Leather_AncientMidnight_1024.png",
    "SSB | Worn leather inset":"Leather_InsetCharcoal_1024.png",
    "SSB | Faded dark-brown leather":"Leather_SpineBrown_1024.png",
    "SSB | Aged parchment":"Parchment_AgedBlank_1024.png",
    "SSB | Parchment warm alternate":"Parchment_AgedBlank_1024.png",
}
for matname,file in texmap.items():
    material=bpy.data.materials[matname]
    bs=material.node_tree.nodes.get("Principled BSDF")
    assert bs
    p=os.path.join(BASE,"Textures",file)
    assert os.path.isfile(p),p
    image=bpy.data.images.load(p,check_existing=True)
    image.pack()
    node=material.node_tree.nodes.get("SSB_ExportablePBRAlbedo") or material.node_tree.nodes.new("ShaderNodeTexImage")
    node.name="SSB_ExportablePBRAlbedo"
    node.image=image
    material.node_tree.links.new(node.outputs["Color"],bs.inputs["Base Color"])
    print("PBR_READY",matname,file,"image_packed",bool(image.packed_file))
# GPU/UI preview should still use the original 3D mesh and page hinges.
assert len([o for o in bpy.context.scene.objects if o.get("is_independent_page")])==24
assert not bpy.data.objects["HINGE | Front cover - rotate Y to open"].rotation_euler.y
bpy.context.scene.render.filepath=os.path.join(BASE,"QA","Spellstrike_PhysicalSpellbook_v02_hero_final_textured.png")
bpy.ops.wm.save_as_mainfile(filepath=TARGET)
print(json.dumps({"file":TARGET,"packed_images":len([i for i in bpy.data.images if i.packed_file]),"textures":list(texmap.values()),"ready_for_glb":True}))
