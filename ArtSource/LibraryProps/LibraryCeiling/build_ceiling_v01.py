"""Modular SPELLSTRIKE study/archive vaulted ceiling; independent Blender build."""
import bpy, math, json
from pathlib import Path
from mathutils import Vector

HERE=Path(__file__).resolve().parent
PROJECT=HERE.parents[2]
OUT=PROJECT/'Assets/Cinematics/Storyboard3D/Environment/LibraryCeiling'
QA=PROJECT/'Library/StoryboardLibraryCeiling_20261023'
for p in (HERE,OUT,QA):p.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
s=bpy.context.scene
s.render.engine='BLENDER_EEVEE'
s.render.resolution_x=1280;s.render.resolution_y=850
s.render.image_settings.file_format='PNG'
s.view_settings.view_transform='Standard'
s.world=bpy.data.worlds.new('GothicCeilingPreviewWorld')
s.world.color=(.19,.20,.25)
c=bpy.data.collections.new('REMOVABLE_VAULT_BAYS')
s.collection.children.link(c)
preview=bpy.data.collections.new('PREVIEW_ONLY')
s.collection.children.link(preview)

def mat(name,col,metal=0):
 m=bpy.data.materials.new(name);m.diffuse_color=(*col,1);m.use_nodes=True
 b=m.node_tree.nodes.get('Principled BSDF')
 b.inputs['Base Color'].default_value=(*col,1)
 b.inputs['Metallic'].default_value=metal;b.inputs['Roughness'].default_value=.77
 return m
slate=mat('Ceiling_BlueSlate',(.09,.112,.14))
slate_b=mat('Ceiling_SlateVariation',(.114,.125,.138))
rib=mat('Ceiling_GothicDeepNavyRibs',(.037,.048,.073))
walnut=mat('Ceiling_WalnutCrossBeams',(.11,.061,.031))
gold=mat('Ceiling_AgedGoldTrim',(.47,.26,.067),.65)

def parent(name,y0,y1,room):
 root=bpy.data.objects.new(name,None);c.objects.link(root)
 root['room']=room;root['start_y']=y0;root['end_y']=y1
 root['toggle_for_cinematic_camera']='Disable this roof bay in Unity for close-up shots'
 return root
def tube(name,points,ma,root,width):
 curve=bpy.data.curves.new(name+'Path','CURVE');curve.dimensions='3D'
 curve.bevel_depth=width;curve.bevel_resolution=1
 sp=curve.splines.new('POLY');sp.points.add(len(points)-1)
 for p,xyz in zip(sp.points,points):p.co=(*xyz,1)
 ob=bpy.data.objects.new(name,curve);c.objects.link(ob);curve.materials.append(ma);ob.parent=root
def box(name,where,dim,ma,root):
 bpy.ops.mesh.primitive_cube_add(size=1,location=where)
 ob=bpy.context.object
 for old in list(ob.users_collection):old.objects.unlink(ob)
 c.objects.link(ob);ob.name=name;ob.dimensions=dim
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 ob.data.materials.append(ma);ob.parent=root

def height(x):
 t=1-abs(x)/7.0
 return 5.5+1.45*max(0,t)**.72

roots=[]
def bay(name,y0,y1,room,detail):
 root=parent(name,y0,y1,room)
 roots.append(root)
 verts=[];faces=[];mi=[]
 count=24
 for j in range(count):
  a=-7+14*j/count;b=-7+14*(j+1)/count
  aa=height(a);bb=height(b);n=len(verts)
  verts.extend([(a,y0,aa),(b,y0,bb),(b,y1,bb),(a,y1,aa),
                (a,y0,aa+.14),(b,y0,bb+.14),(b,y1,bb+.14),(a,y1,aa+.14)])
  for face in ((0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)):
   faces.append(tuple(n+x for x in face));mi.append(1 if j%7==0 else 0)
 me=bpy.data.meshes.new(name+'StoneMesh');me.from_pydata(verts,[],faces);me.update()
 obj=bpy.data.objects.new(name+'_VaultShell',me);c.objects.link(obj);obj.parent=root
 me.materials.append(slate);me.materials.append(slate_b)
 for f,i in zip(me.polygons,mi):f.material_index=i
 # Crosswise gothic arch ribs at BOTH boundaries of every independent bay.
 for side,y in (('S',y0+.05),('N',y1-.05)):
  for j in range(count):
   x0=-7+14*j/count;x1=-7+14*(j+1)/count
   z0=height(x0)-.065;z1=height(x1)-.065
   tube(name+'_Rib_'+side+'_'+str(j),[(x0,y,z0),(x1,y,z1)],rib,root,.058)
   if detail and j%2==0:
    tube(name+'_GoldPin_'+side+'_'+str(j),
         [(x0,y-.02,z0-.04),(x1,y-.02,z1-.04)],gold,root,.009)
 # Directional reinforcing beams and keystone ridge.
 for x in (-5.1,-3.1,3.1,5.1):
  box(name+'_Beam_'+str(x),(x,(y0+y1)/2,height(x)-.08),
      (.10,y1-y0,.15),walnut,root)
 box(name+'_KeystoneRidge',(0,(y0+y1)/2,6.82),(.19,y1-y0,.14),rib,root)
 if detail:box(name+'_GoldBoss',(0,(y0+y1)/2,6.71),(.17,.17,.06),gold,root)

for i in range(4):
 a=-11.5+2.875*i;bay('ROOF_STUDY_BAY_%02d'%(i+1),a,a+2.875,'study',True)
for i in range(3):
 a=3*i;bay('ROOF_ARCHIVE_BAY_%02d'%(i+1),a,a+3,'archive',False)

