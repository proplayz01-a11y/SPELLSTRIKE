import bpy,json,os
from pathlib import Path
glb=Path(r"C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\SpellstrikeSpellbook\Exports\Spellstrike_PhysicalSpellbook_Closed_Textured_v02.glb")
report=Path(r"C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\SpellstrikeSpellbook\Working\QA\GLB_ImportQA_v02.json")
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(glb))
objs=list(bpy.context.scene.objects)
pages=[o for o in objs if "PAGE SHEET | " in o.name and o.type=="MESH"]
hinges=[o for o in objs if "PAGE HINGE | " in o.name]
covers=[o for o in objs if "HINGE | Front cover" in o.name or "HINGE | Back cover" in o.name]
uv=[len(o.data.uv_layers) for o in pages]
mats=len(bpy.data.materials)
textures=[i.name for i in bpy.data.images if i.source=="FILE"]
ok=len(pages)==24 and len(hinges)==24 and len(covers)==2 and all(n>=1 for n in uv) and len(textures)>=4
info={"passed":ok,"blender":bpy.app.version_string,"page_objects":len(pages),"page_hinges":len(hinges),"cover_hinges":len(covers),"materials":mats,"image_count":len(textures),"images":textures,"mesh_objects":len([o for o in objs if o.type=="MESH"])}
report.write_text(json.dumps(info,indent=2),encoding="utf-8")
print("GLB_IMPORT_QA",json.dumps(info))
assert ok
