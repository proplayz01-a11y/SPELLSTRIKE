import bpy,bmesh,math,pathlib,json
from mathutils import Vector
OUT=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/ArtSource/Stage3/Node1/Character/NewMesh_v001');bpy.ops.wm.open_mainfile(filepath=str(OUT/'SunboundWarden_NewMesh_Layer1.blend'));sc=bpy.context.scene
CORE=bpy.data.collections['LAYER_1_Anatomical_Core'];ARMOR=bpy.data.collections['LAYER_2_Armor'];CLOTH=bpy.data.collections['LAYER_2_Cloth'];DETAIL=bpy.data.collections['LAYER_2_Armor_Detail'];REVIEW=bpy.data.collections['REVIEW_Studio']
Gold=bpy.data.materials['Warden_Antique_Gold'];LightGold=bpy.data.materials['Warden_Burnished_Edges'];DarkGold=bpy.data.materials['Warden_Recess_Bronze'];Purple=bpy.data.materials['Warden_Royal_Purple'];Leather=bpy.data.materials['Warden_Dark_Leather'];Dark=bpy.data.materials['Warden_Shadow_Recess'];Silver=bpy.data.materials['Warden_Warm_Steel']
code=(OUT/'build_new_mesh.py').read_text();exec(code[code.index('def objmesh'):code.index('# Muscle/garment')])
def place(o,coll):
 for c in list(o.users_collection):c.objects.unlink(o)
 coll.objects.link(o);return o
def sphere(name,loc,scale,mat,coll=ARMOR):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=20,ring_count=12,radius=1,location=loc);o=bpy.context.object;o.name=name;o.scale=scale;place(o,coll);o.data.materials.append(mat)
 for p in o.data.polygons:p.use_smooth=True
 return o
def cube(name,loc,scale,mat,coll=ARMOR,bevel=.005):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.scale=scale;place(o,coll);o.data.materials.append(mat);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 b=o.modifiers.new('Forged edge bevel','BEVEL');b.width=bevel;b.segments=2;o.modifiers.new('Weighted normals','WEIGHTED_NORMAL');return o
def curve(name,pts,r,mat,coll=DETAIL):
 cu=bpy.data.curves.new(name,'CURVE');cu.dimensions='3D';cu.resolution_u=2;cu.bevel_depth=r;cu.bevel_resolution=1;s=cu.splines.new('POLY');s.points.add(len(pts)-1)
 for p,co in zip(s.points,pts):p.co=(*co,1)
 o=bpy.data.objects.new(name,cu);coll.objects.link(o);cu.materials.append(mat);return o
def plate(name,outline,mat=Gold,coll=ARMOR,depth=.004,bevel=.002):
 vs=outline;fs=[tuple(range(len(vs)))];o=objmesh(name,vs,fs,mat,coll,False);s=o.modifiers.new('Plate thickness','SOLIDIFY');s.thickness=depth;s.offset=0;b=o.modifiers.new('Rounded plate edge','BEVEL');b.width=bevel;b.segments=2;o.modifiers.new('Weighted plate normals','WEIGHTED_NORMAL');return o
def border(name,points,r=.003):return curve(name,points+[points[0]],r,LightGold)
def disc(name,loc,r,mat,depth=.01):
 bpy.ops.mesh.primitive_cylinder_add(vertices=32,radius=r,depth=depth,location=loc,rotation=(math.pi/2,0,0));o=bpy.context.object;o.name=name;place(o,DETAIL);o.data.materials.append(mat);b=o.modifiers.new('Disc bevel','BEVEL');b.width=.0018;b.segments=2;o.modifiers.new('Weighted disc normals','WEIGHTED_NORMAL');return o
