"""Deterministic vector drawings, rendered at 2x and downsampled. No imagegen."""
from pathlib import Path
import json, math, re, html
from PIL import Image, ImageDraw, ImageFont
import aggdraw

ROOT=Path('Assets/VhsInheritance')
SOURCE=ROOT/'Art/Cartoon'
OUTPUT=ROOT/'Resources/Art/Cartoon'
SOURCE.mkdir(parents=True,exist_ok=True); OUTPUT.mkdir(parents=True,exist_ok=True)
INK='#302735'; LAV='#8874b3'; LIGHT='#c5afd8'; DEEP='#645580'; CREAM='#fff2cc'
TEAL='#79aaa7'; CORAL='#cb827f'; GOLD='#efcc84'; BROWN='#ae897e'; manifest=[]

class Art:
    def __init__(self,w,h):
        self.w=w; self.h=h; self.im=Image.new('RGBA',(w*2,h*2)); self.svg=[]
    def rr(self,x,y,w,h,r,fill,stroke=INK,sw=6):
        d=ImageDraw.Draw(self.im)
        d.rounded_rectangle((x*2,y*2,(x+w)*2,(y+h)*2),r*2,fill=fill,outline=stroke,width=int(sw*2) if stroke else 0)
        self.svg.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{r}" fill="{fill or "none"}" stroke="{stroke or "none"}" stroke-width="{sw}"/>')
    def ellipse(self,x,y,w,h,fill,stroke=INK,sw=6):
        ImageDraw.Draw(self.im).ellipse((x*2,y*2,(x+w)*2,(y+h)*2),fill=fill,outline=stroke,width=int(sw*2) if stroke else 0)
        self.svg.append(f'<ellipse cx="{x+w/2}" cy="{y+h/2}" rx="{w/2}" ry="{h/2}" fill="{fill or "none"}" stroke="{stroke or "none"}" stroke-width="{sw}"/>')
    def path(self,commands,fill=None,stroke=INK,sw=6):
        tokens=re.findall(r'[MLCQZmlcqz]|-?\d+(?:\.\d+)?',commands)
        p=aggdraw.Path(); i=0; x=y=0
        while i<len(tokens):
            cmd=tokens[i].upper(); i+=1
            if cmd=='Z': p.close(); continue
            count={'M':2,'L':2,'C':6,'Q':4}[cmd]
            a=list(map(float,tokens[i:i+count])); i+=count
            if cmd=='M': x,y=a; p.moveto(x*2,y*2)
            elif cmd=='L': x,y=a; p.lineto(x*2,y*2)
            elif cmd=='C': p.curveto(*[v*2 for v in a]); x,y=a[-2:]
            elif cmd=='Q':
                cx,cy,nx,ny=a
                p.curveto((x+2*(cx-x)/3)*2,(y+2*(cy-y)/3)*2,(nx+2*(cx-nx)/3)*2,(ny+2*(cy-ny)/3)*2,nx*2,ny*2); x,y=nx,ny
        c=aggdraw.Draw(self.im); c.path(p,aggdraw.Brush(fill) if fill else None,aggdraw.Pen(stroke,sw*2) if stroke else None); c.flush()
        self.svg.append(f'<path d="{commands}" fill="{fill or "none"}" stroke="{stroke or "none"}" stroke-width="{sw}" stroke-linecap="round" stroke-linejoin="round"/>')
    def text(self,x,y,value,size,fill=INK):
        font=ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf',size*2)
        ImageDraw.Draw(self.im).text((x*2,y*2),value,font=font,fill=fill,anchor='mm')
        self.svg.append(f'<text x="{x}" y="{y}" fill="{fill}" font-family="sans-serif" font-weight="bold" font-size="{size}" text-anchor="middle" dominant-baseline="middle">{html.escape(value)}</text>')
    def save(self,name,ppu,pivot=(.5,.08)):
        self.im.resize((self.w,self.h),Image.Resampling.LANCZOS).save(OUTPUT/(name+'.png'))
        (SOURCE/(name+'.svg')).write_text(f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {self.w} {self.h}">'+''.join(self.svg)+'</svg>',encoding='utf-8')
        manifest.append({'name':name,'ppu':ppu,'pivotX':pivot[0],'pivotY':pivot[1]})

# The entire room: herringbone parquet, teal patterned wallpaper, street windows.
a=Art(1920,1080)
a.rr(0,0,1920,1080,0,BROWN,None)
for y in range(210,1200,82):
    for x in range(-130,2050,164):
        offset=82 if (y//82)%2 else 0; xx=x+offset
        a.path(f'M {xx} {y} L {xx+82} {y-58} L {xx+164} {y} L {xx+82} {y+58} Z',None,'#94746f',3)
a.rr(0,0,1920,255,0,TEAL,None)
for y in [32,101,174]:
    for x in range(42,1920,93):
        a.ellipse(x,y,18,24,'#b8c4a2',None); a.path(f'M {x+25} {y+9} Q {x+43} {y-8} {x+50} {y+20}',None,'#c8919b',5)
        a.ellipse(x+35,y+9,13,10,'#c8919b',None)
a.rr(0,232,1920,27,9,DEEP,INK,5)
for x in [650,976]:
    a.rr(x,6,290,218,18,DEEP,INK,8)
    a.rr(x+13,18,264,176,9,'#b3c6c6',INK,5)
    a.path(f'M {x+20} 120 Q {x+100} 104 {x+140} 115 Q {x+210} 130 {x+270} 105 L {x+270} 185 L {x+20} 185 Z','#8ea3a9',None)
    a.path(f'M {x+145} 22 L {x+145} 190',None,INK,5)
    a.path(f'M {x+28} 27 L {x+93} 99 M {x+49} 28 L {x+114} 99',None,'#d2dddd',4)
a.rr(590,17,70,190,10,CORAL,INK,5)
a.rr(1265,17,70,190,10,CORAL,INK,5)
a.rr(735,214,450,61,20,CREAM,INK,6); a.text(960,246,'ВИДЕОСАЛОН',29,DEEP)
a.save('background',96,(.5,.5))

# Wide shallow shelves. The bottom is the physical footprint; the upper facade is decorative.
a=Art(400,300)
a.rr(19,16,363,256,28,LAV,INK,9)
a.rr(29,28,341,61,21,LIGHT,None)
a.path('M 53 39 Q 170 25 319 39',None,'#e3d2eb',6)
for y in [94,172]:
    a.rr(36,y,328,66,12,DEEP,INK,6)
    a.rr(36,y+49,328,21,6,LAV,INK,5)
a.rr(18,243,364,35,12,DEEP,INK,7)
a.rr(39,274,49,14,6,INK,None); a.rr(311,274,49,14,6,INK,None)
a.save('fixture_Shelf',140,(.5,.08))

a=Art(760,260)
a.rr(18,61,724,175,26,DEEP,INK,9)
a.rr(34,110,692,110,16,LAV,INK,6)
for x in [68,330,590]: a.rr(x,128,98,57,12,DEEP,INK,4)
a.rr(13,46,734,71,24,LIGHT,INK,8); a.path('M 45 59 Q 334 43 702 59',None,'#e3d2eb',7)
a.rr(409,12,135,55,11,CORAL,INK,6); a.rr(426,23,74,23,7,CREAM,INK,3)
a.rr(559,19,91,41,10,CREAM,INK,5); a.text(604,40,'VHS',21,DEEP)
a.save('fixture_Counter',145,(.5,.12))

a=Art(300,430)
a.rr(49,32,207,365,37,DEEP,INK,9); a.rr(65,40,170,311,24,LAV,INK,6)
a.rr(79,58,143,102,22,CREAM,INK,6); a.rr(92,72,116,67,17,TEAL,INK,4)
a.path('M 105 104 Q 127 74 143 105 Q 162 133 193 98',None,'#d5edc5',6)
a.rr(74,180,164,50,16,DEEP,INK,6); a.rr(86,188,139,21,7,INK,None)
a.rr(69,259,176,77,15,LIGHT,INK,5)
for x,c in [(96,CORAL),(151,GOLD),(206,TEAL)]: a.ellipse(x-14,273,29,29,c,INK,4)
a.path('M 113 355 L 113 373 M 138 355 L 138 373 M 164 355 L 164 373 M 190 355 L 190 373',None,INK,5)
a.rr(40,388,227,23,9,INK,None)
a.ellipse(130,5,39,35,CORAL,INK,6)
a.save('fixture_Machine',120,(.5,.05))

a=Art(300,320)
a.rr(34,268,232,30,12,DEEP,INK,7)
a.rr(32,67,235,205,36,LAV,INK,8)
a.path('M 126 67 Q 88 21 78 19 M 162 67 Q 177 28 201 16',None,INK,7)
a.rr(48,87,171,138,25,INK,INK,5); a.rr(60,100,145,111,22,TEAL,CREAM,4)
a.path('M 81 174 Q 111 122 128 167 Q 155 205 190 139',None,'#c8d8b0',5)
a.ellipse(230,115,20,20,GOLD,INK,4); a.ellipse(230,153,20,20,CORAL,INK,4)
a.rr(68,240,145,28,9,DEEP,INK,4); a.rr(80,246,92,9,4,INK,None)
a.save('fixture_Television',125,(.5,.05))

a=Art(280,265)
a.rr(40,115,202,125,23,LAV,INK,7); a.rr(26,97,228,39,17,LIGHT,INK,7)
a.rr(69,149,143,67,17,DEEP,INK,5)
a.rr(97,19,88,99,19,CORAL,INK,6); a.rr(112,35,57,51,13,CREAM,INK,4)
a.path('M 128 57 L 143 72 L 155 49',None,DEEP,5)
a.save('fixture_UpgradeStand',125,(.5,.08))

a=Art(320,200)
a.rr(15,24,290,143,27,DEEP,INK,8); a.rr(33,37,254,115,20,TEAL,INK,6)
a.rr(48,57,224,76,16,CREAM,INK,4); a.text(160,95,'ДО ЗАВТРА',22,DEEP)
a.rr(34,162,251,18,8,INK,None)
a.save('fixture_Door',135,(.5,.08))

# Each cassette is a separate entity with an actual cover design.
colors={'Action':CORAL,'Comedy':GOLD,'Drama':LIGHT,'Audio':TEAL,'foreign':'#a3cabb'}
for genre,color in colors.items():
    a=Art(320,200)
    a.rr(9,17,302,172,27,DEEP,INK,8); a.rr(19,20,280,147,20,'#50485e',INK,4)
    a.rr(35,39,251,105,18,CREAM,INK,5)
    a.rr(51,47,217,35,12,color,None)
    if genre=='Action': a.path('M 145 52 L 135 65 L 156 63 L 150 76 M 184 51 L 173 65 L 193 64 L 188 76',None,INK,4)
    elif genre=='Comedy': a.ellipse(145,50,31,25,CREAM,INK,3); a.path('M 150 66 Q 160 75 171 64',None,INK,3)
    elif genre=='Drama': a.path('M 150 53 C 133 46 132 65 158 75 C 183 61 183 47 165 52 Q 157 46 150 53 Z',CORAL,INK,2)
    elif genre=='Audio': a.path('M 144 74 L 144 54 L 170 51 L 170 69',None,INK,4); a.ellipse(136,66,13,10,INK,None); a.ellipse(162,62,13,10,INK,None)
    else: a.text(160,64,'?',29,DEEP)
    a.rr(103,92,115,30,8,DEEP,INK,4)
    for x in [75,248]:
        a.ellipse(x-25,85,51,48,LAV,INK,5); a.ellipse(x-9,100,17,17,CREAM,INK,2)
        for angle in range(0,360,90):
            dx=math.cos(math.radians(angle))*17; dy=math.sin(math.radians(angle))*17
            a.path(f'M {x+dx*.62:.2f} {109+dy*.62:.2f} L {x+dx:.2f} {109+dy:.2f}',None,CREAM,3)
    a.rr(118,153,86,20,8,LAV,INK,3)
    for x in [30,286]: a.ellipse(x-4,153,8,8,LIGHT,None)
    a.save('cassette_'+genre,142,(.5,.5))
    if genre!='foreign':
        b=Art(60,136); b.rr(5,5,50,126,10,DEEP,INK,5); b.rr(10,12,40,110,8,color,None); b.rr(15,56,30,32,6,CREAM,INK,3)
        b.save('slot_'+genre,180,(.5,.5))
        b.ellipse(39,5,16,17,CORAL,INK,2); b.text(47,14,'!',11,CREAM)
        b.save('slot_'+genre+'_wrong',180,(.5,.5))

# Big heads, compact bodies, soft silhouettes and different personalities.
def character(name,shirt,skin,hair,variant=0,mystery=False):
    a=Art(300,390)
    a.rr(84,324,55,41,17,DEEP,INK,7); a.rr(168,324,55,41,17,DEEP,INK,7)
    a.path('M 87 219 Q 58 243 61 290 Q 63 312 82 311 L 89 282 L 85 332 Q 147 349 221 332 L 215 281 Q 218 321 238 310 Q 263 296 239 244 Q 224 217 201 214 Z',shirt,INK,8)
    a.rr(101,277,104,30,12,LIGHT if shirt!=LIGHT else TEAL,INK,4)
    a.rr(125,192,54,45,20,skin,INK,5)
    if mystery:
        a.path('M 48 156 Q 42 37 131 10 Q 225 2 258 131 L 232 232 Q 165 259 81 224 Z',CORAL,INK,9)
    a.ellipse(58,65,193,172,skin,INK,8)
    a.path('M 68 127 C 47 77 82 28 138 29 C 194 12 246 43 244 106 C 219 122 210 77 185 88 Q 161 115 137 75 Q 98 116 68 127 Z',hair,INK,7)
    a.ellipse(75,133,72,70,CREAM,INK,6); a.ellipse(152,133,72,70,CREAM,INK,6)
    a.ellipse(98+(variant%3)*4,148,35,47,INK,None); a.ellipse(163+(variant%2)*7,149,35,47,INK,None)
    a.ellipse(108,152,9,10,CREAM,None); a.ellipse(175,153,9,10,CREAM,None)
    a.path('M 85 126 Q 110 108 137 122 M 161 120 Q 181 104 211 125',None,hair,9)
    a.path('M 131 216 Q 153 229 176 211',None,INK,5)
    a.ellipse(74,206,20,9,'#d99290',None); a.ellipse(212,203,20,9,'#d99290',None)
    if variant==2:
        a.rr(72,142,77,57,17,None,INK,8); a.rr(153,142,77,57,17,None,INK,8); a.path('M 149 162 L 155 162',None,INK,7)
    if variant==3:
        a.rr(65,42,177,58,22,TEAL,INK,7); a.rr(45,88,206,19,9,TEAL,INK,6)
    if mystery: a.path('M 64 114 Q 150 5 242 115',None,CREAM,4)
    a.save(name,168,(.5,.08))
character('player',TEAL,'#edc8a6','#6b515c',0)
character('assistant',GOLD,'#c99479','#ddd7bd',2)
character('mystery',CORAL,'#daa3b6','#67526c',1,True)
for i,(shirt,skin,hair) in enumerate([(LIGHT,'#ecd0b5','#765b56'),(CORAL,'#c99479','#504650'),(TEAL,'#ead2b8','#dfd8c8'),(GOLD,'#e7b8a9','#896565'),('#a6b89c','#b78870','#5c4750'),('#c594b4','#f0c9a9','#997759')]):
    character('visitor_'+str(i),shirt,skin,hair,i)

a=Art(200,160)
a.path('M 84 115 L 104 149 L 123 114',CREAM,INK,6); a.rr(10,8,180,113,35,CREAM,INK,7)
a.rr(91,29,19,44,9,CORAL,INK,3); a.ellipse(92,85,16,15,CORAL,INK,3)
a.save('bubble',160,(.5,.1))

a=Art(400,220); a.rr(8,8,384,204,70,'#ba92ad',INK,7); a.rr(28,26,344,168,59,DEEP,CREAM,5)
a.save('decoration_Rug',110,(.5,.5))
a=Art(280,350)
a.path('M 74 259 L 95 328 Q 143 349 195 328 L 219 259 Z',CORAL,INK,7)
a.rr(61,244,171,31,13,CREAM,INK,6)
for x,y,w,h,rot in [(99,25,51,129,0),(32,90,105,70,0),(140,62,90,77,0),(35,168,115,67,0),(141,158,92,65,0)]:
    a.ellipse(x,y,w,h,'#94b59b',INK,5)
a.path('M 145 254 L 128 78 M 145 211 L 85 130 M 143 199 L 190 110',None,DEEP,5)
a.save('decoration_Plant',138,(.5,.06))
a=Art(220,330); a.rr(12,8,196,312,20,CREAM,INK,7); a.rr(26,28,167,268,15,CORAL,None)
a.ellipse(61,62,100,100,LIGHT,INK,5); a.path('M 47 249 Q 103 126 174 249 Z',TEAL,INK,5)
a.text(110,276,'СКОРО В КИНО',17,CREAM); a.save('decoration_Poster',135,(.5,.05))
a=Art(190,220); a.rr(36,40,125,154,23,DEEP,INK,6); a.rr(21,24,150,34,15,LAV,INK,7)
for x in [59,97,135]: a.path(f'M {x} 78 L {x} 168',None,LIGHT,5)
a.save('decoration_TrashCan',120,(.5,.05))
a=Art(230,150); a.rr(11,21,208,110,20,GOLD,INK,7); a.path('M 119 29 L 119 127 M 29 52 L 201 52',None,CREAM,13)
a.rr(35,76,54,35,7,CREAM,INK,3); a.text(62,94,'VHS',16,DEEP); a.save('decoration_CardboardBox',120,(.5,.08))
a=Art(250,120); a.path('M 15 72 C 52 15 218 22 205 68 C 196 102 45 99 54 61 C 62 31 183 37 161 73 Q 130 100 226 106',None,DEEP,9)
a.save('decoration_CableCoil',120,(.5,.5))

# Transparent overlays; no painted grain or glow wash over the illustrations.
a=Art(300,90); a.ellipse(13,11,274,67,'#604b6530',None); a.save('shadow',150,(.5,.5))
for name,w,h,ppu in [('glow',128,128,32),('vignette',1920,1080,96),('fixture_Pile',104,52,32)]:
    Art(w,h).save(name,ppu,(.5,.5))
(SOURCE/'import-settings.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print(f'Drew {len(manifest)} cartoon sprites and saved vector originals.')
