from pathlib import Path
from PIL import Image,ImageDraw
import json,math
p=Path('ArtSource/Stage3/Node2/Animations/DrawAimRecoil_v001')
for tag in ['Draw','Recoil']:
 fs=sorted(p.glob(tag+'_*.png'));s=Image.new('RGB',(1600,math.ceil(len(fs)/8)*220));d=ImageDraw.Draw(s)
 for i,f in enumerate(fs):
  x=i%8*200;y=i//8*220;s.paste(Image.open(f).resize((200,200)),(x,y));d.text((x+4,y+202),f.stem,fill='white')
 s.save(p/(tag+'_Sheet.jpg'))
d=json.load(open(p/'source_motion.json'));a=d['Draw'][-1]['joints'];b=d['Recoil'][0]['joints']
for n in ['Hips','LeftHand','RightHand','LeftFoot','RightFoot','LeftArm','RightArm']:
 k='mixamorig:'+n;print(n,'position gap',math.dist(a[k]['pos'],b[k]['pos']),'rotation degrees',math.degrees(2*math.acos(min(1,abs(sum(x*y for x,y in zip(a[k]['q'],b[k]['q'])))))))
