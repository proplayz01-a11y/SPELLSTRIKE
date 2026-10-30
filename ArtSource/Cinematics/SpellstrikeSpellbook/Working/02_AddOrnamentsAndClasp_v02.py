import bpy, os, math, importlib.util, random, json
from mathutils import Vector
WORK=r"C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\SpellstrikeSpellbook\Working"
TARGET=os.path.join(WORK,"Spellstrike_PhysicalSpellbook_Model_v02.blend")
assert os.path.normcase(bpy.data.filepath)==os.path.normcase(TARGET),"Not in v02 editable spellbook"
assert not bpy.data.objects.get("COVER | Compass octant 00"),"Compass decoration already exists"
spec=importlib.util.spec_from_file_location("SpellbookTools_v02",os.path.join(WORK,"SpellbookTools_v02.py"))
T=importlib.util.module_from_spec(spec);spec.loader.exec_module(T)
c=bpy.data.collections["22 | Cover Ornaments"]
prop=bpy.data.collections["23 | Clasp and Bookmark"]
front=bpy.data.objects["HINGE | Front cover - rotate Y to open"]
back=bpy.data.objects["HINGE | Back cover - rotatable around Y"]
spineRoot=bpy.data.objects["SPELLSTRIKE | FULL PHYSICAL BOOK ROOT"]
gold=bpy.data.materials["SSB | Antique burnished gold"]
bright=bpy.data.materials["SSB | Raised brass highlights"]
dark=bpy.data.materials["SSB | Oxidized inlay"]
leather=bpy.data.materials["SSB | Faded dark-brown leather"]
red=bpy.data.materials["SSB | Wine-red bookmark"]
blue=T.material("SSB | Deep cobalt sapphire",(.012,.09,.34),metal=.20,rough=.13)
# Antique leather book perimeter, engraved inset lines.
for name,xlo,xhi,ylo,yhi,z,w,mat in [
    ("outer",.16,2.72,-1.75,1.75,.111,.018,gold),
    ("inner",.24,2.64,-1.665,1.665,.116,.007,bright),
    ("hairline",.30,2.58,-1.612,1.612,.115,.004,dark)]:
    pts=[(xlo,ylo,z),(xhi,ylo,z),(xhi,yhi,z),(xlo,yhi,z)]
    T.tube(f"COVER | {name} gilded line",pts,mat,c,front,width=w,cyclic=True)
# Historical raised compass rings and concentric etching.
cx,cy=1.455,0
T.cylinder("COVER | Compass oxidized bronze medallion",(cx,cy,.122),.43,.013,dark,c,front,vertices=64,bevel=.009)
for rad,w,mat in [(.82,.013,gold),(.705,.007,bright),(.56,.009,gold),(.39,.018,gold),(.27,.005,bright),(.13,.014,dark)]:
    T.ring(f"COVER | Compass ring radius {rad:.3f}",(cx,cy,.135),rad,mat,c,front,width=w)
# Compass rose spearheads: faceted, physically raised and independent geometry.
for k in range(8):
    angle=k*math.pi/4
    L=.79 if k%2==0 else .56
    inner=.12
    width=.155 if k%2==0 else .11
    co=(math.cos(angle),math.sin(angle))
    perp=(-co[1],co[0])
    tip=(cx+co[0]*L,cy+co[1]*L)
    pL=(cx+co[0]*inner+perp[0]*width,cy+co[1]*inner+perp[1]*width)
    pR=(cx+co[0]*inner-perp[0]*width,cy+co[1]*inner-perp[1]*width)
    pM=(cx+co[0]*(inner+L)*.46,cy+co[1]*(inner+L)*.46)
    T.polygon(f"COVER | Compass octant {k:02d} left facet",[pL,tip,pM],.151,.007,bright,c,front)
    T.polygon(f"COVER | Compass octant {k:02d} right facet",[pM,tip,pR],.148,.007,gold,c,front)
T.cylinder("COVER | Compass center boss",(cx,cy,.166),.09,.035,bright,c,front,vertices=16,bevel=.014)
T.uv_sphere("COVER | Compass central hammered bead",(cx,cy,.19),(.047,.047,.028),dark,c,front,segments=12,rings=6)
# Gothic corner guards and scrollwork. Local XY remains fixed to front-cover hinge.
for ix,(x0,sx) in enumerate([(.195,1),(2.697,-1)]):
    for iy,(y0,sy) in enumerate([(-1.722,1),(1.722,-1)]):
        k=f"{ix}{iy}"
        path=[(0,0),(.55,0),(.55,.1),(.37,.13),(.23,.22),(.20,.44),(.09,.56),(0,.56)]
        worldPts=[(x0+sx*x,y0+sy*y) for x,y in path]
        T.polygon(f"GUARD | Brass gothic corner {k}",worldPts,.144,.037,gold,c,front)
        for d,idx in [((.11,.11),1),((.42,.08),2),((.08,.42),3)]:
            T.uv_sphere(f"GUARD | Hammered brass rivet {k}-{idx}",(x0+sx*d[0],y0+sy*d[1],.167),(.032,.032,.015),bright,c,front,12,6)
        scroll=[]
        for q in range(24):
            t=q/23*math.pi*1.30
            r=.20-.115*q/23
            scroll.append((x0+sx*(.32+r*math.cos(t)),y0+sy*(.30+r*math.sin(t)),.173))
        T.tube(f"GUARD | Curled filigree vine {k}",scroll,bright,c,front,width=.011)
        T.ring(f"GUARD | Etched knot {k}",(x0+sx*.35,y0+sy*.29,.169),.056,dark,c,front,.008,32)
