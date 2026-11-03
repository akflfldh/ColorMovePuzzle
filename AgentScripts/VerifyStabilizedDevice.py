"""ADB-only UI verification, with read-only PNG analysis; no external image packages.
Requires an approved update installation before --run. Never writes app storage.
"""
import argparse, collections, json, pathlib, struct, subprocess, time, zlib

ROOT = pathlib.Path(__file__).resolve().parents[1]
OUT = ROOT / "Artifacts/AndroidStabilization/Device"
ADB = "C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe"
SERIAL = "R3CY102P6FF"
PACKAGE = "com.colormovepuzzle.game"

def adb(*args):
    return subprocess.run([ADB, "-s", SERIAL, *map(str, args)], check=True, capture_output=True).stdout

def png(path):
    data = pathlib.Path(path).read_bytes()
    assert data[:8] == b"\x89PNG\r\n\x1a\n"
    pos, compressed = 8, bytearray()
    while pos < len(data):
        length = struct.unpack_from(">I", data, pos)[0]
        kind, content = data[pos+4:pos+8], data[pos+8:pos+8+length]
        if kind == b"IHDR": w,h,depth,color,_,_,interlace = struct.unpack(">IIBBBBB",content)
        if kind == b"IDAT": compressed.extend(content)
        pos += length + 12
    assert depth == 8 and color in (2,6) and interlace == 0
    bpp, raw = (3 if color == 2 else 4), zlib.decompress(compressed)
    stride, previous, pixels, offset = w*bpp, bytearray(w*bpp), bytearray(), 0
    for _ in range(h):
        mode = raw[offset]; row = bytearray(raw[offset+1:offset+1+stride]); offset += stride+1
        if mode:
            for x in range(stride):
                a, b, c = (row[x-bpp] if x>=bpp else 0), previous[x], (previous[x-bpp] if x>=bpp else 0)
                if mode == 1: predictor = a
                elif mode == 2: predictor = b
                elif mode == 3: predictor = (a+b)//2
                else:
                    p = a+b-c; distances = (abs(p-a),abs(p-b),abs(p-c))
                    predictor = (a,b,c)[distances.index(min(distances))]
                row[x] = (row[x]+predictor)&255
        pixels.extend(row); previous = row
    return w,h,bpp,pixels

def components(image, colors):
    w,h,bpp,data = image; step=4; cols=(w+3)//4; rows=(h+3)//4
    # Native color conversion can round a channel by one; keep tolerance far
    # below the distance between black tiles and empty-cell backgrounds.
    colors={bytes((max(0,min(255,r+dr)),max(0,min(255,g+dg)),max(0,min(255,b+db)))):label
            for (r,g,b),label in colors.items() for dr in range(-3,4) for dg in range(-3,4) for db in range(-3,4)}
    labels=[]
    for y in range(0,h,step):
        for x in range(0,w,step):
            p=(y*w+x)*bpp; labels.append(colors.get(bytes(data[p:p+3]),-1))
    result=[]
    for start,color in enumerate(labels):
        if color < 0: continue
        queue=collections.deque([start]);labels[start]=-1
        xmin=xmax=start%cols; ymin=ymax=start//cols; area=0
        while queue:
            index=queue.popleft();x=index%cols;y=index//cols;area+=1
            xmin=min(xmin,x);xmax=max(xmax,x);ymin=min(ymin,y);ymax=max(ymax,y)
            for neighbor in (index-1 if x else -1,index+1 if x+1<cols else -1,index-cols if y else -1,index+cols if y+1<rows else -1):
                if neighbor>=0 and labels[neighbor]==color:labels[neighbor]=-1;queue.append(neighbor)
        if area>100:
            result.append(dict(color=color,x=(xmin+xmax)*2,y=(ymin+ymax)*2,width=(xmax-xmin+1)*4,height=(ymax-ymin+1)*4,area=area*16))
    return result

def rgb(hex_value): return bytes.fromhex(hex_value)
def rows_of(boxes):
    rows=[]
    for box in sorted(boxes,key=lambda b:b["y"]):
        if not rows or abs(box["y"]-rows[-1][0]["y"])>box["height"]*.2:rows.append([])
        rows[-1].append(box)
    return [sorted(row,key=lambda b:b["x"]) for row in rows]