def sun(name,cx,cy,cz,r,front=True):
 disc(name+'_outer',(cx,cy,cz),r,LightGold,.010);disc(name+'_recess',(cx,cy-.006 if front else cy+.006,cz),r*.77,DarkGold,.007);disc(name+'_boss',(cx,cy-.012 if front else cy+.012,cz),r*.53,Gold,.012)
 for i in range(12):
  a=i*math.tau/12;outline=[]
  for rr,aa in [(r*.82,a-.11),(r*1.35,a),(r*.82,a+.11)]:outline.append((cx+math.sin(aa)*rr,cy-.003 if front else cy+.003,cz+math.cos(aa)*rr))
  plate(name+'_ray_%02d'%i,outline,LightGold,DETAIL,.004,.0008)
# Fill all visible flex joints with shaped flexible core covers.
for x in [.245,.525]:
 o=sphere('Core_Flex_'+str(x)+'_R',(x,0,1.435),(.070,.061,.067),Leather,CORE);mirror(o)
o=sphere('Core_KneeFlex_R',(.12,0,.615),(.067,.064,.065),Leather,CORE);mirror(o)
# Cuirass shell is a purpose-made ring loft, then embossed front/back panels.
loftz('Armor_Cuirass_Main',[(1.05,.168,.111,0,0),(1.12,.176,.113,0,0),(1.25,.211,.132,0,0),(1.39,.247,.137,0,0),(1.46,.229,.115,0,0),(1.495,.125,.085,0,0)],Gold,ARMOR,20,0)
for side in [1,-1]:
 outline=[(side*.008,-.143,1.405),(side*.100,-.132,1.478),(side*.210,-.104,1.446),(side*.226,-.111,1.330),(side*.157,-.121,1.269),(side*.010,-.145,1.296)]
 plate('Armor_Pectoral_'+str(side),outline,Gold);border('Trim_Pectoral_'+str(side),outline,.003)
 # Radiating breastplate channels retain the reference sun language.
 for dz,x in [(1.40,.18),(1.365,.205),(1.29,.15),(1.205,.135)]:curve('Engraving_Chest_'+str(side),[(side*.055,-.158,1.33),(side*x,-.133,dz)],.0023,DarkGold)
 for i in range(3):
  z=1.10+i*.045;outline=[(side*.015,-.129,z-.018),(side*.148,-.100,z+.005),(side*.164,-.106,z+.032),(side*.02,-.143,z+.017)];plate('Armor_Abdominal_Lame_'+str(side)+'_'+str(i),outline,Gold);curve('Trim_Abdomen',outline[0:2],.0025,LightGold)
 # Back plate follows scapula and lower back rather than a flat panel.
 outline=[(side*.018,.137,1.46),(side*.17,.099,1.436),(side*.225,.092,1.345),(side*.139,.125,1.19),(side*.014,.131,1.105)]
 plate('Armor_Back_'+str(side),outline,Gold);border('Trim_Back',outline,.0025)
 curve('Back_Rib',[(side*.025,.143,1.43),(side*.105,.137,1.31),(side*.026,.138,1.17)],.0035,DarkGold)
sun('Chest_Solar_Seal',0,-.165,1.33,.052)
# Gothic gorget/collar with flared side corners.
loftz('Armor_Gorget',[(1.45,.108,.085,0,0),(1.48,.127,.099,0,0),(1.52,.097,.080,0,0),(1.55,.087,.075,0,0)],DarkGold,ARMOR,20,0)
for side in [1,-1]:
 outline=[(side*.077,-.064,1.485),(side*.134,-.044,1.55),(side*.138,.062,1.53),(side*.062,.092,1.49)];plate('Armor_Gorget_Wing_'+str(side),outline,LightGold)
