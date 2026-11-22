"""Roundtrip quality gate for Nevali v06 cosmetic repair."""
import bpy, json, zipfile, hashlib
from pathlib import Path
ROOT=Path.cwd()
src=ROOT/"ArtSource/Characters/Nevali/Original/base_basic_pbr.glb"
glb=ROOT/"ArtSource/Characters/Nevali/Working/Nevali_FaceHair_Repair_v06.glb"
blend=ROOT/"ArtSource/Characters/Nevali/Working/Nevali_FaceHair_Repair_v06.blend"
original_zip=Path.home()/"Downloads/bc32551b-3b3f-4f0f-8229-a901e8229a7a.zip"
with zipfile.ZipFile(original_zip) as z:
    untouched=hashlib.sha256(src.read_bytes()).hexdigest()==hashlib.sha256(
        z.read("base_basic_pbr.glb")).hexdigest()
assert untouched, "Original extracted GLB no longer matches ZIP!"
assert glb.is_file() and blend.is_file()
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(glb))
meshes=[o for o in bpy.data.objects if o.type=='MESH']
names={o.name for o in meshes}
brows=[o for o in meshes if o.name.startswith("NEVALI_EYEBROW_")]
body=[o for o in meshes if o.name.startswith("NEVALI__RODIN_ORIGINAL_BODY")]
assert len(brows)==2,(len(brows),names)
assert len(body)==1,(len(body),names)
assert all(len(o.data.vertices)>=20 for o in brows)
assert all(len(o.data.materials)>=1 for o in meshes)
assert len(body[0].data.vertices)>=28000
summary={"verified":True,"original_zip_pbr_glb_untouched":True,
         "blend_file":str(blend),"glb_file":str(glb),
         "mesh_objects":len(meshes),"eyebrows":len(brows),
         "original_body_mesh_vertices":len(body[0].data.vertices),
         "source_rig_present":any(o.type=='ARMATURE' for o in bpy.data.objects),
         "notes":"Prototype correction. Crown still has Rodin geometric irregularities, eye texture has dark rectangular projection artifacts. Rigging and deformation untested."}
report=ROOT/"Library/NevaliModelRepair_20261023/After_v06/ExportRoundtripQA.json"
report.write_text(json.dumps(summary,indent=2),encoding='utf8')
print("NEVALI_REPAIR_ROUNDTRIP_PASS",json.dumps(summary))
