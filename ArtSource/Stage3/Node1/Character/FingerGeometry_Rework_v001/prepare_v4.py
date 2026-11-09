from pathlib import Path
p=Path('ArtSource/Stage3/Node1/Character/FingerGeometry_Rework_v001/rebuild_fingers_v3.py');s=p.read_text()
s=s.replace("# Round the cut boundary itself", "root_paint_coords=[v.co.copy() for v in root]\n # Round the cut boundary itself")
s=s.replace("recipes.append({'side':side", "recipes.append({'root_paint':root_paint_coords,'side':side")
s=s.replace("sample=oc+u*ry*fac*ca+up*rz*fac*sa;uv_pixels[yy,xx]=getter(sample)", "sample=oc+u*ry*fac*ca+up*rz*fac*sa\n   if t<.20:\n    rp=recipe['root_paint'];fraction=phi*len(rp);i=int(fraction)%len(rp);rootpoint=rp[i].lerp(rp[(i+1)%len(rp)],fraction-int(fraction));w=t/.20;w=w*w*(3-2*w);sample=rootpoint.lerp(sample,w)\n   uv_pixels[yy,xx]=getter(sample)")
s=s.replace("SunboundWarden_RebuiltFingers_v003.blend","SunboundWarden_RebuiltFingers_v004.blend")
p.with_name('rebuild_fingers_v4.py').write_text(s)
