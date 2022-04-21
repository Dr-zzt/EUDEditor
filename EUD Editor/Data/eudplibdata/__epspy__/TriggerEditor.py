## NOTE: THIS FILE IS GENERATED BY EPSCRIPT! DO NOT MODITY
from eudplib import *

def _RELIMP(path, mod_name):
    import inspect, pathlib, importlib.util
    p = pathlib.Path(inspect.getabsfile(inspect.currentframe())).parent
    for s in path.split("."):
        if s == "":  p = p.parent
        else:  p = p / s
    try:
        spec = importlib.util.spec_from_file_location(mod_name, p / (mod_name + ".py"))
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
    except FileNotFoundError:
        loader = EPSLoader(mod_name, str(p / (mod_name + ".eps")))
        spec = importlib.util.spec_from_loader(mod_name, loader)
        module = loader.create_module(spec)
        loader.exec_module(module)
    return module

def _IGVA(vList, exprListGen):
    def _():
        exprList = exprListGen()
        SetVariables(vList, exprList)
    EUDOnStart(_)

def _CGFW(exprf, retn):
    rets = [ExprProxy(None) for _ in range(retn)]
    def _():
        vals = exprf()
        for ret, val in zip(rets, vals):
            ret._value = val
    EUDOnStart(_)
    return rets

def _ARR(items):
    k = EUDArray(len(items))
    for i, item in enumerate(items):
        k[i] = item
    return k

def _VARR(items):
    k = EUDVArray(len(items))()
    for i, item in enumerate(items):
        k[i] = item
    return k

def _SRET(v, klist):
    return List2Assignable([v[k] for k in klist])

def _SV(dL, sL):
    [d << s for d, s in zip(FlattenList(dL), FlattenList(sL))]

