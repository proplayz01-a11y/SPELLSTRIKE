import bpy,os,math,random,importlib.util,json
WORK=r"C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\SpellstrikeSpellbook\Working"
TARGET=os.path.join(WORK,"Spellstrike_PhysicalSpellbook_Model_v02.blend")
assert os.path.normcase(bpy.data.filepath)==os.path.normcase(TARGET)
assert bpy.data.objects.get("COVER | Compass octant 00 left facet")
assert not bpy.data.objects.get("AGE | Cover hairline scratch 00"),"Already aged"
spec=importlib.util.spec_from_file_location("SpellbookTools_v02",os.path.join(WORK,"SpellbookTools_v02.py"))
T=importlib.util.module_from_spec(spec);spec.loader.exec_module(T)
decor=bpy.data.collections["22 | Cover Ornaments"]
front=bpy.data.objects["HINGE | Front cover - rotate Y to open"]
root=bpy.data.objects["SPELLSTRIKE | FULL PHYSICAL BOOK ROOT"]
# Procedural leather material is retained editable in Blender; for Unity export its
# base PBR swatch remains. Production texture baking is a later distinct QA task.
for matName, colors in [
    ("SSB | Aged midnight leather",[(.015,.018,.026),(.034,.029,.026),(.095,.058,.039)]),
    ("SSB | Worn leather inset",[(.012,.015,.023),(.029,.029,.028),(.065,.044,.036)]),
    ("SSB | Faded dark-brown leather",[(.040,.031,.024),(.082,.049,.030),(.144,.080,.048)])]:
    mat=bpy.data.materials[matName]
    nodes=mat.node_tree.nodes; links=mat.node_tree.links
    bs=nodes.get("Principled BSDF")
    assert bs
    noise=nodes.get("SSB_DistressedLeather_Macro") or nodes.new("ShaderNodeTexNoise")
    noise.name="SSB_DistressedLeather_Macro"
    noise.inputs["Scale"].default_value=8.0
    noise.inputs["Detail"].default_value=5
    noise.inputs["Roughness"].default_value=.79
    ramp=nodes.get("SSB_LeatherColorVariation") or nodes.new("ShaderNodeValToRGB")
    ramp.name="SSB_LeatherColorVariation"
    ramp.color_ramp.elements.remove(ramp.color_ramp.elements[1])
    els=ramp.color_ramp.elements
    els[0].position=.12;els[0].color=(*colors[0],1)
    mid=ramp.color_ramp.elements.new(.50);mid.color=(*colors[1],1)
    light=ramp.color_ramp.elements.new(.85);light.color=(*colors[2],1)
    links.new(noise.outputs["Fac"],ramp.inputs["Fac"])
    links.new(ramp.outputs["Color"],bs.inputs["Base Color"])
    bump=nodes.get("SSB_LeatherBump")
    if bump:bump.inputs["Strength"].default_value=.12;bump.inputs["Distance"].default_value=.025
# Lower the fresh, polished look of the metallic trims.
for matName, base, rough in [
    ("SSB | Antique burnished gold",(.34,.20,.07),.46),
    ("SSB | Raised brass highlights",(.53,.32,.12),.37),
    ("SSB | Oxidized inlay",(.13,.095,.052),.64)]:
    m=bpy.data.materials[matName]
    bs=m.node_tree.nodes.get("Principled BSDF")
    bs.inputs["Base Color"].default_value=(*base,1)
    bs.inputs["Roughness"].default_value=rough
    m.diffuse_color=(*base,1)
# Parchment fiber speckle is shallow, so illustrations remain legible later.
for matName in ["SSB | Aged parchment","SSB | Parchment warm alternate"]:
    m=bpy.data.materials[matName]
    b=m.node_tree.nodes.get("SSB_LeatherBump")
    if b:b.inputs["Strength"].default_value=.028;b.inputs["Distance"].default_value=.012
# 24 page leaves actually have different deckled edges, not a repeated solid block.
for i in range(24):
    obj=bpy.data.objects[f"PAGE SHEET | {i+1:02d} independent UV art"]
    mesh=obj.data
    for v in mesh.vertices:
        x,y,z=v.co
        if x>2.32:
            v.co.x+=.010*math.sin(y*11.1+(i+1)*1.82)+.006*math.sin(y*26.5+(i+1)*2.9)
        if abs(y)>1.62:
            v.co.y+=.005*(1 if y>0 else -1)*math.sin(x*17.3+i*1.4)
    mesh.update()
gold=bpy.data.materials["SSB | Antique burnished gold"]
bright=bpy.data.materials["SSB | Raised brass highlights"]
dark=bpy.data.materials["SSB | Oxidized inlay"]
# Delicate scratches and shallow cracks, concentrated on empty negative space.
random.seed(620)
for i in range(28):
    x=random.uniform(.36,2.52)
    y=random.uniform(-1.48,1.48)
    if math.hypot(x-1.455,y)<.90 or (x>2.37 and abs(y)<.33):
        continue
    length=random.uniform(.055,.19)
    theta=random.uniform(0,2*math.pi)
    x2=x+math.cos(theta)*length;y2=y+math.sin(theta)*length
    pts=[(x,y,.119),(x+math.cos(theta)*length*.48+.01*math.sin(i),y+math.sin(theta)*length*.48,.119),(x2,y2,.119)]
    T.tube(f"AGE | Cover hairline scratch {i:02d}",pts,dark,decor,front,width=random.uniform(.0025,.005))
# Spine blanket stitching in narrow zig-zag near the leather hinge.
for side in (-1,1):
    for i in range(22):
        y=-1.58+i*.15
        pts=[(-1.647,y-.048,side*.139),(-1.658,y,side*.155),(-1.648,y+.042,side*.139)]
        T.tube(f"AGE | Spine antique stitch {side}_{i:02d}",pts,gold,decor,root,width=.004)
# Circular bead accents on the central compass ring.
cx,cy=1.455,0.
for k in range(24):
    a=k*math.tau/24
    x=cx+.705*math.cos(a);y=cy+.705*math.sin(a)
    T.uv_sphere(f"AGE | Compass age rivet {k:02d}",(x,y,.148),(.014,.014,.008),gold,decor,front,8,4)
# Less showroom-perfect lighting while keeping embossed metal readable.
for lightName,mult in [
    ("QA | Large warm softbox",.82),
    ("QA | Blue shadow fill",.70),
    ("QA | Antique gold rim light",.85)]:
    obj=bpy.data.objects.get(lightName)
    if obj:obj.data.energy*=mult
bpy.ops.wm.save_as_mainfile(filepath=TARGET)
print(json.dumps({"mesh_objects":sum(o.type=="MESH" for o in bpy.context.scene.objects),"independent_pages":sum(bool(o.get("is_independent_page")) for o in bpy.context.scene.objects),"material_swatch":"procedural aged leather","saved":TARGET}))
