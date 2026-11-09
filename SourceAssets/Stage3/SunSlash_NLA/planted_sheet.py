from PIL import Image,ImageDraw
from pathlib import Path
p=Path('SourceAssets/Stage3/SunSlash_NLA');fs=sorted(p.glob('Planted_*.png'));s=Image.new('RGB',(1800,6*275));d=ImageDraw.Draw(s)
for i,f in enumerate(fs):
 x=(i%9)*200;y=(i//9)*275;s.paste(Image.open(f).resize((200,217)),(x,y));d.text((x+5,y+220),f.stem,fill='white')
s.save(p/'planted_all_frames.jpg')