def capture(name):
    # Only photograph the approved app while its Activity is focused.
    focus=adb("shell","dumpsys","window").decode(errors="replace")
    assert any(PACKAGE in line for line in focus.splitlines() if "mCurrentFocus" in line), "App lost focus"
    time.sleep(.35);path=OUT/(name+".png");path.write_bytes(adb("exec-out","screencap","-p"));return png(path)
def tap(x,y): adb("shell","input","tap",round(x),round(y));time.sleep(.35)
def back(): adb("shell","input","keyevent",4);time.sleep(.35)
def swipe(a,b,duration=220): adb("shell","input","swipe",round(a[0]),round(a[1]),round(b[0]),round(b[1]),duration);time.sleep(.4)

def menu(image):
    scale=image[0]/1080
    boxes=[b for b in components(image,{rgb("DFE7F4"):1}) if abs(b["width"]-280*scale)<12*scale and abs(b["height"]-320*scale)<12*scale]
    assert len(boxes)==3, f"Main menu: expected three mode cards, got {len(boxes)}"
    return sorted(boxes,key=lambda b:b["x"]),scale

def cards(image, unlocked=False):
    scale=image[0]/1080
    colors={rgb("1C3B3C"):1,rgb("344662"):2}
    if not unlocked:colors[rgb("172338")]=3
    boxes=[b for b in components(image,colors) if 270*scale<b["width"]<310*scale and 215*scale<b["height"]<248*scale]
    assert boxes,"No full stage card found"
    return [box for row in rows_of(boxes) for box in row]

def board(image,size,models):
    scale=image[0]/1080;expected={3:253.3333,4:189,5:151.2}[size]*scale
    colors={rgb(c):i for i,c in enumerate(models["palette"])}
    boxes=[b for b in components(image,colors) if abs(b["width"]-expected)<12*scale and abs(b["height"]-expected)<12*scale]
    assert len(boxes)==size*size,f"Expected {size*size} board cells, found {len(boxes)}"
    boxes=[box for row in rows_of(boxes) for box in row]
    return [b["color"] for b in boxes],[(b["x"],b["y"]) for b in boxes]

