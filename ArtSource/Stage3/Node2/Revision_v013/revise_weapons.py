"""Bounded v012 -> v013 revision. Originals retained; no Unity operations."""
import bpy, bmesh, math, json, hashlib
from pathlib import Path
from mathutils import Vector, Matrix
from mathutils.geometry import tessellate_polygon
from mathutils.bvhtree import BVHTree

BASE=Path(r'C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Stage3\Node2')
OUT=BASE/'Revision_v013'; OUT.mkdir(exist_ok=True)
SOURCE=BASE/'S3N2_DesertWeapons_v012.blend'
DEST=BASE/'S3N2_DesertWeapons_v013.blend'
SOURCE_HASH=hashlib.sha256(SOURCE.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(SOURCE),load_ui=False)
source_scene=bpy.context.scene
bowcol=bpy.data.collections['SRC_Bow']; arrowcol=bpy.data.collections['SRC_Arrow']
root=bpy.data.objects['S3N2_Bow_ROOT']; core=bpy.data.objects['S3N2_Bow_CORE']
aroot=bpy.data.objects['S3N2_Arrow_ROOT']
archive=bpy.data.collections.new('ARCHIVE_v012_Replaced'); source_scene.collection.children.link(archive)
archive.hide_render=True; archive.hide_viewport=True
changed=[]; archived=[]

def archive_obj(name):
    o=bpy.data.objects.get(name)
    if not o:return
    for c in list(o.users_collection):c.objects.unlink(o)
    archive.objects.link(o); o.hide_render=True
    archived.append(name)

def mat(name):return bpy.data.materials[name]
def newmat(name,color,metal=0,rough=.6):
    m=bpy.data.materials.new(name); m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value=(*color,1); p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
    m.diffuse_color=(*color,1)
    return m
gold=newmat('M_S3N2_WornGold_Preview',(.34,.22,.095),.78,.42)
dark=newmat('M_S3N2_Recess_Preview',(.07,.047,.025),.3,.72)

def deform(p):
    p=Vector(p); t=max(0,min(1,(abs(p.z)-.2)/.5)); w=t*t*(3-2*t)
    return p+Vector((0,.12*w,(-.072 if p.z>=0 else .049)*w))

def replace(name,verts,faces,material,kind='bow',draw=None,center=None,uvs=None):
    mesh=bpy.data.meshes.new(name+'_v013');mesh.from_pydata(verts,[],faces);mesh.update()
    o=bpy.data.objects.get(name)
    if o is None:
        o=bpy.data.objects.new(name,mesh); (bowcol if kind=='bow' else arrowcol).objects.link(o)
        o.parent=core if kind=='bow' else aroot
    for modifier in list(o.modifiers):o.modifiers.remove(modifier)
    o.data=mesh;o.matrix_parent_inverse=Matrix.Identity(4);o.matrix_basis=Matrix.Identity(4);o.hide_render=False;o.hide_set(False)
    mesh.materials.append(material)
    bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(mesh);bm.free()
    # Per-face authoring UV baseline, not the final shared atlas.
    uv=mesh.uv_layers.new(name='UVMap')
    for poly in mesh.polygons:
        axis=max(range(3),key=lambda k:abs(poly.normal[k])); axes=[k for k in range(3) if k!=axis]
        for li in poly.loop_indices:
            vi=mesh.loops[li].vertex_index
            uv.data[li].uv=uvs[vi] if uvs else (verts[vi][axes[0]]*2,verts[vi][axes[1]]*2)
    if draw:
        o.shape_key_add(name='Basis'); key=o.shape_key_add(name='Draw')
        delta=deform(center)-Vector(center) if draw=='rigid' else None
        for i,v in enumerate(verts):key.data[i].co=Vector(v)+delta if delta is not None else deform(v)
    changed.append(name)
    return o

def bevel(o,width=.0008):
    m=o.modifiers.new('EdgeFinish','BEVEL');m.width=width;m.segments=1;m.limit_method='ANGLE';m.angle_limit=.5
    m.use_clamp_overlap=True

def loft(rings):
    verts=[tuple(v) for r in rings for v in r];n=len(rings[0]);faces=[]
    for j in range(len(rings)-1):
        for k in range(n):faces.append((j*n+k,j*n+(k+1)%n,(j+1)*n+(k+1)%n,(j+1)*n+k))
    faces += [tuple(reversed(range(n))),tuple((len(rings)-1)*n+k for k in range(n))]
    return verts,faces

