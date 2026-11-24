"""Generate original lightweight potion models matching SPELLSTRIKE inventory."""
import bpy
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets/Resources/PotionLoot"
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

def mat(name, color, metallic=0, roughness=.3):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    p = m.node_tree.nodes.get("Principled BSDF")
    p.inputs["Base Color"].default_value = (*color, 1)
    p.inputs["Metallic"].default_value = metallic
    p.inputs["Roughness"].default_value = roughness
    return m

gold = mat("Potion_Gold", (.78,.42,.065), .75,.24)
dark = mat("Potion_DarkCollar", (.12,.14,.15), .5,.25)
cork = mat("Potion_Cork", (.44,.22,.065), .05,.55)
glass = mat("Potion_Glass", (.78,.90,.96), .02,.12)
glass.diffuse_color = (.78,.90,.96,.24)
glass.node_tree.nodes.get("Principled BSDF").inputs["Alpha"].default_value = .24
glass.surface_render_method = 'DITHERED'

def lathe(name, profile, materials, bands, offset):
    n = 24
    verts = [(r*math.cos(i*math.tau/n), r*math.sin(i*math.tau/n), z)
             for r,z in profile for i in range(n)]
    faces = []
    for j in range(len(profile)-1):
        for i in range(n):
            k=(i+1)%n
            faces.append((j*n+i,j*n+k,(j+1)*n+k,(j+1)*n+i))
    faces += [tuple(reversed(range(n))), tuple((len(profile)-1)*n+i for i in range(n))]
    mesh=bpy.data.meshes.new(name)
    mesh.from_pydata(verts,[],faces); mesh.update()
    obj=bpy.data.objects.new(name,mesh); bpy.context.collection.objects.link(obj)
    obj.location.x=offset
    for m in materials: mesh.materials.append(m)
    for p in mesh.polygons:
        p.use_smooth=p.index<len(faces)-2
        p.material_index=bands[min(p.index//n,len(bands)-1)]
    return obj

for k,(name,col) in enumerate([
    ("Health",(.65,.008,.018)),("Purify",(.008,.20,.82)),("PowerUp",(.025,.56,.012))]):
    liquid=mat("Potion_"+name+"_Liquid",col,.04,.16)
    surface=mat("Potion_"+name+"_Meniscus",tuple(min(1,x*1.35+.025) for x in col),.02,.13)
    body=lathe("Glass_"+name,
        [(.16,.015),(.24,.035),(.30,.09),(.33,.18),(.32,.26),(.27,.34),
         (.18,.43),(.105,.52),(.10,.62),(.105,.65)],
        [glass],[0],k*1.0)
    fluid=lathe("Liquid_"+name,
        [(.145,.047),(.221,.065),(.281,.108),(.306,.18),(.295,.25),
         (.249,.32),(.216,.365),(.206,.373),(.187,.378)],
        [liquid,surface],[0,0,0,0,0,0,1,1],k*1.0)
    collar=lathe("Collar_"+name,[(.108,.615),(.123,.625),(.123,.65),
        (.112,.66),(.12,.69),(.11,.715)],[dark,gold],[1,0,1,0,1],k*1.0)
    cap=lathe("Stopper_"+name,[(.11,.695),(.12,.72),(.12,.79),(.105,.81)],
              [gold,cork],[0,1,0],k*1.0)
    bpy.ops.object.select_all(action='DESELECT')
    parts=[body,fluid,collar,cap]
    for part in parts:
        part.select_set(True)
        part.location.x=0
    bpy.context.view_layer.objects.active=body
    bpy.ops.export_scene.fbx(filepath=str(OUT/("Potion_"+name+".fbx")),
        use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',
        apply_unit_scale=True,add_leaf_bones=False,bake_anim=False)
    for part in parts: part.location.x=k*1.0
    print(name, sum(len(p.data.vertices) for p in parts), sum(len(p.data.polygons) for p in parts))

bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/"ArtSource/PotionLoot/PotionModels.blend"))
