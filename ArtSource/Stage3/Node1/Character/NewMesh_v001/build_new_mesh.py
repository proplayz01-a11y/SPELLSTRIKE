import bpy,bmesh,math,pathlib,json
from mathutils import Vector,Matrix
OUT=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/ArtSource/Stage3/Node1/Character/NewMesh_v001');OUT.mkdir(exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
sc=bpy.context.scene
CORE=bpy.data.collections.new('LAYER_1_Anatomical_Core');ARMOR=bpy.data.collections.new('LAYER_2_Armor');CLOTH=bpy.data.collections.new('LAYER_2_Cloth');DETAIL=bpy.data.collections.new('LAYER_2_Armor_Detail');REVIEW=bpy.data.collections.new('REVIEW_Studio')
for c in [CORE,ARMOR,CLOTH,DETAIL,REVIEW]:sc.collection.children.link(c)
def material(name,color,metal=0,rough=.5):
 m=bpy.data.materials.new(name);m.use_fake_user=True;m.diffuse_color=(*color,1);m.use_nodes=True;nt=m.node_tree;p=nt.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
 tex=nt.nodes.new('ShaderNodeTexNoise');tex.inputs['Scale'].default_value=55;tex.inputs['Detail'].default_value=3
 ramp=nt.nodes.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].position=.15;ramp.color_ramp.elements[0].color=(*(v*.72 for v in color),1);ramp.color_ramp.elements[1].position=.85;ramp.color_ramp.elements[1].color=(*(min(1,v*1.15) for v in color),1);nt.links.new(tex.outputs['Fac'],ramp.inputs[0]);nt.links.new(ramp.outputs[0],p.inputs['Base Color'])
 m['metal']=metal;m['rough']=rough;return m
Gold=material('Warden_Antique_Gold',(.39,.235,.095),.78,.34);LightGold=material('Warden_Burnished_Edges',(.64,.43,.18),.82,.29);DarkGold=material('Warden_Recess_Bronze',(.20,.105,.038),.72,.48);Purple=material('Warden_Royal_Purple',(.095,.030,.18),0,.64);Leather=material('Warden_Dark_Leather',(.038,.022,.06),0,.66);Dark=material('Warden_Shadow_Recess',(.012,.011,.019),.25,.45);Silver=material('Warden_Warm_Steel',(.25,.22,.17),.8,.37)
def objmesh(name,vs,fs,mat,coll,smooth=True,sub=0):
 me=bpy.data.meshes.new(name+'_Mesh');me.from_pydata(vs,[],fs);me.update();o=bpy.data.objects.new(name,me);coll.objects.link(o);me.materials.append(mat)
 bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
 for p in me.polygons:p.use_smooth=smooth
 if sub:s=o.modifiers.new('Surface refinement','SUBSURF');s.levels=sub;s.render_levels=sub
 return o
def loftz(name,rings,mat,coll,n=16,sub=0):
 vs=[];fs=[]
 for z,rx,ry,cx,cy in rings:
  for i in range(n):a=i*math.tau/n;vs.append((cx+rx*math.cos(a),cy+ry*math.sin(a),z))
 for j in range(len(rings)-1):
  for i in range(n):fs.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
 fs+=[tuple(reversed(range(n))),tuple((len(rings)-1)*n+i for i in range(n))];return objmesh(name,vs,fs,mat,coll,sub=sub)
def loftx(name,rings,mat,coll,n=16,sub=0):
 vs=[];fs=[]
 for x,ry,rz,cy,cz in rings:
  for i in range(n):a=i*math.tau/n;vs.append((x,cy+ry*math.cos(a),cz+rz*math.sin(a)))
 for j in range(len(rings)-1):
  for i in range(n):fs.append((j*n+i,(j+1)*n+i,(j+1)*n+(i+1)%n,j*n+(i+1)%n))
 fs+=[tuple(range(n)),tuple(reversed([(len(rings)-1)*n+i for i in range(n)]))];return objmesh(name,vs,fs,mat,coll,sub=sub)
