"""Read-only scene inspection, run in StudyArchive Blender's Python Console."""
import bpy
import json
from mathutils import Vector
from pathlib import Path

P=Path(r'C:\Users\David\PROTOTYPE\SPELLSTRIKE')
out=P/'Library/StoryboardLibraryShell_20261022/ScholarCorner_LIVE_INSPECT.json'
table=bpy.data.objects['STUDY_ScholarSideTable']
globe=bpy.data.objects['STUDY_AntiqueCelestialGlobe']
def detail(obj):
    pts=[obj.matrix_world @ v.co for v in obj.data.vertices] if obj.type=='MESH' else []
    return {
        'name':obj.name,'type':obj.type,
        'parent':obj.parent.name if obj.parent else None,
        'location':[round(float(v),4) for v in obj.location],
        'scale':[round(float(v),4) for v in obj.scale],
        'euler':[round(float(v),4) for v in obj.rotation_euler],
        'delta_location':[round(float(v),4) for v in obj.delta_location],
        'parent_inverse_matrix':[[round(float(v),4) for v in row] for row in obj.matrix_parent_inverse],
        'world_location':[round(float(v),4) for v in obj.matrix_world.translation],
        'mesh_local_min':[round(min(float(v.co[i]) for v in obj.data.vertices),4) for i in range(3)] if pts else [],
        'mesh_local_max':[round(max(float(v.co[i]) for v in obj.data.vertices),4) for i in range(3)] if pts else [],
        'mesh_world_min':[round(min(float(p[i]) for p in pts),4) for i in range(3)] if pts else [],
        'mesh_world_max':[round(max(float(p[i]) for p in pts),4) for i in range(3)] if pts else [],
        'selected':obj.select_get()
    }
record={'file':bpy.data.filepath,
        'table':detail(table),'globe':detail(globe),
        'table_children':[detail(obj) for obj in table.children],
        'globe_children':[detail(obj) for obj in globe.children],
        'active':bpy.context.view_layer.objects.active.name if bpy.context.view_layer.objects.active else None}
out.write_text(json.dumps(record,indent=2),encoding='utf8')
print('SCHOLAR_LIVE_INSPECT_WRITTEN',str(out))
