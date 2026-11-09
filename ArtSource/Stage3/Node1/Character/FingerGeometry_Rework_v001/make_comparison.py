from PIL import Image,ImageDraw,ImageFont
from pathlib import Path
p=Path('ArtSource/Stage3/Node1/Character/FingerGeometry_Rework_v001');out=Image.new('RGB',(1440,786),(26,26,29));d=ImageDraw.Draw(out);f=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',30)
for i,(image,label) in enumerate([('Original_HandTop.png','Original'),('HandTop.png','Rebuilt fingers')]):
 out.paste(Image.open(p/image).convert('RGB').resize((720,720)),(720*i,66));d.text((720*i+24,16),label,font=f,fill='white')
out.save(p/'Hand_Before_After.png')