def mirror(o,name=None,coll=None):
 c=o.copy();c.data=o.data.copy();(coll or o.users_collection[0]).objects.link(c);c.name=name or o.name.replace('_R','_L');c.scale.x*=-1;c.location.x*=-1;return c
# Muscle/garment core has deliberate joint rings and independent caps.
loftz('Core_Torso',[(.98,.15,.095,0,0),(1.01,.16,.105,0,0),(1.09,.16,.10,0,0),(1.20,.19,.115,0,0),(1.34,.235,.12,0,0),(1.43,.25,.115,0,0),(1.47,.20,.095,0,0),(1.50,.095,.075,0,0)],Leather,CORE,20,1)
loftz('Core_Pelvis',[(.88,.13,.09,0,0),(.94,.17,.11,0,0),(1.01,.17,.10,0,0)],Leather,CORE,16,1)
loftz('Core_Neck',[(1.46,.067,.064,0,0),(1.56,.064,.063,0,0)],Leather,CORE,16,1)
loftz('Core_Head',[(1.55,.06,.065,0,-.01),(1.59,.083,.088,0,-.015),(1.69,.092,.093,0,0),(1.77,.080,.085,0,0),(1.82,.02,.027,0,0)],Leather,CORE,20,1)
for label,rings in [('UpperArm',[(.235,.082,.090,0,1.435),(.28,.083,.082,0,1.435),(.38,.064,.075,0,1.435),(.48,.055,.063,0,1.435),(.53,.048,.047,0,1.435)]),('Forearm',[(.51,.048,.047,0,1.435),(.56,.064,.062,0,1.435),(.66,.062,.058,0,1.435),(.77,.047,.045,0,1.435),(.88,.036,.033,0,1.435),(.935,.033,.026,0,1.435)])]:
 o=loftx('Core_'+label+'_R',rings,Leather,CORE,16,1);mirror(o)
for label,rings in [('Thigh',[(.59,.065,.069,.12,0),(.64,.072,.075,.12,0),(.77,.09,.09,.105,0),(.91,.097,.095,.092,0),(.99,.085,.085,.09,0)]),('Shin',[(.15,.048,.05,.135,0),(.22,.05,.053,.13,0),(.40,.069,.071,.125,0),(.53,.067,.065,.12,0),(.61,.064,.064,.12,0)])]:
 o=loftz('Core_'+label+'_R',rings,Leather,CORE,16,1);mirror(o)
