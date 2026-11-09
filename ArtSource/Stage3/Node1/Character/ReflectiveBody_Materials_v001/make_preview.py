from PIL import Image,ImageDraw,ImageFont
from pathlib import Path
p=Path('ArtSource/Stage3/Node1/Character/ReflectiveBody_Materials_v001');font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',23);frames=[]
for side in ['Left','Front','Right']:
 im=Image.open(p/('After_'+side+'_v002.png')).convert('RGB').resize((600,660));sheet=Image.new('RGB',(600,704),(17,20,25));sheet.paste(im,(0,44));ImageDraw.Draw(sheet).text((18,9),'Key light: '+side,font=font,fill='white');frames.append(sheet)
frames[0].save(p/'ReflectiveBody_LightAngles.gif',save_all=True,append_images=frames[1:]+[frames[1]],duration=1000,loop=0)
comp=Image.new('RGB',(1200,704),(17,20,25));d=ImageDraw.Draw(comp)
for i,(name,label) in enumerate([('Before_Front_v002.png','Original material'),('After_Front_v002.png','Reflective body material')]):
 comp.paste(Image.open(p/name).convert('RGB').resize((600,660)),(i*600,44));d.text((i*600+18,9),label,font=font,fill='white')
comp.save(p/'Material_Before_After.png')
