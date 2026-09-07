import struct, json, sys
from pathlib import Path

data = Path(sys.argv[1]).read_bytes()
version = struct.unpack_from('<I', data, 23)[0]
wide = version >= 7500
header = 25 if wide else 13
def prop(pos):
    t = chr(data[pos]); pos += 1
    fmts = {'Y':'h','C':'?','I':'i','F':'f','D':'d','L':'q'}
    if t in fmts:
        fmt = '<' + fmts[t]
        return struct.unpack_from(fmt,data,pos)[0], pos+struct.calcsize(fmt)
    if t in 'SR':
        n = struct.unpack_from('<I',data,pos)[0]; pos += 4
        v = data[pos:pos+n]
        return (v.decode('utf-8',errors='replace') if t=='S' else {'bytes':n}), pos+n
    if t in 'fdlibc':
        count,enc,n = struct.unpack_from('<III',data,pos)
        return {'array':t,'count':count},pos+12+n
    raise ValueError(t)
def node(pos):
    end,count,size = struct.unpack_from('<QQQ' if wide else '<III',data,pos)
    if end == 0: return None,pos+header
    n = data[pos+header-1]; pos += header
    name = data[pos:pos+n].decode(); pos += n
    props=[]
    for _ in range(count):
        v,pos=prop(pos); props.append(v)
    children=[]
    while pos < end-header:
        ch,pos=node(pos)
        if ch is None: break
        children.append(ch)
    return {'name':name,'props':props,'children':children},end
nodes=[]; pos=27
while pos < len(data)-header:
    n,pos=node(pos)
    if n is None: break
    nodes.append(n)
objects=next(n for n in nodes if n['name']=='Objects')['children']
selected=[n for n in objects if n['name'] in ('Material','Texture','Video','LayeredTexture')]
connections=next(n for n in nodes if n['name']=='Connections')['children']
ids={n['props'][0] for n in selected}
result={'version':version,'objects':selected,'connections':[n['props'] for n in connections if any(p in ids for p in n['props'][1:3])]}
out=Path('tmp/fierd01-material-audit.json'); out.write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
for n in selected:
    print(n['name'],repr(n['props']))
    for ch in n['children']:
        if ch['name']=='Properties70':
            for p in ch['children']:
                if any(k in str(p['props'][0]).lower() for k in ('base','color','roughness','metal','transmission','emission','filename','path')):
                    print(' ',p['props'])
        else: print(' ',ch['name'],ch['props'])
print('connections',result['connections'])
