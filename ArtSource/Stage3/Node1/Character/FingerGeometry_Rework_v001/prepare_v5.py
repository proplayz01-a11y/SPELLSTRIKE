from pathlib import Path
p=Path('ArtSource/Stage3/Node1/Character/FingerGeometry_Rework_v001/rebuild_fingers_v4.py');s=p.read_text()
marker='for _ in range(5):bmesh.ops.smooth_vert(bm,verts=soft,factor=.36,use_axis_x=True,use_axis_y=True,use_axis_z=True)'
extra=marker+'''
# Remove the old pointed web wedges with a concave thumb/index transition.
for v in bm.verts:
 if v in newverts:continue
 rx=abs(v.co.x-.001069)
 if .833<rx<.854 and .131<v.co.y<.149 and 1.245<v.co.z<1.309:
  t=max(0,min(1,(.150-v.co.y)/.023));goal=.823+.032*t-.0025*math.sin(t*math.pi)
  if rx>goal:v.co.x=.001069+math.copysign(rx*.24+goal*.76,v.co.x-.001069)
 if .834<rx<.854 and .012<v.co.y<.023 and 1.24<v.co.z<1.28:
  v.co.y=v.co.y*.4+.023*.6
'''
s=s.replace(marker,extra).replace('SunboundWarden_RebuiltFingers_v004.blend','SunboundWarden_RebuiltFingers_v005.blend')
p.with_name('rebuild_fingers_v5.py').write_text(s)