# Shoulder shells with multiple overlapping lames.
for side in [1,-1]:
 sphere('Armor_Shoulder_Under_'+str(side),(side*.285,0,1.455),(.12,.115,.087),DarkGold)
 outline=[(side*.21,-.119,1.49),(side*.263,-.146,1.56),(side*.375,-.130,1.542),(side*.421,-.098,1.49),(side*.408,.10,1.489),(side*.356,.135,1.533),(side*.261,.135,1.55),(side*.215,.101,1.495)]
 # Triangulated raised center gives the pauldron a real dome.
 vs=outline+[(side*.312,0,1.581)];fs=[(i,(i+1)%8,8) for i in range(8)];o=objmesh('Armor_Pauldron_'+str(side),vs,fs,Gold,ARMOR,False);s=o.modifiers.new('Forged shell','SOLIDIFY');s.thickness=.008;b=o.modifiers.new('Edge bevel','BEVEL');b.width=.004;b.segments=2;o.modifiers.new('Weighted normals','WEIGHTED_NORMAL');border('Trim_Pauldron_'+str(side),outline,.004)
 for i in range(3):
  x=.342+i*.034;outline=[(side*x,-.091,1.49-i*.018),(side*(x+.075),-.074,1.448-i*.018),(side*(x+.079),.079,1.45-i*.018),(side*x,.095,1.489-i*.018)];plate('Armor_ShoulderLame_'+str(side)+'_'+str(i),outline,Gold);border('Trim_ShoulderLame',outline,.002)
 # Raised pauldron inset and rivets.
 outline=[(side*.273,-.115,1.554),(side*.346,-.108,1.547),(side*.357,-.106,1.508),(side*.275,-.121,1.517)];plate('Pauldron_Inset',outline,DarkGold,DETAIL);border('Trim_Pauldron_Inset',outline,.002)
 for x in [.255,.383]:sphere('Pauldron_Rivet',(side*x,-.124,1.506),(.005,.005,.005),LightGold,DETAIL)
# Upper-arm bands and long shield-shaped bracers.
for side in [1,-1]:
 rings=[(.445,.059,.069,0,1.435),(.463,.061,.071,0,1.435),(.479,.058,.064,0,1.435)];o=loftx('Armor_BicepBand_'+str(side),[(side*x,ry,rz,cy,cz) for x,ry,rz,cy,cz in rings],LightGold,ARMOR,16)
 o=loftx('Armor_Bracer_Cuff_'+str(side),[(side*.555,.075,.075,0,1.435),(side*.574,.079,.079,0,1.435),(side*.589,.073,.074,0,1.435)],LightGold,ARMOR,16)
 outline=[(side*.581,-.056,1.48),(side*.624,-.050,1.522),(side*.748,-.031,1.506),(side*.879,-.024,1.477),(side*.881,.025,1.478),(side*.745,.045,1.51),(side*.623,.054,1.52),(side*.581,.056,1.48)]
 plate('Armor_Bracer_Dorsal_'+str(side),outline,Gold,ARMOR,.01,.004);border('Trim_Bracer_Dorsal',outline,.003)
 curve('Bracer_Central_Ridge',[(side*.610,0,1.534),(side*.747,0,1.523),(side*.868,0,1.493)],.0035,LightGold)
 # Side skin remains purple leather between plated panels.
 outline=[(side*.58,-.071,1.42),(side*.69,-.065,1.468),(side*.866,-.037,1.461),(side*.864,-.037,1.415),(side*.69,-.065,1.397)];plate('Armor_Bracer_Flank_'+str(side),outline,DarkGold)
 o=loftx('Armor_WristCuff_'+str(side),[(side*.878,.044,.042,0,1.435),(side*.898,.048,.046,0,1.435),(side*.916,.042,.039,0,1.435)],LightGold,ARMOR,16)
# Helmet's true volume and separate cheek/nose mask.
loftz('Armor_Helmet_Crown',[(1.573,.065,.070,0,0),(1.61,.099,.098,0,0),(1.70,.103,.104,0,0),(1.77,.100,.100,0,0),(1.82,.080,.081,0,0),(1.845,.030,.034,0,0)],Gold,ARMOR,20,0)
# Eyes are recessed slits between brow and cheek plates.
for side in [1,-1]:
 outline=[(side*.009,-.108,1.754),(side*.077,-.083,1.761),(side*.086,-.091,1.736),(side*.026,-.118,1.737)];plate('Helmet_Brow_'+str(side),outline,LightGold,ARMOR,.011,.002)
 outline=[(side*.012,-.115,1.735),(side*.077,-.104,1.746),(side*.075,-.108,1.725),(side*.022,-.131,1.720)];plate('Helmet_Eye_Slit_'+str(side),outline,Dark,DETAIL,.001,.0005)
 outline=[(side*.020,-.135,1.713),(side*.081,-.105,1.724),(side*.078,-.082,1.650),(side*.051,-.101,1.597),(side*.008,-.126,1.629)];plate('Helmet_Cheek_'+str(side),outline,Gold,ARMOR,.014,.003);curve('Helmet_Cheek_Trim',outline[1:4],.0025,LightGold)
 outline=[(side*.095,-.049,1.746),(side*.121,.012,1.722),(side*.107,.064,1.672),(side*.070,.027,1.620),(side*.085,-.060,1.661)];plate('Helmet_Temple_'+str(side),outline,DarkGold)