class _ATTW:
    def __init__(self, obj, attrName):
        self.obj = obj
        self.attrName = attrName

    def __lshift__(self, r):
        setattr(self.obj, self.attrName, r)

    def __iadd__(self, v):
        ov = getattr(self.obj, self.attrName)
        setattr(self.obj, self.attrName, ov + v)

    def __isub__(self, v):
        ov = getattr(self.obj, self.attrName)
        setattr(self.obj, self.attrName, ov - v)

    def __imul__(self, v):
        ov = getattr(self.obj, self.attrName)
        setattr(self.obj, self.attrName, ov * v)

    def __ifloordiv__(self, v):
        ov = getattr(self.obj, self.attrName)
        setattr(self.obj, self.attrName, ov // v)

    def __iand__(self, v):
        ov = getattr(self.obj, self.attrName)
        setattr(self.obj, self.attrName, ov & v)

    def __ior__(self, v):
        ov = getattr(self.obj, self.attrName)
        setattr(self.obj, self.attrName, ov | v)

    def __ixor__(self, v):
        ov = getattr(self.obj, self.attrName)
        setattr(self.obj, self.attrName, ov ^ v)

class _ARRW:
    def __init__(self, obj, index):
        self.obj = obj
        self.index = index

    def __lshift__(self, r):
        self.obj[self.index] = r

    def __iadd__(self, v):
        ov = self.obj[self.index]
        self.obj[self.index] = ov + v

    def __isub__(self, v):
        ov = self.obj[self.index]
        self.obj[self.index] = ov - v

    def __imul__(self, v):
        ov = self.obj[self.index]
        self.obj[self.index] = ov * v

    def __ifloordiv__(self, v):
        ov = self.obj[self.index]
        self.obj[self.index] = ov // v

    def __iand__(self, v):
        ov = self.obj[self.index]
        self.obj[self.index] = ov & v

    def __ior__(self, v):
        ov = self.obj[self.index]
        self.obj[self.index] = ov | v

    def __ixor__(self, v):
        ov = self.obj[self.index]
        self.obj[self.index] = ov ^ v

def _L2V(l):
    ret = EUDVariable()
    if EUDIf()(l):
        ret << 1
    if EUDElse()():
        ret << 0
    EUDEndIf()
    return ret

def _MVAR(vs):
    return List2Assignable([
        v.makeL() if IsEUDVariable(v) else EUDVariable() << v
        for v in FlattenList(vs)])

def _LSH(l, r):
    if IsEUDVariable(l):  return f_bitlshift(l, r)
    else: return l << r

## NOTE: THIS FILE IS GENERATED BY EPSCRIPT! DO NOT MODITY

# (Line 1) import BGMPlayer as BGM;
import BGMPlayer as BGM
# (Line 2) import tempcustomText as tct;
import tempcustomText as tct
# (Line 4) var txtPtr, btnPtr, btnPos, oldCP;
txtPtr, btnPtr, btnPos, oldCP = EUDCreateVariables(4)
# (Line 5) const trgk = $T('Artanis & safhfh');
trgk = _CGFW(lambda: [GetStringIndex('Artanis & safhfh')], 1)[0]
# (Line 10) function ClassicTriggerExec() {
@EUDFunc
def ClassicTriggerExec():
    # (Line 11) }
    # (Line 12) function ClassicTriggerStarter() {
    pass

@EUDFunc
def ClassicTriggerStarter():
    # (Line 14) if (playerexist(0)){
    if EUDIf()(f_playerexist(0)):
        # (Line 15) setcurpl(0);
        f_setcurpl(0)
        # (Line 16) }
        # (Line 18) if (playerexist(1)){
    EUDEndIf()
    if EUDIf()(f_playerexist(1)):
        # (Line 19) setcurpl(1);
        f_setcurpl(1)
        # (Line 20) }
        # (Line 22) if (playerexist(2)){
    EUDEndIf()
    if EUDIf()(f_playerexist(2)):
        # (Line 23) setcurpl(2);
        f_setcurpl(2)
        # (Line 24) }
        # (Line 26) if (playerexist(3)){
    EUDEndIf()
    if EUDIf()(f_playerexist(3)):
        # (Line 27) setcurpl(3);
        f_setcurpl(3)
        # (Line 28) }
        # (Line 30) if (playerexist(4)){
    EUDEndIf()
    if EUDIf()(f_playerexist(4)):
        # (Line 31) setcurpl(4);
        f_setcurpl(4)
        # (Line 32) }
        # (Line 34) if (playerexist(5)){
    EUDEndIf()
    if EUDIf()(f_playerexist(5)):
        # (Line 35) setcurpl(5);
        f_setcurpl(5)
        # (Line 36) }
        # (Line 38) if (playerexist(6)){
    EUDEndIf()
    if EUDIf()(f_playerexist(6)):
        # (Line 39) setcurpl(6);
        f_setcurpl(6)
        # (Line 40) }
        # (Line 42) if (playerexist(7)){
    EUDEndIf()
    if EUDIf()(f_playerexist(7)):
        # (Line 43) setcurpl(7);
        f_setcurpl(7)
        # (Line 44) }
        # (Line 45) }
    EUDEndIf()
    # (Line 46) function WaitableTriggerExec() {

@EUDFunc
def WaitableTriggerExec():
    # (Line 47) }
    # (Line 48) function onPluginStart() {
    pass

@EUDFunc
def onPluginStart():
    # (Line 49) randomize();
    f_randomize()
    # (Line 50) LeaderBoardScore((7), " ");
    # (Line 52) }
    DoActions(LeaderBoardScore((7), " "))
    # (Line 53) function beforeTriggerExec() {

@EUDFunc
def beforeTriggerExec():
    # (Line 54) EUDPlayerLoop()();
    EUDPlayerLoop()()
    # (Line 55) WaitableTriggerExec();
    WaitableTriggerExec()
    # (Line 56) ClassicTriggerExec();
    ClassicTriggerExec()
    # (Line 57) EUDEndPlayerLoop();
    EUDEndPlayerLoop()
    # (Line 58) ClassicTriggerStarter();
    ClassicTriggerStarter()
    # (Line 59) BGM.Player();
    BGM.Player()
    # (Line 60) SetMemory(0x6509A0, SetTo, 0);
    # (Line 61) }
    DoActions(SetMemory(0x6509A0, SetTo, 0))
    # (Line 62) function afterTriggerExec() {

@EUDFunc
def afterTriggerExec():
    # (Line 63) setcurpl((0));
    f_setcurpl((0))
    # (Line 64) foreach(ptr, epd : EUDLoopPlayerUnit(0) ) {
    for ptr, epd in EUDLoopPlayerUnit(0):
        # (Line 65) if(
        _t1 = EUDIf()
        # (Line 66) bread(ptr + 0x121) != 0
        # (Line 67) ){
        if _t1(f_bread(ptr + 0x121) == 0, neg=True):
            # (Line 68) SetScore((0), (7), bread(ptr+0x121), (7));
            # (Line 69) var v = bread(ptr + 0x121);
            DoActions(SetScore((0), (7), f_bread(ptr + 0x121), (7)))
            v = EUDVariable()
            v << (f_bread(ptr + 0x121))
            # (Line 70) if (v >= 128) {v-= 128; DisplayText("P8");}
            if EUDIf()(v >= 128):
                v.__isub__(128)
                DoActions(DisplayText("P8"))
                # (Line 71) if (v >= 64) {v-= 64; DisplayText("P7");}
            EUDEndIf()
            if EUDIf()(v >= 64):
                v.__isub__(64)
                DoActions(DisplayText("P7"))
                # (Line 72) if (v >= 32) {v-= 32; DisplayText("P6");}
            EUDEndIf()
            if EUDIf()(v >= 32):
                v.__isub__(32)
                DoActions(DisplayText("P6"))
                # (Line 73) if (v >= 16) {v-= 16; DisplayText("P5");}
            EUDEndIf()
            if EUDIf()(v >= 16):
                v.__isub__(16)
                DoActions(DisplayText("P5"))
                # (Line 74) if (v >= 8) {v-= 8; DisplayText("P4");}
            EUDEndIf()
            if EUDIf()(v >= 8):
                v.__isub__(8)
                DoActions(DisplayText("P4"))
                # (Line 75) if (v >= 4) {v-= 4; DisplayText("P3");}
            EUDEndIf()
            if EUDIf()(v >= 4):
                v.__isub__(4)
                DoActions(DisplayText("P3"))
                # (Line 76) if (v >= 2) {v-= 2; DisplayText("P2");}
            EUDEndIf()
            if EUDIf()(v >= 2):
                v.__isub__(2)
                DoActions(DisplayText("P2"))
                # (Line 77) if (v >= 1) {v-= 1; DisplayText("P1");};
            EUDEndIf()
            if EUDIf()(v >= 1):
                v.__isub__(1)
                DoActions(DisplayText("P1"))
            EUDEndIf()
            # (Line 78) bwrite_epd(epd + 0x121 / 4,  0x121 % 4, 0);
            f_bwrite_epd(epd + 0x121 // 4, 0x121 % 4, 0)
            # (Line 79) }
            # (Line 80) }
        EUDEndIf()
        # (Line 81) }
