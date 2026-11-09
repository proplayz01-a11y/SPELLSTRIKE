from pathlib import Path
p=Path('ArtSource/Stage3/Node1/Character/FingerGeometry_Rework_v001/rebuild_fingers_v2.py');s=p.read_text()
marker='prev=root;uvtargets={};params_uv='
replace="""# Round the cut boundary itself so narrow new shafts do not meet angular source flaps.
 for k,v in enumerate(root):
  aa=baseangle+k*math.tau/n;cc=math.copysign(abs(math.cos(aa))**.64,math.cos(aa));ss=math.copysign(abs(math.sin(aa))**.60,math.sin(aa))
  rootu=Vector((side,0,0)) if isthumb else Vector((0,1,0));target=c+rootu*(ry*1.2*cc)+Vector((0,0,rz*1.18*ss))
  if isthumb:target.y=v.co.y
  else:target.x=v.co.x
  v.co=v.co.lerp(target,.52)
 prev=root;uvtargets={};params_uv="""
s=s.replace(marker,replace).replace('range(3):bmesh.ops.smooth_vert(bm,verts=soft,factor=.32','range(5):bmesh.ops.smooth_vert(bm,verts=soft,factor=.36')
s=s.replace("SunboundWarden_RebuiltFingers_v002.blend","SunboundWarden_RebuiltFingers_v003.blend")
p.with_name('rebuild_fingers_v3.py').write_text(s)
