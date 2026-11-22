"""Read-only Blender GLB roundtrip and reference source-integrity audit."""
import bpy
import hashlib
import json
import struct
import zipfile
from pathlib import Path
ROOT=Path.cwd()
ORIGINAL=ROOT/"ArtSource/Characters/Nevali/Original/base_basic_pbr.glb"
ARCHIVE=Path.home()/"Downloads/bc32551b-3b3f-4f0f-8229-a901e8229a7a.zip"
WORK=ROOT/"ArtSource/Characters/Nevali/Working"
GLB=WORK/"Nevali_FaceRebuilt_Reference_v08.glb"
BLEND=WORK/"Nevali_FaceRebuilt_Reference_v08.blend"
PNG=WORK/"Nevali_FacialReference_v08_RGBA.png"
with zipfile.ZipFile(ARCHIVE) as z:
    original_ok=hashlib.sha256(ORIGINAL.read_bytes()).digest()==hashlib.sha256(
        z.read("base_basic_pbr.glb")).digest()
assert original_ok,"Original Rodin asset changed!"
for p in [GLB,BLEND,PNG]:
    assert p.exists() and p.stat().st_size>100000,p

# glTF file metadata check: confirm final material still blends out the
# projection edges rather than exporting an opaque rectangular card.
data=GLB.read_bytes()
assert data[:4]==b'glTF'
length=struct.unpack_from('<I',data,12)[0]
gltf=json.loads(data[20:20+length].decode('utf-8'))
mat_modes=[(m.get("name",""),m.get("alphaMode","OPAQUE"))
           for m in gltf.get('materials',[])]
assert any("FACE_FROM_APPROVED" in name and mode in ("BLEND","MASK")
           for name,mode in mat_modes),mat_modes

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(GLB))
meshes=[o for o in bpy.data.objects if o.type=='MESH']
body=[o for o in meshes if o.name.startswith("NEVALI__RODIN_ORIGINAL_BODY")]
face=[o for o in meshes if o.name.startswith("NEVALI_FACE_EYES_NOSE_MOUTH")]
assert len(body)==1 and len(face)==1,[(o.name,len(o.data.vertices)) for o in meshes]
assert len(face[0].data.vertices)>2500
assert len(body[0].data.vertices)>28000
assert all(m is not None for o in meshes for m in o.data.materials)
report={
    "source_pbr_unchanged":original_ok,
    "result_blend":str(BLEND),
    "result_glb":str(GLB),
    "imported_meshes":[(o.name,len(o.data.vertices),len(o.data.polygons)) for o in meshes],
    "gltf_alpha_modes":mat_modes,
    "material_slots_complete":True,
    "rigged":any(o.type=='ARMATURE' for o in bpy.data.objects),
    "status":"PROTOTYPE_PASS_NOT_RIGGED",
    "known_issue":"Projection is for appearance only; crown strips and profile need manual 3D repair"
}
out=ROOT/"Library/NevaliModelRepair_20261023/After_v08/ExportRoundtripQA.json"
out.write_text(json.dumps(report,indent=2),encoding='utf8')
print("NEVALI_V08_ROUNDTRIP_PASS",json.dumps(report))