# Decorative edge runes as short branching gold engravings, distributed along outer margin.
random.seed(121)
for side in (-1,1):
    x=1.45+side*1.09
    for i in range(9):
        y=-1.35+i*.335
        stem=[(x,y-.072,.123),(x,y+.078,.123)]
        T.tube(f"RUNE | Side {side:+d} {i:02d} stem",stem,dark,c,front,width=.004)
        direction=-1 if (i+int(side==1))%2==0 else 1
        T.tube(f"RUNE | Side {side:+d} {i:02d} branch",[(x,y+.015,.125),(x+direction*.043,y+.052,.125)],gold,c,front,width=.006)
for side in (-1,1):
    y=side*1.39
    for i in range(7):
        x=.63+i*.275
        T.tube(f"RUNE | Upper/lower {side:+d} {i}",[(x-.038,y,.124),(x,y+side*.055,.124),(x+.038,y,.124)],gold,c,front,width=.006)
# Cobalt-blue sapphire fastening. Real raised strap + metallic clasp, not a painted emblem.
T.cube("CLASP | Stitched dark leather strap",(2.66,0,.146),(.71,.37,.068),leather,prop,front,.035)
T.tube("CLASP | Leather stitching left",[(2.40,-.128,.189),(2.85,-.128,.189)],gold,prop,front,.004)
T.tube("CLASP | Leather stitching right",[(2.40,.128,.189),(2.85,.128,.189)],gold,prop,front,.004)
T.cube("CLASP | Brass slider plate",(2.72,0,.199),(.35,.44,.031),gold,prop,front,.045)
for rad,w,mat in [(.18,.025,bright),(.123,.012,dark)]:
    T.ring(f"CLASP | Gem bezel ring {rad}",(2.68,0,.235),rad,mat,prop,front,width=w)
T.uv_sphere("CLASP | Cobalt sapphire gemstone",(2.68,0,.254),(.115,.115,.066),blue,prop,front,24,12)
# 8 tiny facets give the blue gem its characteristic cut shape.
for k in range(8):
    a=k*math.tau/8
    x=2.68+math.cos(a)*.075;y=math.sin(a)*.075
    T.uv_sphere(f"CLASP | Sapphire side highlight {k:02}",(x,y,.285),(.009,.008,.006),bright,prop,front,8,4)
for sign in (-1,1):
    T.uv_sphere(f"CLASP | Brass hinge pin {sign:+}",(2.83,sign*.17,.22),(.032,.032,.025),gold,prop,front,12,6)
# Gold-star decorations on exposed leather spine, on Y/Z plane (facing x-negative).
for idx,y in enumerate([-.97,0,.97]):
    z=0
    T.tube(f"SPINE | Bronze engraved compass {idx}",[(-1.70,y+.105*math.cos(2*math.pi*i/32),z+.105*math.sin(2*math.pi*i/32)) for i in range(32)],gold,c,spineRoot,.008,True)
    T.tube(f"SPINE | Long gilt rune {idx}",[(-1.710,y-.145,0),(-1.710,y,.045),(-1.710,y+.145,0)],bright,c,spineRoot,.012)
    T.tube(f"SPINE | Cross gilt rune {idx}",[(-1.711,y,-.13),(-1.711,y,.13)],gold,c,spineRoot,.009)
# Small visible gold dust flecks on the cover, physically modelled but economical.
for i in range(40):
    x=random.uniform(.37,2.5);y=random.uniform(-1.54,1.54)
    r=math.hypot(x-cx,y-cy)
    if .86<r and not (x>2.40 and abs(y)<.32):
        T.uv_sphere(f"COVER | Stipple gold fleck {i:02}",(x,y,.127),(.008,.008,.003),gold,c,front,8,4)
bpy.ops.wm.save_as_mainfile(filepath=TARGET)
print(json.dumps({"decor_objects":len(c.objects),"clasp_objects":len(prop.objects),"all_meshes":sum(o.type=="MESH" for o in bpy.context.scene.objects),"file":bpy.data.filepath,"clasp":bpy.data.objects.get("CLASP | Cobalt sapphire gemstone") is not None}))