def neighbors(cells,size,source):
    return [i for i,c in enumerate(cells) if c==0 and abs(i//size-source//size)+abs(i%size-source%size)==1] if cells[source] else []

def run():
    OUT.mkdir(parents=True,exist_ok=True)
    models=json.loads((OUT.parent/"DeviceModels.json").read_text(encoding="utf-8-sig"))
    baseline=json.loads((OUT.parent/"DeviceSavesBeforeUpdate.json").read_text(encoding="utf-8-sig"))
    if isinstance(baseline,dict):baseline=[baseline]
    saved={r["name"]:r["data"] for r in baseline if not r["name"].endswith(".bak")}
    profile=saved.get("menu-profile-v1.json",{})
    report={"passed":False,"running":True,"input":"ADB input tap/swipe/keyevent, not human touch feel","modes":[],"adbBackFailures":[]}
    report_path=OUT/"UiVerification.json"
    report_path.write_text(json.dumps(report,indent=2),encoding="utf-8")
    def enter(size,name):
        boxes,_=menu(capture(name+"-menu"));tap(boxes[size-3]["x"],boxes[size-3]["y"])
        visible=cards(capture(name+"-selection"),True);tap(visible[0]["x"],visible[0]["y"])
        cells,centers=board(capture(name+"-start"),size,models)
        stage=next(s for s in models["stages"] if s["size"]==size and s["cells"]==cells)
        return stage,cells,centers
    try:
        first_cards,scale=menu(capture("ui-initial-menu"));offset=first_cards[0]["y"]-1315*scale
        def to_selection(name):
            back();image=capture(name+"-keyback")
            try:cards(image)
            except AssertionError:
                report["adbBackFailures"].append(name+": keyevent 4 did not return to selection")
                tap(190*scale,100*scale+offset);cards(capture(name+"-ui-back"))
        def to_menu(name):
            back();image=capture(name+"-keyback")
            try:menu(image)
            except AssertionError:
                report["adbBackFailures"].append(name+": keyevent 4 did not return to main menu")
                tap(160*scale,94*scale+offset);menu(capture(name+"-ui-back"))
        for size in (3,4,5):
            name=f"grid{size}";stage,initial,centers=enter(size,name)
            source=next(i for i in range(size*size) if len(neighbors(initial,size,i))==1)
            destination=neighbors(initial,size,source)[0];tap(*centers[source])
            moved=initial.copy();moved[source],moved[destination]=moved[destination],moved[source]
            assert board(capture(name+"-tap"),size,models)[0]==moved,"Native tap did not move exactly one cell"
            swipe(centers[destination],centers[source])
            assert board(capture(name+"-swipe-back"),size,models)[0]==initial,"Native directional swipe failed"
            ambiguous=next((i for i in range(size*size) if len(neighbors(initial,size,i))>1),None)
            if ambiguous is not None:
                tap(*centers[ambiguous]);assert board(capture(name+"-ambiguous-tap"),size,models)[0]==initial
                to=neighbors(initial,size,ambiguous)[0];swipe(centers[ambiguous],centers[to])
                expected=initial.copy();expected[ambiguous],expected[to]=expected[to],expected[ambiguous]
                assert board(capture(name+"-direction"),size,models)[0]==expected
                swipe(centers[to],centers[ambiguous])
                assert board(capture(name+"-restored"),size,models)[0]==initial
            to_selection(name+"-selection");to_menu(name+"-menu")
            report["modes"].append(dict(size=size,stage=stage["number"],tap=True,swipe=True,ambiguous=ambiguous is not None,uiBack=True))
        # Restore every pre-existing mode's last-played stage through the game's UI.
        restore=[]
        for size in (3,4,5):
            data=saved.get("progress-v1.json" if size==3 else f"progress-{size}x{size}-v1.json")
            if data:restore.append((size,data["lastPlayed"]))
        if profile.get("hasContinue"):restore.append((profile["lastGridSize"],profile["lastStage"]))
        for index,(size,number) in enumerate(restore):
            boxes,scale=menu(capture(f"restore{index}-menu"));tap(boxes[size-3]["x"],boxes[size-3]["y"])
            row=(number-1)//3
            for _ in range(3):
                if row<5:swipe((540*scale,800*scale),(540*scale,1600*scale),500)
                else:swipe((540*scale,1600*scale),(540*scale,800*scale),500)
            visible_rows=rows_of(cards(capture(f"restore{index}-selection")))
            options=visible_rows[row] if row<5 else visible_rows[-1-(9-row)]
            cell=options[(number-1)%3];tap(cell["x"],cell["y"])
            expected=next(s["cells"] for s in models["stages"] if s["size"]==size and s["number"]==number)
            assert board(capture(f"restore{index}-board"),size,models)[0]==expected,"Could not restore original last-played stage"
            to_selection(f"restore{index}-selection");to_menu(f"restore{index}-menu")
        boxes,scale=menu(capture("final-menu"));offset=boxes[0]["y"]-1315*scale
        tap(540*scale,1660*scale+offset);capture("settings-restored");tap(540*scale,1510*scale+offset);menu(capture("settings-back"))
        if profile.get("hasContinue"):
            tap(540*scale,948*scale+offset)
            expected=next(s["cells"] for s in models["stages"] if s["size"]==profile["lastGridSize"] and s["number"]==profile["lastStage"])
            assert board(capture("continue-restored"),profile["lastGridSize"],models)[0]==expected
            to_selection("continue-selection");to_menu("continue-menu")
        report.update(passed=True,continueRestored=profile.get("hasContinue",False),originalLastPlayedRestored=True,settingsPanelOpened=True,adbBackPassed=not report["adbBackFailures"])
    except Exception as error:
        report["error"]=repr(error)
        raise
    finally:
        report["running"]=False;report_path.write_text(json.dumps(report,indent=2),encoding="utf-8")

if __name__=="__main__":
    parser=argparse.ArgumentParser();parser.add_argument("--run",action="store_true");parser.add_argument("--inspect");parser.add_argument("--capture");parser.add_argument("--grid",type=int,default=5);args=parser.parse_args()
    if args.run:run()
    elif args.capture:
        cards,_=menu(capture(args.capture));print(json.dumps(dict(mainMenu=True,modeCards=len(cards))))
    elif args.inspect:
        models=json.loads((ROOT/"Artifacts/AndroidStabilization/DeviceModels.json").read_text(encoding="utf-8-sig"))
        cells,centers=board(png(args.inspect),args.grid,models);print(json.dumps(dict(cells=cells,centers=centers)))