# Proper palm with four branched fingers and a radial thumb.
def hand():
 vs=[];fs=[]
 def add(p):vs.append(tuple(p));return len(vs)-1
 ys=[-.043,-.025,-.020,-.001,.004,.024,.029,.047];rows=[];cz=1.435
 for x,w,rz in [(.910,.68,.022),(.932,.86,.025),(.970,1,.025),(1.000,1,.019)]:
  rows.append([[add((x,y*w,cz+rz)) for y in ys],[add((x,y*w,cz-rz)) for y in ys]])
 for i in range(3):
  for j in range(7):fs.append((rows[i][0][j],rows[i+1][0][j],rows[i+1][0][j+1],rows[i][0][j+1]));fs.append((rows[i][1][j+1],rows[i+1][1][j+1],rows[i+1][1][j],rows[i][1][j]))
  fs.append((rows[i][0][0],rows[i][1][0],rows[i+1][1][0],rows[i+1][0][0]))
  if i!=1:fs.append((rows[i][0][-1],rows[i+1][0][-1],rows[i+1][1][-1],rows[i][1][-1]))
 for j in range(7):fs.append((rows[0][0][j+1],rows[0][1][j+1],rows[0][1][j],rows[0][0][j]))
 for j in [1,3,5]:fs.append((rows[-1][0][j],rows[-1][1][j],rows[-1][1][j+1],rows[-1][0][j+1]))
 for ix,(name,length,spread) in enumerate([('Little',.058,-.014),('Ring',.075,-.005),('Middle',.085,.002),('Index',.078,.01)]):
  j=ix*2;cy=(ys[j]+ys[j+1])/2;ry=(ys[j+1]-ys[j])/2;prev=[rows[-1][0][j],rows[-1][0][j+1],rows[-1][1][j+1],rows[-1][1][j]]
  for t,f in [(.07,1),(.27,.97),(.34,1),(.43,.90),(.60,.83),(.68,.87),(.76,.74),(.93,.63),(1,.3)]:
   x=1+length*t;y=cy+spread*t;z=cz-.020*t*t;r=ry*f;rz=.015*f;cur=[add((x,y-r,z+rz)),add((x,y+r,z+rz)),add((x,y+r,z-rz)),add((x,y-r,z-rz))]
   for k in range(4):fs.append((prev[k],cur[k],cur[(k+1)%4],prev[(k+1)%4]))
   prev=cur
  fs.append(tuple(prev))
 prev=[rows[1][0][-1],rows[2][0][-1],rows[2][1][-1],rows[1][1][-1]]
 for p,r in [((.947,.055,1.43),.014),((.957,.075,1.42),.013),((.971,.094,1.41),.011),((.99,.108,1.401),.009),((1.00,.111,1.399),.004)]:
  p=Vector(p);u=Vector((.8,-.6,0));cur=[add(p-u*r+Vector((0,0,r))),add(p+u*r+Vector((0,0,r))),add(p+u*r-Vector((0,0,r))),add(p-u*r-Vector((0,0,r)))]
  for k in range(4):fs.append((prev[k],cur[k],cur[(k+1)%4],prev[(k+1)%4]))
  prev=cur
 fs.append(tuple(prev));return objmesh('Core_Hand_R',vs,fs,Leather,CORE,sub=1)
h=hand();mirror(h)
# Feet have a planted sole, arched instep, heel and toe support rings.
for side in [1,-1]:
 o=loftz('Core_Foot_'+('R' if side==1 else 'L'),[(.02,.061,.12,side*.135,-.047),(.05,.063,.12,side*.135,-.047),(.10,.059,.105,side*.135,-.043),(.145,.048,.072,side*.135,-.009),(.19,.042,.048,side*.135,0)],Leather,CORE,16,1)
# Studio helpers.
def camera(name,pos,target,scale):
 d=bpy.data.cameras.new(name);o=bpy.data.objects.new(name,d);REVIEW.objects.link(o);o.location=pos;o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler();d.type='ORTHO';d.ortho_scale=scale;sc.camera=o;return o
cam=camera('ReviewCamera',(0,-5,1.0),(0,0,1.0),2.5)
sc.render.engine='BLENDER_WORKBENCH';sc.render.resolution_x=1000;sc.render.resolution_y=1000;sc.render.resolution_percentage=100;sc.world=bpy.data.worlds.new('StudioWorld');sc.world.color=(.10,.10,.10);sc.display.shading.light='STUDIO';sc.display.shading.color_type='MATERIAL';sc.display.shading.show_cavity=True;sc.display.shading.show_shadows=True
# Layer 1 geometry audit before armor authoring.
qa={}
for o in CORE.objects:
 bm=bmesh.new();bm.from_mesh(o.data);qa[o.name]={'vertices':len(bm.verts),'nonmanifold_edges':sum(not e.is_manifold for e in bm.edges)};bm.free()
assert all(x['nonmanifold_edges']==0 for x in qa.values()),qa
(OUT/'layer1_geometry_qa.json').write_text(json.dumps(qa,indent=2));bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'SunboundWarden_NewMesh_Layer1.blend'));sc.render.filepath=str(OUT/'Layer1_Front.png');bpy.ops.render.render(write_still=True)

