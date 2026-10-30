import bpy, os, math, importlib.util, json
from mathutils import Vector, Euler
WORK=r"C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\SpellstrikeSpellbook\Working"
SOURCE=os.path.join(WORK,"Spellstrike_Spellbook_ReferenceWorkbench_v01.blend")
OUTPUT=os.path.join(WORK,"Spellstrike_PhysicalSpellbook_Model_v02.blend")
assert os.path.normcase(bpy.data.filepath)==os.path.normcase(SOURCE),f"Wrong Blender source {bpy.data.filepath}"
assert not os.path.exists(OUTPUT),"v02 exists already. Refusing overwrite"
assert bpy.data.objects.get("REFERENCE | Closed, spine, clasp, page geometry") is not None
spec=importlib.util.spec_from_file_location("SpellbookTools_v02",os.path.join(WORK,"SpellbookTools_v02.py"))
T=importlib.util.module_from_spec(spec);spec.loader.exec_module(T)
base=bpy.context.scene.collection
coll=T.collection("20 | Spellbook Physical Components",base)
hinges=T.collection("21 | Working Hinges and Pages",base)
decor=T.collection("22 | Cover Ornaments",base)
props=T.collection("23 | Clasp and Bookmark",base)
stage=T.collection("24 | Studio for QA Renders",base)
root=T.empty("SPELLSTRIKE | FULL PHYSICAL BOOK ROOT",coll,loc=(0,0,0))
root["book_width_m"]=2.86;root["book_height_m"]=3.86;root["closed_depth_m"]=.82
root["modular"]="Separate cover pivots, individual turnable page meshes, independent clasps"
leather=T.material("SSB | Aged midnight leather",(0.044,.050,.062),rough=.85,noise=True)
leatherEdge=T.material("SSB | Faded dark-brown leather",(0.082,.055,.043),rough=.89,noise=True)
inset=T.material("SSB | Worn leather inset",(0.035,.042,.049),rough=.84,noise=True)
paper=T.material("SSB | Aged parchment",(0.64,.485,.300),rough=.95,noise=True)
paperAlt=T.material("SSB | Parchment warm alternate",(0.59,.443,.285),rough=.95,noise=True)
gold=T.material("SSB | Antique burnished gold",(0.46,.262,.080),metal=.80,rough=.34)
goldBright=T.material("SSB | Raised brass highlights",(0.67,.42,.145),metal=.78,rough=.28)
goldDark=T.material("SSB | Oxidized inlay",(0.17,.12,.065),metal=.58,rough=.54)
red=T.material("SSB | Wine-red bookmark",(0.24,.052,.067),rough=.93,noise=True)
# Spine: cylinder axis aligned Y so it reads as a physical raised leather-bound rib.
spine=T.cylinder("SPINE | Rounded hand-sewn leather binding",(-1.43,0,0),.245,3.78,leatherEdge,coll,root,vertices=28,rotate=(math.pi/2,0,0),bevel=.035)
# Thin reinforced spine outside shoulder.
for iy in (-1.48,1.48):
    T.cylinder(f"SPINE | End-cap collar {iy:+.2f}",(-1.43,iy,0),.255,.13,goldDark,coll,root,vertices=28,rotate=(math.pi/2,0,0),bevel=.019)
# Spine ribs from upper and lower, distinct metal from leather.
for n,y in enumerate([-1.32,-.74,0,.74,1.32],1):
    T.cylinder(f"SPINE | Rib {n:02d}",(-1.43,y,0),.260,.068,gold,coll,root,vertices=28,rotate=(math.pi/2,0,0),bevel=.017)
# Covers hinged around the long left edge.
backHinge=T.empty("HINGE | Back cover - rotatable around Y",hinges,root,loc=(-1.43,0,-.345))
frontHinge=T.empty("HINGE | Front cover - rotate Y to open",hinges,root,loc=(-1.43,0,.345))
for hinge,key,zSign in [(frontHinge,"FRONT",1),(backHinge,"BACK",-1)]:
    T.cube(f"{key} COVER | Solid bevelled board",(1.45,0,0),(2.85,3.86,.145),leather,coll,hinge,.07)
    T.cube(f"{key} COVER | Raised leather surface",(1.45,0,zSign*.085),(2.70,3.66,.024),inset,coll,hinge,.025)
    T.cube(f"{key} COVER | Inner reinforcing rim",(1.45,0,-zSign*.078),(2.70,3.67,.015),leatherEdge,coll,hinge,.014)
# Edge backing inside the covers; not a fake flat picture.
T.cube("ENDBOARD | Back pastedown",(.01,0,-.237),(2.52,3.41,.024),paperAlt,coll,root,.009)
T.cube("ENDBOARD | Front pastedown",(.01,0,.237),(2.52,3.41,.024),paper,coll,root,.009)
# Independently turned pages, attached to left-edge pivot objects.
count=24
page_objects=[]
for j in range(count):
    z=-.205+.410*(j/(count-1))
    pivot=T.empty(f"PAGE HINGE | {j+1:02d} - Rotate Y",hinges,root,loc=(-1.235,0,z))
    pivot["page_number"]=j+1
    pivot["turn_axis"]="Y"
    pivot["turn_degrees_to_left"]=-180.
    pivot["Page Surface"]="Full 0-1 independent UV layer on both sides"
    sheet=T.page_mesh(f"PAGE SHEET | {j+1:02d} independent UV art",j+1,count,paper if j%3 else paperAlt,hinges,pivot)
    page_objects.append(sheet)
# Very shallow alternating page-edge ridges, not a single page-card object.
for j in range(12):
    z=-.205+(j+.5)*(.410/12)
    T.cube(f"PAGE EDGE | textured fore-edge {j+1:02d}",(1.202,0,z),(.014,3.28,.009),paperAlt if j%2 else paper,coll,root,.003)
# Bookmark physically trailing beneath fore-edge at bottom, not glued to the reference.
T.polygon("BOOKMARK | Frayed red ribbon",[
    (-.61,-1.35),(-.43,-1.35),(-.43,-2.14),(-.52,-2.065),(-.61,-2.14)],.245,.009,red,props,root)
T.tube("BOOKMARK | Gold stitched sigil",[(-.55,-1.88,.254),(-.52,-1.83,.254),(-.49,-1.88,.254),(-.52,-1.93,.254),(-.55,-1.88,.254)],goldBright,props,root,.009)
# Page and covers in same coordinate system. All loose geometry is genuine mesh.
bpy.context.scene.render.engine="BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in [x.identifier for x in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items] else "BLENDER_EEVEE"
bpy.context.scene.render.resolution_x=1280
bpy.context.scene.render.resolution_y=800
bpy.context.scene.render.resolution_percentage=85
bpy.ops.wm.save_as_mainfile(filepath=OUTPUT,check_existing=False)
print(json.dumps({"output":OUTPUT,"mesh_objects":sum(o.type=="MESH" for o in bpy.context.scene.objects),"pages":len(page_objects),"hinges":len([o for o in hinges.objects if o.name.startswith("PAGE HINGE")]),"ref_embedded":bool(bpy.data.objects["REFERENCE | Closed, spine, clasp, page geometry"].data.packed_file),"saved":os.path.exists(OUTPUT)}))
