import bpy,bmesh,pathlib,math
from mathutils import Vector
OUT=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/ArtSource/Stage3/Node1/Character/NewMesh_v001');bpy.ops.wm.open_mainfile(filepath=str(OUT/'SunboundWarden_NewMesh_Refined.blend'));sc=bpy.context.scene;CORE=bpy.data.collections['LAYER_1_Anatomical_Core'];ARMOR=bpy.data.collections['LAYER_2_Armor'];CLOTH=bpy.data.collections['LAYER_2_Cloth'];DETAIL=bpy.data.collections['LAYER_2_Armor_Detail'];REVIEW=bpy.data.collections['REVIEW_Studio']
Gold=bpy.data.materials['Warden_Antique_Gold'];LightGold=bpy.data.materials['Warden_Burnished_Edges'];DarkGold=bpy.data.materials['Warden_Recess_Bronze'];Purple=bpy.data.materials['Warden_Royal_Purple'];Leather=bpy.data.materials['Warden_Dark_Leather'];Dark=bpy.data.materials['Warden_Shadow_Recess'];Silver=bpy.data.materials['Warden_Warm_Steel']
code=(OUT/'build_new_mesh.py').read_text();exec(code[code.index('def objmesh'):code.index('# Muscle/garment')]);code=(OUT/'build_armor.py').read_text();exec(code[code.index('def place'):code.index('# Fill all visible')])
# Coordinates below follow the normalized figure.
f=.941  # measured approximate normalization from 2.02m to 1.90m; details scale with the figure.
def C(p):return tuple(v*f for v in p)
def line(name,pts,r,mat):return curve(name,[C(p) for p in pts],r*f,mat)
# Refined patinated metals; the reference has dark grooves and restrained gold.
for mat,mult,rough in [(Gold,.78,.43),(LightGold,.89,.36),(DarkGold,.9,.54)]:
 nt=mat.node_tree;p=nt.nodes.get('Principled BSDF');p.inputs['Roughness'].default_value=rough;mat['rough']=rough
 for ramp in [n for n in nt.nodes if n.type=='VALTORGB']:
  for e in ramp.color_ramp.elements:e.color=(*(v*mult for v in e.color[:3]),1)
# Fine engraved curls and nested border motifs use original design language.
for side in [1,-1]:
 # Cover exposed shoulder ball with the inner swept flange.
 outline=[(side*.188,-.116,1.475),(side*.224,-.115,1.565),(side*.278,-.132,1.570),(side*.276,-.139,1.479),(side*.234,-.138,1.425)];o=plate('Armor_InnerShoulderFlange',[C(p) for p in outline],Gold,ARMOR,.006*f,.002*f)
 line('Shoulder_Flange_Trim',outline+[outline[0]],.0023,LightGold)
 # Gold cheek inlay replaces the blunt uninterrupted face panels.
 line('Helmet_Cheek_Flute',[(side*.048,-.129,1.708),(side*.050,-.125,1.664),(side*.030,-.140,1.624)],.0016,DarkGold)
 line('Helmet_Forehead_Angle',[(side*.006,-.105,1.826),(side*.040,-.114,1.808),(side*.077,-.097,1.782)],.002,DarkGold)
 # Forearm flanks preserve purple leather with gold embroidery.
 for o in ARMOR.objects:
  if o.name.startswith('Armor_Bracer_Flank_'+str(side)):o.data.materials[0]=Purple
 for x in [.625,.699,.774]:
  outline=[(side*x,-.074,1.44),(side*(x+.018),-.066,1.463),(side*(x+.036),-.064,1.44),(side*(x+.018),-.070,1.413),(side*x,-.074,1.44)];line('Bracer_EmbroideredDiamond',outline,.0015,LightGold)
 # Ornament on hip plates and shin upper shield.
 for z,x in [(.91,.154),(.81,.17),(.71,.183)]:line('Tasset_EngravedV',[(side*(x-.012),-.132,z+.012),(side*(x+.018),-.137,z-.029),(side*(x+.035),-.12,z+.011)],.0015,DarkGold)
 line('Greave_Outer_Ridge',[(side*.158,-.075,.52),(side*.155,-.086,.375),(side*.143,-.085,.245)],.0014,DarkGold)
 # Rivets added at real plate corners.
 for pos in [(side*.236,-.131,1.491),(side*.399,-.114,1.462),(side*.14,-.103,.510),(side*.142,-.084,.238)]:sphere('Armor_Rivet',C(pos),(.003*f,)*3,LightGold,DETAIL)
# More defined helmet chin and cloth hood band.
outline=[(-.048,-.129,1.628),(0,-.150,1.598),(.048,-.129,1.628),(.035,-.129,1.596),(0,-.136,1.580),(-.035,-.129,1.596)];plate('Helmet_Chin_Guard',[C(p) for p in outline],Gold,ARMOR,.008*f,.002*f)
loftz('Cloth_Hood_Neck',[(z*f,rx*f,ry*f,0,cy*f) for z,rx,ry,cy in [(1.49,.088,.076,.011),(1.55,.078,.075,.016),(1.585,.077,.075,.016)]],Purple,CLOTH,20,1)
# Studio render settings and final native source.
sc.cycles.samples=48;sc.render.resolution_x=1100;sc.render.resolution_y=1100
cam=sc.camera;cam.data.ortho_scale=2.35;cam.location=(3,-5,1.98);cam.rotation_euler=(Vector((0,0,.94))-cam.location).to_track_quat('-Z','Y').to_euler()
notes=bpy.data.texts.new('README_New_Character');notes.write('New Sunbound Warden mesh, authored 2026-10-05 from visual reference. Original geometry, source UVs and source images were NOT reused. Anatomy/core, armor, cloth and details are independently editable in named collections. Height 1.90m including horns. Original and rejected rework remain separate. This is an unrigged modeling candidate; skin weights, animation deformation, final grip, Unity integration and device acceptance are pending. See adjacent README.md and verification JSON.\n')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'SunboundWarden_NewMesh_v001.blend'))
sc.render.filepath=str(OUT/'NewMesh_ThreeQuarter.png');bpy.ops.render.render(write_still=True)
for name,pos in [('Front',(0,-5,.94)),('Back',(0,5,.94))]:
 cam.location=pos;cam.rotation_euler=(Vector((0,0,.94))-cam.location).to_track_quat('-Z','Y').to_euler();sc.render.filepath=str(OUT/('NewMesh_'+name+'.png'));bpy.ops.render.render(write_still=True)
# Hand closeup proves five-finger anatomy and plate placement.
cam.data.ortho_scale=.27;center=Vector((.948,.014,1.349));cam.location=center+Vector((.5,-.55,.75));cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();sc.render.filepath=str(OUT/'NewMesh_Hand.png');bpy.ops.render.render(write_still=True)