plate('Helmet_Nose',[(0,-.143,1.778),(-.020,-.125,1.736),(-.013,-.143,1.673),(0,-.154,1.642),(.013,-.143,1.673),(.020,-.125,1.736)],LightGold,ARMOR,.01,.0015)
curve('Helmet_Forehead_Crest',[(0,-.093,1.837),(0,-.117,1.80),(0,-.132,1.767)],.0035,LightGold)
# Forked crescent horns generated as tapered polygonal lofts along new paths.
for side in [1,-1]:
 points=[((side*.072,.006,1.724),.033),((side*.128,.012,1.765),.046),((side*.166,.011,1.839),.038),((side*.168,.010,1.903),.025),((side*.145,.003,1.969),.015),((side*.108,-.003,2.029),.001)]
 vs=[];fs=[];n=8
 for i,(p,r) in enumerate(points):
  p=Vector(p);prev=Vector(points[max(0,i-1)][0]);nxt=Vector(points[min(len(points)-1,i+1)][0]);t=(nxt-prev).normalized();u=Vector((0,1,0));w=t.cross(u).normalized()
  for k in range(n):a=k*math.tau/n;vs.append(tuple(p+u*math.cos(a)*r*.72+w*math.sin(a)*r))
 for i in range(len(points)-1):
  for k in range(n):fs.append((i*n+k,i*n+(k+1)%n,(i+1)*n+(k+1)%n,(i+1)*n+k))
 fs.extend([tuple(reversed(range(n))),tuple((len(points)-1)*n+k for k in range(n))]);o=objmesh('Helmet_CrescentHorn_'+str(side),vs,fs,Gold,ARMOR,False);b=o.modifiers.new('Horn ridge bevel','BEVEL');b.width=.0015;b.segments=2;o.modifiers.new('Weighted horn normals','WEIGHTED_NORMAL')
 curve('Horn_Inlaid_Ridge',[(p[0],p[1]-.72*r-.002,p[2]) for p,r in points[:-1]],.0025,LightGold)
plate('Helmet_Center_Crest',[(-.025,.008,1.825),(0,.002,1.92),(.025,.008,1.825)],DarkGold,ARMOR,.023,.002)
# Belt, buckle and layered hip tassets.
loftz('Armor_WaistBelt',[(.975,.180,.122,0,0),(.986,.188,.13,0,0),(1.030,.186,.129,0,0),(1.042,.174,.12,0,0)],DarkGold,ARMOR,24)
for z in [.982,1.036]:curve('Belt_Edge',[(.184*math.cos(i*math.tau/40),.128*math.sin(i*math.tau/40),z) for i in range(41)],.003,LightGold)
plate('Armor_Solar_Buckle',[(-.043,-.143,1.042),(0,-.154,1.068),(.043,-.143,1.042),(.039,-.154,.975),(0,-.167,.917),(-.039,-.154,.975)],LightGold,ARMOR,.014,.003)
disc('Buckle_Recess',(0,-.168,1.005),.025,DarkGold,.012)
curve('Buckle_Rune',[(0,-.178,1.03),(-.013,-.18,1.012),(0,-.183,.962),(.013,-.18,1.012),(0,-.178,1.03)],.003,Gold)
for side in [1,-1]:
 for i in range(3):
  z=.953-i*.10;x=.14+i*.013;outline=[(side*(x-.035),-.095,z),(side*(x+.052),-.072,z+.025),(side*(x+.075),-.063,z-.065),(side*(x+.014),-.103,z-.123),(side*(x-.036),-.115,z-.060)];plate('Armor_Tasset_'+str(side)+'_'+str(i),outline,Gold,ARMOR,.008,.003);border('Trim_Tasset',outline,.0022)
