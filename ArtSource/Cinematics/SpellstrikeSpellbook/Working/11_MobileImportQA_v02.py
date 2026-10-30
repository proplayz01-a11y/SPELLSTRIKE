import bpy,json
from pathlib import Path
glb=Path(r"C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\SpellstrikeSpellbook\Exports\Spellstrike_PhysicalSpellbook_MobileLOD1_v02.glb")
out=Path(r"C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\SpellstrikeSpellbook\Working\QA\GLB_MobileImportQA_v02.json")
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(glb))
objs=list(bpy.context.scene.objects)
pages=[o for o in objs if "PAGE SHEET | " in o.name and o.type=="MESH"]
hinges=[o for o in objs if "PAGE HINGE | " in o.name]
covers=[o for o in objs if "HINGE | Front cover" in o.name or "HINGE | Back cover" in o.name]
textures=[i.name for i in bpy.data.images if i.source=="FILE"]
result={"pass":len(pages)==24 and len(hinges)==24 and len(covers)==2 and len(textures)>=4 and all(len(o.data.uv_layers)>=1 for o in pages),"pages":len(pages),"page_hinges":len(hinges),"cover_hinges":len(covers),"meshes":sum(o.type=="MESH" for o in objs),"embedded_textures":textures,"blender":bpy.app.version_string}
out.write_text(json.dumps(result,indent=2),encoding="utf-8")
print("MOBILE_IMPORT_QA",json.dumps(result))
assert result["pass"]
