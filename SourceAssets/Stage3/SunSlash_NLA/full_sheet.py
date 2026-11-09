from PIL import Image,ImageDraw
from pathlib import Path
p=Path('SourceAssets/Stage3/SunSlash_NLA/Full158');fs=sorted(p.glob('Frame_*.png'))
for part in range(4):
 s=Image.new('RGB',(1600,5*240));d=ImageDraw.Draw(s)
 for i,f in enumerate(fs[part*40:(part+1)*40]):
  x=(i%8)*200;y=(i//8)*240;s.paste(Image.open(f).resize((200,217)),(x,y));d.text((x+4,y+220),f.stem,fill='white')
 s.save(p/('AllFrames_%d.jpg'%(part+1)))