# Knees, greaves, sabatons.
for side in [1,-1]:
 outline=[(side*.12,-.082,.704),(side*.190,-.073,.636),(side*.176,-.092,.586),(side*.12,-.112,.550),(side*.063,-.092,.586),(side*.052,-.073,.636)];plate('Armor_Knee_'+str(side),outline,Gold,ARMOR,.014,.003);border('Trim_Knee',outline,.003);sun('Knee_SolarBoss_'+str(side),side*.12,-.115,.628,.027)
 outline=[(side*.066,-.064,.574),(side*.128,-.090,.542),(side*.184,-.061,.569),(side*.181,-.063,.443),(side*.172,-.054,.273),(side*.152,-.065,.167),(side*.117,-.078,.167),(side*.089,-.065,.273),(side*.066,-.063,.443)];plate('Armor_Greave_'+str(side),outline,Gold,ARMOR,.012,.004);border('Trim_Greave',outline,.003)
 curve('Greave_Ridge',[(side*.128,-.105,.539),(side*.132,-.088,.369),(side*.135,-.090,.196)],.0035,LightGold)
 for z in [.19,.26]:loftz('Armor_AnkleBand_'+str(side)+'_'+str(z),[(z,.055,.059,side*.135,0),(z+.022,.057,.061,side*.135,0)],DarkGold,ARMOR,16)
 o=loftz('Armor_Sabaton_'+str(side),[(.019,.067,.139,side*.135,-.063),(.037,.068,.142,side*.135,-.065),(.073,.067,.128,side*.135,-.056),(.11,.063,.108,side*.135,-.042),(.149,.05,.068,side*.135,-.013),(.178,.042,.051,side*.135,0)],Gold,ARMOR,20)
 for y,z in [(-.162,.067),(-.121,.10),(-.073,.132)]:curve('Sabaton_Articulation',[(side*.135-.06,y,z),(side*.135,y-.008,z+.006),(side*.135+.06,y,z)],.0025,DarkGold)
# Layered cloth surfaces have their own geometry and gold embroidery.
def tabard(name,top,bottom,width,front=True,cx=0):
 vs=[];fs=[];ny=10;nx=8;sg=-1 if front else 1
 for row in range(ny):
  t=row/(ny-1);z=top+(bottom-top)*t;w=width*(1-.12*t)
  for col in range(nx+1):
   u=col/nx*2-1;zz=z-(.075*(1-u*u)*t**8);y=sg*(.144+.019*math.sin(t*math.pi)+.01*t+.010*math.cos(u*math.pi*3)*math.sin(t*math.pi));vs.append((cx+u*w,y,zz))
 for r in range(ny-1):
  for c in range(nx):a=r*(nx+1)+c;fs.append((a,a+1,a+nx+2,a+nx+1))
 o=objmesh(name,vs,fs,Purple,CLOTH,True,1);so=o.modifiers.new('Cloth thickness','SOLIDIFY');so.thickness=.003
 for edgeu in [-.92,.92]:
  pts=[]
  for r in range(ny):
   t=r/(ny-1);z=top+(bottom-top)*t-.075*(1-edgeu*edgeu)*t**8;w=width*(1-.12*t);y=sg*(.147+.019*math.sin(t*math.pi)+.01*t+.010*math.cos(edgeu*math.pi*3)*math.sin(t*math.pi));pts.append((cx+edgeu*w,y,z))
  curve(name+'_GoldBorder',pts,.0027,LightGold)
 return o
