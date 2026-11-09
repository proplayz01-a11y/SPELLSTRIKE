from PIL import Image,ImageDraw
from pathlib import Path
P=Path(__file__).parent
for tag in ['Crouch','Run','CurrentRush','Final','Side']:
 files=sorted(P.glob(('Source_'+tag if tag in ['Crouch','Run','CurrentRush'] else tag)+'_*.png'))
 for part in range((len(files)+29)//30):
  group=files[part*30:(part+1)*30];s=Image.new('RGB',(1500,((len(group)+5)//6)*295),(25,28,32));d=ImageDraw.Draw(s)
  for i,p in enumerate(group):
   x=i%6*250;y=i//6*295;s.paste(Image.open(p).resize((250,275)),(x,y));d.text((x+5,y+277),p.stem,fill='white')
  s.save(P/(tag+'_Sheet_%d.jpg'%(part+1)))
 if tag=='Final' and files:
  images=[Image.open(p).convert('RGB').resize((400,440)) for p in files]
  images[0].save(P/'RadiantRush_ShortHeavy_Preview.gif',save_all=True,append_images=images[1:],duration=[33,33,34]*(len(images)//3)+[33]*(len(images)%3),loop=0)
