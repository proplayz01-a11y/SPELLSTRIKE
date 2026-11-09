"""Re-export changed StudyArchive composition without running the builder.

Run headless in Blender against the SAVED source .blend. Only the study/landmark
GLB and the complete one-building review GLB are updated, using all currently
saved edits (not a rebuild). Original export revisions are backed up to QA.
"""
import bpy
import json
import math
import shutil
from pathlib import Path
from mathutils import Vector

P=Path(r'C:\Users\David\PROTOTYPE\SPELLSTRIKE')
SOURCE=P/'ArtSource/LibraryProps/LibraryShell/LibraryStudyArchive_OneBuilding_v01.blend'
EXPORT=P/'Assets/Cinematics/Storyboard3D/Environment/LibraryShell'
QA=P/'Library/StoryboardLibraryShell_20261022'
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
table=bpy.data.objects['STUDY_ScholarSideTable']
globe=bpy.data.objects['STUDY_AntiqueCelestialGlobe']
bpy.context.view_layer.update()
assert globe.parent is table, 'Globe not grouped under table'
assert abs(math.degrees(table.rotation_euler.z)+90)<.001, 'Table is not at -90 degrees Z'
assert (table.matrix_world.translation-Vector((5.32,-7.80,0))).length<.001
assert (globe.matrix_world.translation-Vector((5.34,-8.11,.85))).length<.001

def export_asset(filename,collections):
    all_objs=[]
    for name in collections:
        c=bpy.data.collections.get(name)
        assert c is not None, ('Collection not found',name)
        all_objs.extend(list(c.objects))
    bpy.ops.object.select_all(action='DESELECT')
    for obj in all_objs:
        if obj.type not in ('LIGHT','CAMERA'):
            obj.select_set(True)
    path=EXPORT/filename
    assert path.exists(), ('Existing GLB expected',path)
    backup=QA/(path.stem+'_BEFORE_CloseToShelf_20261023.glb')
    if not backup.exists():
        shutil.copy2(path,backup)
    bpy.ops.export_scene.gltf(filepath=str(path),export_format='GLB',
                              use_selection=True,export_apply=True)
    assert path.stat().st_size>1000000, ('Unexpectedly tiny GLB',path)
    return {
        'file':str(path),
        'backup':str(backup),
        'bytes':path.stat().st_size,
        'mesh_nodes_selected':sum(o.type=='MESH' for o in all_objs),
    }

study=export_asset('Library_StudyFurnitureGatewayAndDecor_v01.glb',[
    '04_FRONT_STUDY_DRESSING','05_GATEWAY_ARCH_AND_HERO'])
all_building=export_asset('LibraryStudyArchive_OneBuilding_v01.glb',[
    '02_ONE_BUILDING_WALLS_FLOORS',
    '03_ARCHIVE_SHELF_LAYOUT',
    '04_FRONT_STUDY_DRESSING',
    '05_GATEWAY_ARCH_AND_HERO'])

report={
    'saved_blend':str(SOURCE),
    'table_world_position':[round(float(v),4) for v in table.matrix_world.translation],
    'table_z_degrees':math.degrees(table.rotation_euler.z),
    'globe_world_position':[round(float(v),4) for v in globe.matrix_world.translation],
    'globe_parent':globe.parent.name,
    'study_zone_glb':study,
    'complete_glb':all_building,
    'other_glbs_unchanged':True,
    'unity_scene_files_unchanged':True
}
(QA/'ScholarCornerCloseToShelf_GLBSynced.json').write_text(json.dumps(report,indent=2))
print('STUDYARCHIVE_GLB_REEXPORT_PASS_CLOSE_TO_SHELF',json.dumps(report))