tabard('Cloth_Main_Front_Tabard',.977,.427,.101,True);tabard('Cloth_Main_Back_Tabard',.978,.447,.112,False)
# Embroidered solar emblem lies on the front cloth and shares its plane.
sun('Tabard_EmbroideredSun',0,-.181,.635,.030)
curve('Tabard_LowerChevron',[(-.072,-.179,.535),(0,-.185,.471),(.072,-.179,.535)],.0028,LightGold)
for side in [1,-1]:
 tabard('Cloth_ShoulderBanner_'+str(side),1.442,1.156,.040,True,side*.245)
 for z in [1.37,1.27,1.19]:
  pts=[(side*.245,-.158,z+.014),(side*.258,-.158,z),(side*.245,-.158,z-.014),(side*.232,-.158,z),(side*.245,-.158,z+.014)];curve('Banner_Diamond',pts,.002,LightGold)
 # Knee pennants soften the heavy armor silhouette.
 outline=[(side*.068,-.061,.593),(side*.190,-.061,.592),(side*.184,-.056,.475),(side*.128,-.077,.426),(side*.066,-.056,.472)];plate('Cloth_KneePennant_'+str(side),outline,Purple,CLOTH,.003,.001);border('Pennant_GoldEdge',outline,.0018)
# Hands: armor sized to the anatomical core rather than decorative oversized blocks.
for side in [1,-1]:
 outline=[(side*.931,-.031,1.463),(side*.947,-.046,1.470),(side*.98,-.045,1.47),(side*1.000,-.026,1.461),(side*1.002,.033,1.461),(side*.973,.044,1.471),(side*.943,.034,1.468)];plate('Armor_HandBack_'+str(side),outline,Gold,ARMOR,.004,.002);border('Trim_HandBack',outline,.0015)
 for cy,length,spread,ry in [(-.034,.058,-.014,.009),(-.0105,.075,-.005,.0095),(.014,.085,.002,.010),(.038,.078,.010,.009)]:
  for k,(a,b) in enumerate([(.06,.30),(.45,.65),(.80,.97)]):
   vs=[];fs=[]
   for t in [a,(a+b)/2,b]:
    x=1+length*t;y=cy+spread*t;z=1.435-.02*t*t;r=.015*(1-.3*t)+.004*max(0,1-t/.32)
    for deg in [-65,-30,0,30,65]:
     aa=math.radians(deg);vs.append((side*x,y+math.sin(aa)*ry*(1-.30*t),z+math.cos(aa)*r+.001))
   for row in range(2):
    for j in range(4):fs.append((row*5+j,(row+1)*5+j,(row+1)*5+j+1,row*5+j+1))
   o=objmesh('Armor_Finger_'+str(side)+'_'+str(cy)+'_'+str(k),vs,fs,Gold,ARMOR,True);s=o.modifiers.new('Finger plate thickness','SOLIDIFY');s.thickness=.0013;b=o.modifiers.new('Fine edge','BEVEL');b.width=.0007;b.segments=2
 # Thumb dorsal diagonal leather/metal articulation.
 for x,y,z in [(.955,.073,1.425),(.976,.094,1.412)]:
  o=cube('Armor_Thumb_'+str(side),(side*x,y,z+.009),(.014,.014,.006),Gold,ARMOR,.002);o.rotation_euler.z=side*.70
# Save independent geometry; all original meshes/textures remain external references only.
for c in [CORE,ARMOR,CLOTH,DETAIL]:
 for o in c.objects:o['provenance']='New geometry authored from visual reference; no source mesh copied'
cam=sc.camera;cam.location=(0,-5,1.02);cam.rotation_euler=(Vector((0,0,1.02))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=2.55
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'SunboundWarden_NewMesh_Armor_v001.blend'))
for name,pos in [('Front',(0,-5,1.02)),('Back',(0,5,1.02)),('ThreeQuarter',(3,-5,2.15))]:
 cam.location=pos;cam.rotation_euler=(Vector((0,0,1.02))-cam.location).to_track_quat('-Z','Y').to_euler();sc.render.filepath=str(OUT/('Preview_'+name+'.png'));bpy.ops.render.render(write_still=True)
