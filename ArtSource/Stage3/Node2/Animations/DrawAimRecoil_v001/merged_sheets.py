from PIL import Image,ImageDraw
from pathlib import Path
import math
p=Path('ArtSource/Stage3/Node2/Animations/DrawAimRecoil_v001')
for tag in ['Merged','JoinSide']:
 fs=sorted(p.glob(tag+'_*.png'));s=Image.new('RGB',(1600,math.ceil(len(fs)/8)*220));d=ImageDraw.Draw(s)
 for i,f in enumerate(fs):
  x=i%8*200;y=i//8*220;s.paste(Image.open(f).resize((200,200)),(x,y));d.text((x+4,y+202),f.stem,fill='white')
 s.save(p/(tag+'_Sheet.jpg'))