def ellipse_rings(stations,sides=12):
    return [[(cx+rx*math.cos(2*math.pi*k/sides),cy+ry*math.sin(2*math.pi*k/sides),z) for k in range(sides)] for cx,cy,z,rx,ry in stations]

def extrude_yz(points,x,thick):
    n=len(points);verts=[(x+dx,y,z) for dx in (-thick/2,thick/2) for y,z in points]
    faces=[tuple(reversed(range(n))),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return verts,faces

def extrude_xy(points,thick):
    n=len(points);v=[(x,y,z) for z in (-thick/2,thick/2) for x,y in points]
    f=[tuple(reversed(range(n))),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return v,f

def box(center,dims):
    x,y,z=center;a,b,c=[d/2 for d in dims]
    v=[(x+i*a,y+j*b,z+k*c) for i,j,k in [(-1,-1,-1),(-1,-1,1),(-1,1,-1),(-1,1,1),(1,-1,-1),(1,-1,1),(1,1,-1),(1,1,1)]]
    return v,[(0,4,6,2),(1,3,7,5),(0,1,5,4),(2,6,7,3),(0,2,3,1),(4,5,7,6)]

# A: fixed, offset window. Keep the original coordinate contract.
stations=[(0,-.03,.09,.020,.023),(0,-.03,.098,.020,.023),(.047,-.017,.106,.010,.022),(.047,-.008,.174,.010,.024),(.025,0,.202,.014,.027),(0,0,.22,.017,.030)]
v,f=loft(ellipse_rings(stations,8));replace('Bow_RiserUpper',v,f,mat('M_S3N2_SandWood'))
v,f=loft(ellipse_rings([(0,-.03,-.09,.021,.024),(0,-.015,-.15,.020,.026),(0,0,-.22,.017,.030)],8));replace('Bow_RiserLower',v,f,mat('M_S3N2_SandWood'))
# Existing bulky rear wall is now a thin bronze bridge facing the opening.
v,f=extrude_yz([(-.03,.175),(.01,.175),(.025,.21),(.014,.232),(-.018,.232),(-.042,.205)],.037,.010)
replace('Bow_WindowBack',v,f,mat('M_S3N2_Bronze'))
v,f=box((0,-.004,.1055),(.0056,.012,.015));replace('Bow_WindowShelf',v,f,gold)
bpy.data.objects['CHECK_ArrowWindow'].location=(0,.055,.12)

# Nock with rounded throat at local origin and 9 mm overlap onto the shaft.
r=.00215
outline=[(-.008,-.021),(.008,-.021),(.008,.005),(.0065,.010),(.00215,.010),(.00215,0)]
outline += [(r*math.cos(-math.pi*k/6),r*math.sin(-math.pi*k/6)) for k in range(1,7)]
outline += [(-.00215,.010),(-.0065,.010),(-.008,.005)]
v,f=extrude_xy(outline,.010);replace('Arrow_NockCollar',v,f,mat('M_S3N2_Bronze'),kind='arrow')
archive_obj('Arrow_NockTine_L');archive_obj('Arrow_NockTine_R')

# B: volumetric cord with unchanged ring centers in Basis and Draw.
anchors0=[Vector((0,.28,.7)),Vector((0,.28,.12)),Vector((0,.28,-.7))]
anchors1=[Vector((0,.4,.628)),Vector((0,.68,.12)),Vector((0,.4,-.651))]
def cord_rings(anchors):
    rings=[]
    for i,p in enumerate(anchors):
        tangent=(anchors[min(2,i+1)]-anchors[max(0,i-1)]).normalized()
        a=Vector((1,0,0)); b=tangent.cross(a).normalized()
        rings.append([p+.0015*(math.cos(k*math.pi/2)*a+math.sin(k*math.pi/2)*b) for k in range(4)])
    return rings
v,f=loft(cord_rings(anchors0));o=replace('Bow_String_Brace',v,f,mat('M_S3N2_String'),draw='flex')
dv,_=loft(cord_rings(anchors1))
for i,p in enumerate(dv):o.data.shape_keys.key_blocks['Draw'].data[i].co=p
for suffix,index in [('U',0),('L',2)]:
    # Small saddle wraps around the anchor; cord penetrates this intentional seat.
    p=anchors0[index];v,f=box(tuple(p),(.026,.007,.012));o=replace('Bow_Nock_'+suffix,v,f,mat('M_S3N2_Leather'),draw='rigid',center=p)
    bevel(o,.0006)

# C: retain ten limb objects, replace only their section geometry with a shared continuous profile.
zs=[.22,.28,.35,.425,.50,.56,.62,.66,.70,.755]
ys=[0,.006,.015,.045,.09,.14,.19,.235,.28,.31]
ws=[.065,.090,.103,.105,.093,.080,.064,.050,.036,.028]
ts=[.034,.034,.032,.030,.028,.026,.024,.022,.020,.018]
def interp(z,values):
    if z<=zs[0]:return values[0]
    if z>=zs[-1]:return values[-1]
    for j in range(len(zs)-1):
        if zs[j]<=z<=zs[j+1]:
            t=(z-zs[j])/(zs[j+1]-zs[j]);return values[j]*(1-t)+values[j+1]*t
def limb_center(z,sign):return Vector((0,interp(z,ys),sign*z))
def limb_basis(z,sign):
    d=.001;a=limb_center(max(.22,z-d),sign);b=limb_center(min(.755,z+d),sign)
    tang=(b-a).normalized();profile=Vector((0,tang.z,-tang.y))*sign
    return profile
def ring_at(z,sign):
    center=limb_center(z,sign);n=limb_basis(z,sign);t=interp(z,ts)/2;w=interp(z,ws)/2;c=.0013
    corners=[(-t,-w+c),(-t+c,-w),(t-c,-w),(t,-w+c),(t,w-c),(t-c,w),(-t+c,w),(-t,w-c)]
    return [center+Vector((x,0,0))+n*y for x,y in corners]
ends=[.22,.35,.50,.62,.70,.755]
for sign,suffix in [(1,'U'),(-1,'L')]:
    for j in range(5):
        samples=[ends[j],(ends[j]+ends[j+1])/2,ends[j+1]]
        v,f=loft([ring_at(z,sign) for z in samples]);replace(f'Bow_Limb_{suffix}_{j+1:02d}',v,f,mat('M_S3N2_SandWood'),draw='flex')
for o in list(bowcol.objects):
    if o.name.startswith('Bow_Border_'):archive_obj(o.name)
for sign,suffix in [(1,'U'),(-1,'L')]:
    for edge in (-1,1):
        rings=[]
        for k in range(10):
            z=.235+(.735-.235)*k/9;c=limb_center(z,sign);n=limb_basis(z,sign)
            # Full-thickness narrow edging, rather than detached front/back posts.
            c+=n*edge*(interp(z,ws)/2-.0015);t=interp(z,ts)/2+.0006
            rings.append([c+Vector((x,0,0))+n*y for x,y in [(-t,-.0025),(t,-.0025),(t,.0025),(-t,.0025)]])
        v,f=loft(rings);replace(f'Bow_Border_{suffix}_Rail_{"Inner" if edge>0 else "Outer"}',v,f,mat('M_S3N2_Bronze'),draw='flex')

# D: swept plates in the bow plane; no horizontal crossbars.
main_outline=[(-.059,.203),(-.086,.232),(-.110,.370),(-.068,.319),(-.049,.375),(-.015,.310),(.027,.265),(.018,.228),(-.019,.244)]
for sign,suffix,guard in [(1,'U','Bow_Guard_R'),(-1,'L','Bow_Guard_L')]:
    points=[(y,sign*z) for y,z in main_outline]
    for name,x in [(guard,.024),('Bow_Shoulder_'+suffix+'_A',-.024)]:
        v,f=extrude_yz(points,x,.011);replace(name,v,f,mat('M_S3N2_Bronze'),draw='rigid',center=(0,-.018,sign*.275))
    small=[(-.065,.225),(-.071,.251),(-.036,.286),(-.003,.316),(-.011,.274),(.019,.252)]
    for name,x in [('Bow_Shoulder_'+suffix+'_Wing',.031),('Bow_Shoulder_'+suffix+'_Wing2',-.031)]:
        v,f=extrude_yz([(y,sign*z) for y,z in small],x,.003);replace(name,v,f,gold,draw='rigid',center=(0,-.018,sign*.275))
    tips=[(.263,.696),(.289,.696),(.332,.749),(.323,.780),(.294,.803),(.302,.771),(.288,.749),(.258,.727)]
    v,f=extrude_yz([(y,sign*z) for y,z in tips],0,.020);replace('Bow_TipCap_'+suffix,v,f,mat('M_S3N2_Bronze'),draw='rigid',center=(0,.28,sign*.70))
    # Reuse the old sideways spike as a narrow raised chased-metal face on the hook.
    inlay=[(.278,.718),(.309,.751),(.307,.779),(.316,.760),(.312,.743)]
    v,f=extrude_yz([(y,sign*z) for y,z in inlay],.0105,.0015);replace('Bow_TipCap_'+suffix+'_Spike',v,f,gold,draw='rigid',center=(0,.28,sign*.70))

# E: oval grip, one continuous helical wrap, retained two cloth strips and pendant.
v,f=loft(ellipse_rings([(0,-.03,-.09,.018,.021),(0,-.03,0,.020,.023),(0,-.03,.09,.018,.021)],12));replace('Bow_Grip',v,f,mat('M_S3N2_Leather'))
for name,z in [('Bow_Collar_U',.087),('Bow_Collar_L',-.087),('Bow_GripCap_U',.076),('Bow_GripPommel',-.076)]:
    v,f=loft(ellipse_rings([(0,-.03,z-.005,.023,.026),(0,-.03,z+.005,.023,.026)],10));replace(name,v,f,mat('M_S3N2_Bronze'))
rings=[]
for k in range(43):
    t=k/42;a=2*math.pi*3.5*t;z=-.071+.142*t
    rings.append([(rx*math.cos(a),-.03+ry*math.sin(a),z+dz) for rx,ry,dz in [(.0204,.0234,-.012),(.0215,.0245,-.012),(.0215,.0245,.012),(.0204,.0234,.012)]])
v,f=loft(rings);replace('Bow_Wrap_01',v,f,mat('M_S3N2_Leather'))
for k in range(2,8):archive_obj(f'Bow_Wrap_{k:02d}')
for name in ['Bow_Wrap_Tail_A','Bow_Wrap_Tail_B']:archive_obj(name)
def cloth_mesh(x,y,z,length,width,phase):
    rings=[]
    for k in range(7):
        t=k/6;w=width*(1-.32*t)
        if k==5:w*=.55
        outward=(1 if x>0 else -1)*.042*math.sin(min(1,t/.6)*math.pi/2)
        cx=x+outward+.006*math.sin(t*math.pi*2+phase);cy=y-.032*t;zz=z-length*t
        rings.append([(cx+dx,cy+side*w/2,zz+(.010 if k==6 and side>0 else 0)) for dx,side in [(-.00035,-1),(.00035,-1),(.00035,1),(-.00035,1)]])
    return loft(rings)
for name,args in [('Bow_Cloth_Long',(.029,-.066,.260,.205,.027,0)),('Bow_Cloth_Short',(-.029,-.065,-.247,.137,.024,1.2))]:
    v,f=cloth_mesh(*args);replace(name,v,f,mat('M_S3N2_Cloth'),draw='rigid',center=(0,-.025,args[2]))

# Embedded PREVIEW mask: glyphs occupy one lane; final atlas and normal bake remain deferred.
W,H=128,512;pixels=[0.0]*(W*H*4)
def stroke(a,b,width=2):
    ax,ay=a;bx,by=b
    for y in range(max(0,int(min(ay,by)-width-1)),min(H,int(max(ay,by)+width+2))):
        for x in range(max(0,int(min(ax,bx)-width-1)),min(W,int(max(ax,bx)+width+2))):
            den=(bx-ax)**2+(by-ay)**2;t=max(0,min(1,((x-ax)*(bx-ax)+(y-ay)*(by-ay))/den)) if den else 0
            d=math.hypot(x-(ax+t*(bx-ax)),y-(ay+t*(by-ay)));c=max(0,min(1,width+.6-d));i=4*(y*W+x)
            for q in range(3):pixels[i+q]=max(pixels[i+q],c)
            pixels[i+3]=1
glyphs=[[(.3,.2),(.7,.5),(.3,.8)],[(.35,.2),(.35,.8),(.7,.5),(.35,.5)],[(.35,.25),(.65,.25),(.45,.75),(.7,.75)],[(.3,.6),(.5,.2),(.7,.6),(.5,.8),(.3,.6)],[(.3,.2),(.5,.5),(.3,.8),(.7,.8)],[(.3,.25),(.65,.25),(.45,.5),(.65,.75),(.3,.75)]]
for j,points in enumerate(glyphs):
    yy=14+j*80
    for a,b in zip(points,points[1:]):stroke((a[0]*W,yy+a[1]*60),(b[0]*W,yy+b[1]*60),2.3)
im=bpy.data.images.new('PREVIEW_S3N2_RuneLaneMask',W,H,alpha=True);im.pixels.foreach_set(pixels);im.pack()
lane=newmat('M_S3N2_RuneLane_Preview',(.11,.068,.028),.2,.7)
nt=lane.node_tree;p=nt.nodes.get('Principled BSDF');tex=nt.nodes.new('ShaderNodeTexImage');tex.image=im;tex.interpolation='Linear'
mix=nt.nodes.new('ShaderNodeMixRGB');mix.inputs[1].default_value=(.12,.079,.035,1);mix.inputs[2].default_value=(.75,.31,.028,1);nt.links.new(tex.outputs['Color'],mix.inputs[0]);nt.links.new(mix.outputs[0],p.inputs['Base Color'])
mul=nt.nodes.new('ShaderNodeMath');mul.operation='MULTIPLY';mul.inputs[1].default_value=2.1;nt.links.new(tex.outputs['Color'],mul.inputs[0]);nt.links.new(mul.outputs[0],p.inputs['Emission Strength']);p.inputs['Emission Color'].default_value=(1,.35,.024,1)
for sign,suffix in [(1,'U'),(-1,'L')]:
    verts=[];faces=[];uvs=[]
    for side in (-1,1):
        offset=len(verts)
        for k in range(9):
            t=k/8;z=.345+.295*t;c=limb_center(z,sign);n=limb_basis(z,sign)
            for across in (-1,1):
                pt=c+Vector((side*(interp(z,ts)/2+.0005),0,0))+n*across*.011
                verts.append(tuple(pt));uvs.append(((across+1)/2,t))
        for k in range(8):faces.append((offset+k*2,offset+k*2+1,offset+k*2+3,offset+k*2+2))
    replace('Bow_RunePlate_'+suffix,verts,faces,lane,draw='flex',uvs=uvs)
v,f=extrude_yz([(-.037,.018),(-.03,.025),(-.023,.018),(-.03,.005)],.022,.002)
replace('Bow_RunePlate_Center',v,f,gold)
# Preserve the pendant silhouette; seat its upper tip directly under its existing ring.
for name in ('Bow_Pendant_Frame','Bow_Pendant_Rune'):
    bpy.data.objects[name].location.z+=.100
    bpy.data.objects[name].location.y+=.026
    changed.append(name)
bpy.data.objects['Bow_ClothRing'].location.y+=.020
changed.append('Bow_ClothRing')

# F: pointed, genuinely ridged head; retains max 110 mm span and four diamonds per face.
left=[(0,.950),(-.012,.925),(-.029,.873),(-.021,.852),(-.055,.803),(-.025,.819),(-.019,.790),(-.035,.758),(-.014,.775),(-.012,.730),(0,.730)]
centerline=[(0,s) for s in (.755,.790,.825,.860,.895,.925)]
verts=[];faces=[];lookup={}
def addv(p):
    k=tuple(round(float(c),6) for c in p)
    if k not in lookup:lookup[k]=len(verts);verts.append(tuple(p))
    return lookup[k]
for side in (-1,1):
    for half in (-1,1):
        poly=[(half*x,-s) for x,s in left]+[(x,-s) for x,s in centerline]
        points=[Vector((x,y,0)) for x,y in poly]
        for tri in tessellate_polygon([points]):
            face=[]
            for corner in tri:
                q=points[corner] if isinstance(corner,int) else corner
                s=-q.y
                if abs(q.x)<1e-7:
                    h=.009*min(1,max(0,(s-.730)/.025),max(0,(.950-s)/.025))
                else:h=.00075
                face.append(addv((q.x,q.y,side*h)))
            faces.append(tuple(face))
outline=left[:-1]+[(-x,s) for x,s in reversed(left[1:])]
for i,(x,s) in enumerate(outline):
    nx,ns=outline[(i+1)%len(outline)]
    def edgeh(x,s):return .00075 if abs(x)>1e-7 else 0
    ids=[addv((x,-s,edgeh(x,s))),addv((nx,-ns,edgeh(nx,ns))),addv((nx,-ns,-edgeh(nx,ns))),addv((x,-s,-edgeh(x,s)))]
    unique=list(dict.fromkeys(ids))
    if len(unique)>=3:faces.append(tuple(unique))
head=replace('Arrow_Head',verts,faces,mat('M_S3N2_Bronze'),kind='arrow')
archive_obj('Arrow_Ridge_A');archive_obj('Arrow_Ridge_B')
head.data.calc_loop_triangles();tree=BVHTree.FromPolygons([v.co for v in head.data.vertices],[tuple(t.vertices) for t in head.data.loop_triangles],all_triangles=True)
for side,letter in [(1,'A'),(-1,'B')]:
    for i,s in enumerate([.795,.825,.855,.885],1):
        coords=[(-.005,s),(0,s+.007),(.005,s),(0,s-.007),(0,s)]
        vv=[]
        for j,(x,ss) in enumerate(coords):
            h=tree.ray_cast(Vector((x,-ss,side*.1)),Vector((0,0,-side)))[0]
            zz=h.z if h is not None else side*.006
            vv.append((x,-ss,zz+side*(.001 if j==4 else .00045)))
        replace(f'Arrow_Rune_{letter}_{i}',vv,[(0,1,4),(1,2,4),(2,3,4),(3,0,4)],mat('M_S3N2_Runes'),kind='arrow')
# Three opaque feather solids with a few broad ragged notches.
fp=[(.0055,-.050),(.015,-.060),(.024,-.078),(.026,-.098),(.023,-.110),(.020,-.112),(.024,-.119),(.022,-.140),(.017,-.143),(.021,-.151),(.017,-.177),(.007,-.195),(.0055,-.195)]
for i,angle in enumerate([math.pi/2,math.pi/2+2*math.pi/3,math.pi/2+4*math.pi/3],1):
    rvec=Vector((math.cos(angle),0,math.sin(angle)));normal=Vector((-math.sin(angle),0,math.cos(angle)))
    n=len(fp);v=[tuple(rvec*r+Vector((0,y,0))+normal*th) for th in (-.0003,.0003) for r,y in fp]
    f=[tuple(reversed(range(n))),tuple(range(n,2*n))]+[(k,(k+1)%n,(k+1)%n+n,k+n) for k in range(n)]
    replace(f'Arrow_Feather_{i}',v,f,mat('M_S3N2_Feather'),kind='arrow')
v,f=extrude_xy([(.005,-.730),(.011,-.728),(.017,-.718),(.012,-.710),(.010,-.721),(.004,-.724)],.002)
replace('Arrow_TieStub',v,f,mat('M_S3N2_Leather'),kind='arrow')

# Preserve readable bands while allowing them to pass over the fixed rest.
for name in ('Arrow_Band_1','Arrow_Band_2'):
    o=bpy.data.objects[name]
    for vert in o.data.vertices:vert.co.x*=.825;vert.co.z*=.825
    changed.append(name)

# A single small bevel catches light on the major metal plates, without extra ornaments.
for o in bowcol.objects:
    if o.name.startswith(('Bow_Guard_','Bow_Shoulder_','Bow_TipCap_')):bevel(o,.00075)

# Author-visible single Draw property, plus a separate linked review scene.
root['Draw']=0.0;root.id_properties_ui('Draw').update(min=0,max=1,description='Bow draw amount. Preserved Basis/Draw anchors.')
for o in bowcol.objects:
    if o.type=='MESH' and o.data.shape_keys and o.data.shape_keys.key_blocks.get('Draw'):
        key=o.data.shape_keys.key_blocks['Draw'];fc=key.driver_add('value');dr=fc.driver;dr.type='SCRIPTED';var=dr.variables.new();var.name='d';var.type='SINGLE_PROP';var.targets[0].id=root;var.targets[0].data_path='["Draw"]';dr.expression='d'
for frame,value in [(1,0),(31,1),(38,1),(43,0),(60,0)]:root['Draw']=float(value);root.keyframe_insert(data_path='["Draw"]',frame=frame)
root['Draw']=0.0
sc=bpy.data.scenes.new('S3N2_Review_v013');sc.collection.children.link(bowcol)
preview=bpy.data.collections.new('PREVIEW_HeldArrow');sc.collection.children.link(preview)
held=bpy.data.objects.new('PREVIEW_HeldArrow_ROOT',None);preview.objects.link(held);held.location.z=.12;held['Release']=0.0
for o in arrowcol.objects:
    if o.type=='MESH':
        cp=bpy.data.objects.new('PREVIEW_'+o.name,o.data);preview.objects.link(cp);cp.parent=held;cp.matrix_basis=o.matrix_basis.copy()
fc=held.driver_add('location',1);dr=fc.driver;dr.type='SCRIPTED'
for name,ob,path in [('d',root,'["Draw"]'),('r',held,'["Release"]')]:
    v=dr.variables.new();v.name=name;v.type='SINGLE_PROP';v.targets[0].id=ob;v.targets[0].data_path=path
dr.expression='.28+.4*d if r<=0 else .68-1.76*r'
for frame,value in [(1,0),(38,0),(60,1)]:held['Release']=float(value);held.keyframe_insert(data_path='["Release"]',frame=frame)
for ob in (root,held):
    action=ob.animation_data.action
    for layer in action.layers:
        for strip in layer.strips:
            for bag in strip.channelbags:
                for fc in bag.fcurves:
                    for kp in fc.keyframe_points:kp.interpolation='LINEAR'
sc.frame_start=1;sc.frame_end=60
for name,frame in [('BRACE',1),('HALF_DRAW',16),('FULL_DRAW',31),('RELEASE',38),('FLIGHT',60)]:sc.timeline_markers.new(name,frame=frame)
sc.render.engine='CYCLES';sc.cycles.device='CPU';sc.cycles.samples=32;sc.cycles.use_denoising=True
sc.render.threads_mode='FIXED';sc.render.threads=4
sc.render.resolution_x=1100;sc.render.resolution_y=1100;sc.render.resolution_percentage=100
sc.render.image_settings.file_format='PNG';sc.render.film_transparent=False
sc.world=bpy.data.worlds.new('S3N2_ReviewWorld');sc.world.use_nodes=True
sc.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.055,.06,.065,1);sc.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.45
camera=bpy.data.objects.new('S3N2_ReviewCamera',bpy.data.cameras.new('S3N2_ReviewCamera'));sc.collection.objects.link(camera);sc.camera=camera
camera.data.type='ORTHO';camera.data.ortho_scale=1.94
camera.location=(3,-1.65,.85);camera.rotation_euler=(Vector((0,.06,0))-camera.location).to_track_quat('-Z','Y').to_euler()
for name,loc,energy,size in [('Key',(2,-3,3),280,3),('Fill',(-2,-1,1),160,3),('Rim',(1,2,2),300,2)]:
    ld=bpy.data.lights.new('Review_'+name,'AREA');ld.energy=energy;ld.shape='DISK';ld.size=size;o=bpy.data.objects.new('Review_'+name,ld);sc.collection.objects.link(o);o.location=loc;o.rotation_euler=(-o.location).to_track_quat('-Z','Y').to_euler()
bpy.context.window.scene=sc;sc.frame_set(1);bpy.context.view_layer.update()
for o in bpy.context.selected_objects:o.select_set(False)
root.select_set(True);bpy.context.view_layer.objects.active=root
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.region_3d.view_distance=2.6;area.spaces.active.region_3d.view_location=(0,.06,0)
            area.spaces.active.shading.type='MATERIAL'
text=bpy.data.texts.new('READ_ME_v013')
text.write('v013: approved A-F local revisions. Use S3N2_Review_v013 scene, timeline 1-60 for a Blender-only draw/release preview. S3N2_Bow_ROOT custom Draw controls keyed parts. Original arrow meshes remain at their original root in Scene. Archived v012 parts are excluded from production collections. Rune mask is packed PREVIEW artwork, not a final atlas. Unity, LODs, export consolidation and final texture baking remain deferred. See Revision_v013 QA and revision report.\n')
bpy.ops.wm.save_as_mainfile(filepath=str(DEST))
assert hashlib.sha256(SOURCE.read_bytes()).hexdigest()==SOURCE_HASH
(OUT/'changes.json').write_text(json.dumps({'source_sha256':SOURCE_HASH,'output':str(DEST),'modified_or_added_objects':changed,'archived_objects':archived},indent=2))
print('REVISION_SAVED '+str(DEST),flush=True)
