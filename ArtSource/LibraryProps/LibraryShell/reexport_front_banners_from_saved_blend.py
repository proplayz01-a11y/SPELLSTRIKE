"""Synchronize banner-facing StudyArchive GLBs from saved live scene.

Only study-zone and full-library artifacts are changed. Backups are created
for both. Blender source has already been safely saved in the UI.
"""
import bpy
import json
import shutil
from pathlib import Path
from mathutils import Vector

P=Path(r'C:\Users\David\PROTOTYPE\SPELLSTRIKE')
source=P/'ArtSource/LibraryProps/LibraryShell/LibraryStudyArchive_OneBuilding_v01.blend'
output=P/'Assets/Cinematics/Storyboard3D/Environment/LibraryShell'
qa=P/'Library/StoryboardLibraryShell_20261022'
bpy.ops.wm.open_mainfile(filepath=str(source))
bpy.context.view_layer.update()
assert bpy.data.filepath.lower()==str(source).lower()
for side in (-1,1):
    root=bpy.data.objects[f'STUDY_BANNER_{side}']
    assert (root.matrix_world.translation-Vector((side*1.65,-11.15,2.50))).length<.002
    front=root.matrix_world.to_3x3()@Vector((0,-1,0))
    assert front.y>.99,(root.name,tuple(front))
    assert len(root.children)==6

reports=[]
def export_glb(file,collnames):
    selected=[]
    for name in collnames:
        col=bpy.data.collections[name]
        selected.extend(ob for ob in col.objects if ob.type not in ('LIGHT','CAMERA'))
    assert selected
    bpy.ops.object.select_all(action='DESELECT')
    for ob in selected:ob.select_set(True)
    path=output/file
    assert path.is_file()
    safety=qa/(path.stem+'_BEFORE_BannerFaceFix_20261023.glb')
    assert not safety.exists(),f'Do not overwrite an existing backup: {safety}'
    shutil.copy2(path,safety)
    bpy.ops.export_scene.gltf(filepath=str(path),export_format='GLB',use_selection=True,export_apply=True)
    assert path.stat().st_size>1_000_000
    reports.append({'output':str(path),'backup':str(safety),'bytes':path.stat().st_size,'selected_nodes':len(selected)})

export_glb('Library_StudyFurnitureGatewayAndDecor_v01.glb',[
    '04_FRONT_STUDY_DRESSING','05_GATEWAY_ARCH_AND_HERO'])
export_glb('LibraryStudyArchive_OneBuilding_v01.glb',[
    '02_ONE_BUILDING_WALLS_FLOORS','03_ARCHIVE_SHELF_LAYOUT',
    '04_FRONT_STUDY_DRESSING','05_GATEWAY_ARCH_AND_HERO'])
(qa/'StudyBannerFrontFacing_GLBSync_20261023.json').write_text(json.dumps(reports,indent=2),encoding='utf8')
print('STUDY_BANNERS_FRONT_GLB_EXPORT_OK',json.dumps(reports))
