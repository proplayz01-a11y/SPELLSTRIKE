r"""One-time edit for CURRENT, OPEN StudyArchive Blender UI session.

Preserves unsaved in-memory work via copy=True .blend backup, rotates table
about its own center -90 degrees Z, parents globe to table preserving its
world position before rotation, then saves the original Blender scene.

Usage in the StudyArchive Blender Python Console:
 exec(compile(open(r'C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\LibraryProps\LibraryShell\rotate_scholar_corner_minus90_in_open_blender.py', encoding='utf-8').read(), 'rotate_scholar_corner_minus90_in_open_blender.py', 'exec'))

Idempotence guard: only applies when source roots are both unrotated/unparented.
"""
import bpy
import math
import json
from pathlib import Path
from mathutils import Vector

project = Path(r'C:\Users\David\PROTOTYPE\SPELLSTRIKE')
source = (project / 'ArtSource/LibraryProps/LibraryShell/LibraryStudyArchive_OneBuilding_v01.blend').resolve()
qa = project / 'Library/StoryboardLibraryShell_20261022'
qa.mkdir(parents=True,exist_ok=True)
backup_live = qa/'LibraryStudyArchive_LIVE_BEFORE_ScholarRotation.blend'

assert Path(bpy.data.filepath).resolve() == source, (
    'Wrong Blender file active! Refusing to rotate any other scene.',bpy.data.filepath)
table = bpy.data.objects['STUDY_ScholarSideTable']
globe = bpy.data.objects['STUDY_AntiqueCelestialGlobe']
assert table.type=='EMPTY' and globe.type=='EMPTY'
assert table.parent is None and globe.parent is None, (
    'Someone already parented an asset; refusing duplicate transform.')
assert abs(table.rotation_euler.z)<0.00001 and abs(globe.rotation_euler.z)<0.00001, (
    'These objects have been rotated already; refusing duplicate transform.')
assert (table.location-Vector((5.04,-7.80,0))).length<0.02
assert (globe.location-Vector((5.35,-7.78,.85))).length<0.02
assert len(table.children)>=1 and len(globe.children)>=1
assert not backup_live.exists(), ('Live session snapshot already exists; refusing overwrite.',str(backup_live))

# Save a copy of the unsaved in-memory state without changing the active file.
result=bpy.ops.wm.save_as_mainfile(
    filepath=str(backup_live),copy=True,compress=True,check_existing=False)
assert 'FINISHED' in result and backup_live.exists(), 'Unable to save safety snapshot.'
assert Path(bpy.data.filepath).resolve()==source, 'Backup altered active document path.'

bpy.context.view_layer.update()
globe_world_before=globe.matrix_world.copy()
table_position_before=table.matrix_world.translation.copy()
globe_world_position_before=globe_world_before.translation.copy()

# Parent to group future moves/rotations automatically, preserving the globe's
# original world transform. Then turn the combined group clockwise around
# the TABLE ROOT rather than spinning each object about its own origin.
globe.parent=table
globe.matrix_world=globe_world_before
bpy.context.view_layer.update()
table.rotation_euler.z-=math.pi/2
bpy.context.view_layer.update()

got_table=table.matrix_world.translation.copy()
got_globe=globe.matrix_world.translation.copy()
expected_globe=table_position_before+Vector((
    globe_world_position_before.y-table_position_before.y,
    -(globe_world_position_before.x-table_position_before.x),
    globe_world_position_before.z-table_position_before.z
))
assert (got_table-table_position_before).length<0.0001, 'Table center moved.'
assert (got_globe-expected_globe).length<0.001, (
    'Globe did not rotate with table as a group.', tuple(got_globe), tuple(expected_globe))
assert globe.parent == table

table['scholar_corner_orientation']='clockwise -90deg about local Z'
globe['scholar_corner_parenting']='Child of STUDY_ScholarSideTable: follows table rotation and translation'
metrics={
    'source':str(source),
    'live_unsaved_backup':str(backup_live),
    'table_world_center':[round(float(v),5) for v in got_table],
    'table_z_rotation_degrees':round(math.degrees(table.rotation_euler.z),5),
    'globe_world_center':[round(float(v),5) for v in got_globe],
    'globe_parent':globe.parent.name,
    'globe_expected_world_center':[round(float(v),5) for v in expected_globe]
}

# Scene state with other users' edits is saved as-is (not re-generated).
bpy.ops.wm.save_as_mainfile(filepath=str(source),compress=True,check_existing=False)
(qa/'ScholarCornerRotation_20261022.json').write_text(json.dumps(metrics,indent=2),encoding='utf-8')
print('SCHOLAR_CORNER_ROTATION_AND_SAVE_PASS',json.dumps(metrics))