# Close the triangular void above the 5.5m straight masonry walls.
# Without these caps, Unity's camera sees the bright sky between the
# 5.5m top of the wall and the 6.95m ridge of the Gothic roof.
for title,y in [('SOUTH_FRONT',-11.5),('STUDY_ARCHIVE_PARTITION',0),('NORTH_ARCHIVE',9)]:
 root=parent('ROOF_GABLE_'+title,y-.09,y+.09,'upper_wall_infill')
 n=24;v=[];faces=[]
 for j in range(n):
  a=-7+14*j/n;b=-7+14*(j+1)/n
  verts=[(a,y-.085,5.5),(b,y-.085,5.5),(b,y-.085,height(b)),
         (a,y-.085,height(a)),(a,y+.085,5.5),(b,y+.085,5.5),
         (b,y+.085,height(b)),(a,y+.085,height(a))]
  start=len(v);v.extend(verts)
  for idxs in ((0,1,2,3),(4,7,6,5),(0,4,5,1),(1,5,6,2),
               (2,6,7,3),(3,7,4,0)):
   faces.append(tuple(start+i for i in idxs))
 me=bpy.data.meshes.new(title+'GableMesh');me.from_pydata(v,[],faces);me.update()
 ob=bpy.data.objects.new('ROOF_GABLE_'+title+'_SlateInfill',me);c.objects.link(ob)
 ob.parent=root;me.materials.append(slate)
 tube('ROOF_GABLE_'+title+'_CrownTrim',
      [(-7+14*j/n,y-.11,height(-7+14*j/n)-.045) for j in range(n+1)],
      rib,root,.046)

# The existing chandelier's ceiling-attachment origin is Z=5.35 at Y=-6.76,
# while the high Gothic vault apex is 6.95m. A matching long suspension chain
# visually bridges that 1.6m gap WITHOUT moving the carefully placed fixture.
fixture=roots[1]
fixture_y=-6.76
for j in range(11):
 z=5.425+j*.123
 points=[]
 for k in range(14):
  a=math.tau*k/14
  v=.067*math.cos(a);side=.028*math.sin(a)
  points.append((side if j%2==0 else 0,fixture_y+(0 if j%2==0 else side),z+v))
 tube('CHANDELIER_SuspensionLink_%02d'%(j+1),points,gold,fixture,.009)
box('CHANDELIER_CeilingHook',(0,fixture_y,6.80),(.12,.12,.24),gold,fixture)

def light(name,point,energy,color):
 data=bpy.data.lights.new(name,'AREA');data.energy=energy;data.size=9
 data.color=color
 ob=bpy.data.objects.new(name,data);preview.objects.link(ob);ob.location=point
 ob.rotation_euler=(Vector((0,-2,5.8))-ob.location).to_track_quat('-Z','Y').to_euler()
light('PreviewWarm',(-6,-9,2),1400,(1,.72,.46))
light('PreviewBlue',(5,6,2),1600,(.49,.65,1))
light('PreviewTop',(0,-2,11),450,(1,1,1))
data=bpy.data.cameras.new('QA_Camera')
camera=bpy.data.objects.new('QA_Camera',data);preview.objects.link(camera)
s.camera=camera;data.type='ORTHO'
def render(name,loc,target,scale):
 camera.location=loc
 camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler()
 data.ortho_scale=scale
 s.render.filepath=str(QA/(name+'.png'))
 bpy.ops.render.render(write_still=True)
render('01_VaultExterior',(20,-24,18),(0,-1,6),29)
render('02_VaultInterior',(0,-7,1.7),(0,-4.6,6.7),19)
render('03_VaultUnderside',(0,-2,1),(0,-2,6.8),19)

bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'Library_GothicVaultCeiling_v01.blend'),compress=True)
# Curves converted for Unity GLB exports, editable curve source remains in blend.
for ob in list(c.objects):
 if ob.type=='CURVE':
  bpy.ops.object.select_all(action='DESELECT')
  ob.select_set(True);bpy.context.view_layer.objects.active=ob
  bpy.ops.object.convert(target='MESH')
# One renderer per material PER roof section instead of hundreds of tiny ribs.
# Independent roots remain toggleable for cinematic camera cutaways.
for root in roots:
 groups={}
 for ob in list(root.children):
  if ob.type!='MESH':continue
  assert len(ob.data.materials) in (1,2)
  # Stone roof shell is its own single multi-material mesh.
  if len(ob.data.materials)>1:continue
  groups.setdefault(ob.data.materials[0].name,[]).append(ob)
 for matname,objects in groups.items():
  if len(objects)<2:continue
  bpy.ops.object.select_all(action='DESELECT')
  for obj in objects:obj.select_set(True)
  bpy.context.view_layer.objects.active=objects[0]
  bpy.ops.object.join()
  objects[0].name=root.name+'_MAT_'+matname
def export(name,selected):
 bpy.ops.object.select_all(action='DESELECT')
 for obj in selected:obj.select_set(True)
 p=OUT/name
 bpy.ops.export_scene.gltf(filepath=str(p),export_format='GLB',use_selection=True,export_apply=True)
 return {'filename':name,'bytes':p.stat().st_size,'triangles':sum(
   sum(len(f.vertices)-2 for f in o.data.polygons) for o in selected if o.type=='MESH')}
report={'all':export('Library_GothicVaultCeiling_v01.glb',list(c.objects)), 'sections':[]}
for root in roots:
 report['sections'].append(export(root.name+'.glb',[root]+list(root.children)))
assert len(report['sections'])==7
(QA/'CeilingQA.json').write_text(json.dumps(report,indent=2))
print('LIBRARY_CEILING_COMPLETE',json.dumps(report))
