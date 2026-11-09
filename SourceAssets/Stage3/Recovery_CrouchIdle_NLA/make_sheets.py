from PIL import Image,ImageDraw
from pathlib import Path
P=Path(__file__).parent
for tag in ['Crouch','CrouchIdle','SlashReference','Final','Side']:
 files=sorted(P.glob(('Source_'+tag if tag not in ['Final','Side'] else tag)+'_*.png'))
 for part in range((len(files)+29)//30):
  group=files[part*30:(part+1)*30];sheet=Image.new('RGB',(1500,((len(group)+5)//6)*295),(25,28,32));d=ImageDraw.Draw(sheet)
  for i,f in enumerate(group):
   x=i%6*250;y=i//6*295;sheet.paste(Image.open(f).resize((250,275)),(x,y));d.text((x+5,y+277),f.stem,fill='white')
  sheet.save(P/(tag+'_Sheet_%d.jpg'%(part+1)))
 if tag=='Final' and files:
  frames=[Image.open(p).convert('RGB') for p in files]
  frames[0].save(P/'Recovery_CrouchIdle_Preview.gif',save_all=True,append_images=frames[1:],duration=[33,33,34]*(len(frames)//3)+[33]*(len(frames)%3),loop=0)
