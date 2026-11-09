"""Deterministic 2D texture atlases for the antique globe and walnut desk.

Run with Windows Python (Pillow installed), then Blender builder. Source
reference: user's Oct 22 cropped library image, globe on small side table.
"""
from PIL import Image, ImageDraw, ImageFilter
from pathlib import Path
import random
import math

DIR=Path(__file__).resolve().parent
DIR.mkdir(parents=True,exist_ok=True)
rng=random.Random(3478)

# A parchment equirectangular sphere, no readable labels or fake writing.
w,h=1024,512
img=Image.new('RGB',(w,h))
px=img.load()
for y in range(h):
    lat=(y/(h-1)-0.5)*math.pi
    for x in range(w):
        u=x/w
        scratch=(rng.random()-.5)*10
        m=math.sin(u*math.tau*9.3)*6+math.sin(u*math.tau*37+lat*8)*2.7
        old=math.sin(lat*8.4+math.sin(u*math.tau*4))*5
        pole=-11*(abs(lat)/(math.pi/2))**1.7
        c=int(max(-25,min(24,scratch+m+old+pole)))
        px[x,y]=(max(0,min(255,175+c)),max(0,min(255,139+c)),
                 max(0,min(255,94+c)))
draw=ImageDraw.Draw(img,'RGBA')

# Grid lines engraved on the parchment (an old celestial/earth globe fusion).
for k in range(1,12):
    x=int(w*k/12)
    draw.line((x,0,x,h),fill=(61,39,24,72),width=1)
for k in range(1,8):
    y=int(h*k/8)
    draw.line((0,y,w,y),fill=(60,39,19,70),width=1)

# Irregular archipelago ink forms, with weathered stippled interiors. Keep
# edges subtle and winding; the rings make the silhouette, texture adds life.
continents=[
    [(72,110),(104,89),(163,112),(188,149),(209,175),(190,219),
     (167,236),(145,272),(117,257),(104,219),(75,199),(54,160)],
    [(224,282),(254,275),(278,311),(293,348),(272,374),(264,419),
     (240,455),(218,416),(212,373),(193,328)],
    [(401,86),(464,64),(504,91),(558,93),(589,120),(624,109),
     (674,145),(709,169),(667,192),(633,190),(596,220),(574,218),
     (542,195),(497,213),(466,171),(425,170)],
    [(466,240),(506,231),(541,257),(551,306),(529,359),(495,389),
     (478,365),(444,332),(434,283)],
    [(722,170),(759,143),(806,156),(830,209),(807,260),(774,261),
     (757,225),(723,214)],
    [(765,337),(808,317),(853,338),(871,376),(837,408),(787,394)],
]
for poly in continents:
    draw.polygon(poly,fill=(77,54,31,30))
    draw.line(poly+[poly[0]],fill=(64,42,20,130),width=2,joint='curve')
    for i in range(75):
        x,y=rng.choice(poly)
        xx=x+rng.randrange(-24,25);yy=y+rng.randrange(-24,25)
        if 0<=xx<w and 0<=yy<h:
            draw.line((xx,yy,xx+rng.randrange(1,7),yy+rng.randrange(-2,3)),
                      fill=(55,36,18,rng.randrange(18,55)),width=1)
for k in range(42):
    x=rng.randrange(w);y=rng.randrange(55,h-55)
    radius=rng.randint(1,3)
    draw.arc((x-radius,y-radius,x+radius,y+radius),
             rng.randint(0,90),rng.randint(180,360),
             fill=(47,34,21,60),width=1)
img.save(DIR/'Globe_AntiqueParchment_1024x512.png',optimize=True)

# Dark walnut with grain, knots and two tone streaks; tiled UV on furniture.
w,h=1024,512
img=Image.new("RGB",(w,h))
px=img.load()
for y in range(h):
    for x in range(w):
        u=x/w;v=y/h
        ripple=(math.sin(v*85+5*math.sin(u*math.tau*1.37))+
                .45*math.sin(v*160+3*math.sin(u*math.tau*2.3))+
                .2*math.sin(v*325))
        base=ripple*6+(rng.random()-.5)*4
        varnish=4*math.sin(u*math.tau*.8)
        k=int(base+varnish)
        px[x,y]=(max(0,min(255,73+k)),max(0,min(255,39+k//2)),
                 max(0,min(255,20+k//3)))
draw=ImageDraw.Draw(img,"RGBA")
for i in range(21):
    y=rng.randrange(10,h-10)
    offset=rng.randrange(5,130)
    pts=[(k,y+5*math.sin(k*.014+offset)) for k in range(0,w+1,12)]
    draw.line(pts,fill=(16,7,2,rng.randrange(18,50)),width=1)
img.save(DIR/'Walnut_AgedGrain_1024x512.png',optimize=True)
print("CREATED",DIR/'Globe_AntiqueParchment_1024x512.png',
      DIR/'Walnut_AgedGrain_1024x512.png')
