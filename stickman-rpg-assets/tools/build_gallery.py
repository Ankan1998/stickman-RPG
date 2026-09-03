"""Build the browsable asset gallery as one self-contained HTML file."""
import base64, json, os, sys

ROOT = sys.argv[1] if len(sys.argv) > 1 else "/root/assetgen/out/stickman-rpg-assets"
OUT = sys.argv[2] if len(sys.argv) > 2 else "/root/assetgen/out/gallery.html"
man = json.load(open(os.path.join(ROOT, "manifest.json")))


def b64(p):
    with open(p, "rb") as f:
        return "data:image/png;base64," + base64.b64encode(f.read()).decode()


atlases = {}
for k in ("heroes_all", "enemies_all", "weapons", "dungeon", "fx"):
    a = man["atlases"][k]
    atlases[k] = dict(src=b64(os.path.join(ROOT, "atlases", a["path"])),
                      cols=a["cols"], rows=a["rows"],
                      cw=a["cell_w"], ch=a["cell_h"])

# individual dungeon tiles so seamless ones can be shown genuinely repeating
tiles = {d["name"]: b64(os.path.join(ROOT, d["file"])) for d in man["dungeon"]}

ANIMS = [("idle", 0, 4, 6), ("walk", 4, 6, 10), ("attack", 10, 6, 12),
         ("hurt", 16, 3, 14), ("death", 19, 6, 8)]

data = dict(
    totals=man["totals"], conventions=man["conventions"],
    anims=[dict(name=n, start=s, count=c, fps=f) for n, s, c, f in ANIMS],
    heroes=[dict(i=i, name=h["name"], label=h["label"], role=h["role"],
                 weapon=h["weapon"] or "-", offhand=h["offhand"] or "-",
                 blurb=h["blurb"], dir=h["dir"])
            for i, h in enumerate(man["heroes"])],
    enemies=[dict(i=i, name=e["name"], label=e["label"], tier=e["tier"],
                  tier_name=e["tier_name"], weapon=e["weapon"] or "-",
                  blurb=e["blurb"], dir=e["dir"])
             for i, e in enumerate(man["enemies"])],
    weapons=[dict(i=i, name=w["name"], label=w["label"], kind=w["archetype"],
                  rarity=w["rarity"], slot=w["slot"], blurb=w["blurb"],
                  file=w["file"])
             for i, w in enumerate(man["weapons"])],
    dungeon=[dict(i=i, name=d["name"], cat=d["category"], seamless=d["seamless"],
                  blurb=d["blurb"], file=d["file"])
             for i, d in enumerate(man["dungeon"])],
    fx=[dict(i=i, name=f["name"], blurb=f["blurb"], frames=f["frames"],
             fps=f["fps"], dir=f["dir"]) for i, f in enumerate(man["fx"])],
    atlases=atlases, tiles=tiles,
)

HTML = r"""<title>Stickman RPG Asset Pack</title>
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Silkscreen:wght@400;700&family=IBM+Plex+Sans:wght@400;500;600;700&family=IBM+Plex+Mono:wght@400;500&display=swap">
<style>
:root{
  --ink:#14131c; --ground:#17161f; --panel:#201e2b; --panel2:#282534;
  --line:#332f44; --line2:#453f5c;
  --text:#e8e4f0; --muted:#9c96b0; --dim:#6f6a83;
  --torch:#e08a3c; --torch-dim:#a8642a; --gold:#e0c46c; --stone:#6b6f7a;
  --cell:#26242f;
  --r-common:#8a8f9c; --r-uncommon:#7dae4c; --r-rare:#4d8fd6;
  --r-epic:#a86fd6; --r-legendary:#e0a13c;
  --t1:#7f8794; --t2:#4d8fd6; --t3:#d9743c;
  --sans:"IBM Plex Sans",ui-sans-serif,system-ui,sans-serif;
  --mono:"IBM Plex Mono",ui-monospace,"SF Mono",Menlo,monospace;
  --pix:"Silkscreen",var(--mono);
  --maxw:1320px;
}
@media (prefers-color-scheme:light){:root:not([data-theme="dark"]){
  --ground:#e6e4ea; --panel:#f4f3f7; --panel2:#e9e7ee;
  --line:#d3cfdc; --line2:#bdb7cb;
  --text:#221e2c; --muted:#5f5972; --dim:#837d95;
  --torch:#b35f14; --torch-dim:#8a4a10; --gold:#9a7c1e;
}}
:root[data-theme="light"]{
  --ground:#e6e4ea; --panel:#f4f3f7; --panel2:#e9e7ee;
  --line:#d3cfdc; --line2:#bdb7cb;
  --text:#221e2c; --muted:#5f5972; --dim:#837d95;
  --torch:#b35f14; --torch-dim:#8a4a10; --gold:#9a7c1e;
}
*{box-sizing:border-box}
body{margin:0;background:var(--ground);color:var(--text);
  font-family:var(--sans);font-size:15px;line-height:1.5;
  -webkit-font-smoothing:antialiased}
.wrap{max-width:var(--maxw);margin:0 auto;padding:0 22px}
a{color:var(--torch)}
h1,h2,h3{text-wrap:balance;margin:0}

/* ---------- header ---------- */
header{border-bottom:1px solid var(--line);
  background:linear-gradient(180deg,var(--panel),var(--ground))}
.mast{display:flex;flex-wrap:wrap;gap:22px;align-items:flex-end;
  justify-content:space-between;padding:30px 0 22px}
.eyebrow{font-family:var(--pix);font-size:10px;letter-spacing:.16em;
  text-transform:uppercase;color:var(--torch);margin-bottom:10px}
h1{font-family:var(--pix);font-size:clamp(21px,3.4vw,34px);font-weight:700;
  line-height:1.15;letter-spacing:.01em}
.sub{color:var(--muted);max-width:60ch;margin-top:10px;font-size:14px}
.totals{display:flex;gap:0;flex-wrap:wrap;border:1px solid var(--line);
  border-radius:3px;overflow:hidden;background:var(--panel2)}
.tot{padding:9px 15px;border-right:1px solid var(--line);min-width:78px}
.tot:last-child{border-right:0}
.tot b{display:block;font-family:var(--mono);font-size:19px;font-weight:500;
  font-variant-numeric:tabular-nums;color:var(--torch);line-height:1.2}
.tot span{font-family:var(--pix);font-size:9px;letter-spacing:.1em;
  text-transform:uppercase;color:var(--muted)}

/* ---------- toolbar ---------- */
.bar{position:sticky;top:0;z-index:40;background:var(--ground);
  border-bottom:1px solid var(--line);padding:9px 0}
.barin{display:flex;gap:14px;align-items:center;flex-wrap:wrap}
.tabs{display:flex;gap:2px;flex-wrap:wrap}
.tab{font-family:var(--pix);font-size:10px;letter-spacing:.08em;
  text-transform:uppercase;padding:8px 12px;border:1px solid transparent;
  background:none;color:var(--muted);cursor:pointer;border-radius:3px}
.tab:hover{color:var(--text);background:var(--panel2)}
.tab[aria-selected="true"]{color:var(--ink);background:var(--torch);
  border-color:var(--torch)}
:root[data-theme="light"] .tab[aria-selected="true"],
:root:not([data-theme="dark"]) .tab[aria-selected="true"]{color:#fff}
.spacer{flex:1 1 auto}
.ctl{display:flex;align-items:center;gap:7px}
.ctl label{font-family:var(--pix);font-size:9px;letter-spacing:.1em;
  text-transform:uppercase;color:var(--dim)}
select,input[type=search]{font-family:var(--sans);font-size:13px;
  background:var(--panel2);color:var(--text);border:1px solid var(--line2);
  border-radius:3px;padding:6px 8px}
input[type=search]{width:150px}
input[type=range]{width:92px;accent-color:var(--torch)}
button:focus-visible,select:focus-visible,input:focus-visible,
.card:focus-visible{outline:2px solid var(--torch);outline-offset:2px}

/* ---------- sections ---------- */
section{padding:34px 0 8px}
.shead{display:flex;align-items:baseline;gap:12px;margin-bottom:4px}
.shead h2{font-family:var(--pix);font-size:14px;letter-spacing:.06em;
  text-transform:uppercase}
.shead .n{font-family:var(--mono);font-size:12px;color:var(--dim);
  font-variant-numeric:tabular-nums}
.snote{color:var(--muted);font-size:13.5px;max-width:70ch;margin-bottom:18px}
.grouphead{font-family:var(--pix);font-size:10px;letter-spacing:.12em;
  text-transform:uppercase;color:var(--muted);margin:22px 0 10px;
  display:flex;align-items:center;gap:9px}
.grouphead::after{content:"";flex:1;height:1px;background:var(--line)}
.grouphead .swatch{width:8px;height:8px;border-radius:1px}

/* ---------- sprite grid ---------- */
.grid{display:grid;gap:10px;
  grid-template-columns:repeat(auto-fill,minmax(var(--col,116px),1fr))}
.card{background:var(--panel);border:1px solid var(--line);border-radius:3px;
  padding:0;overflow:hidden;cursor:pointer;text-align:left;font:inherit;
  color:inherit;display:flex;flex-direction:column;transition:border-color .12s}
.card:hover{border-color:var(--line2)}
.card.sel{border-color:var(--torch)}
.stage{display:flex;align-items:flex-end;justify-content:center;
  padding:10px 6px 8px;background:var(--cell);min-height:76px}
.stage.bd-checker{background-image:
  linear-gradient(45deg,#2e2c38 25%,transparent 25%),
  linear-gradient(-45deg,#2e2c38 25%,transparent 25%),
  linear-gradient(45deg,transparent 75%,#2e2c38 75%),
  linear-gradient(-45deg,transparent 75%,#2e2c38 75%);
  background-size:12px 12px;
  background-position:0 0,0 6px,6px -6px,-6px 0;background-color:#232129}
.stage.bd-stone{background:#5c6069}
.stage.bd-light{background:#d9d6cf}
.stage.bd-ink{background:#14131c}
.spr{image-rendering:pixelated;background-repeat:no-repeat;flex:none}
.meta{padding:7px 9px 9px;border-top:1px solid var(--line);
  background:var(--panel);display:flex;flex-direction:column;gap:3px}
.nm{font-size:12.5px;font-weight:600;line-height:1.25}
.id{font-family:var(--mono);font-size:10.5px;color:var(--dim);
  overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
.chip{font-family:var(--pix);font-size:8px;letter-spacing:.08em;
  text-transform:uppercase;padding:2px 5px;border-radius:2px;
  border:1px solid currentColor;align-self:flex-start;margin-top:2px;
  max-width:100%;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
.tilewrap{width:100%;height:64px;image-rendering:pixelated;
  background-repeat:no-repeat;background-position:center}
.tilewrap.tiled{background-repeat:repeat;background-position:center}

/* ---------- drawer ---------- */
.scrim{position:fixed;inset:0;background:#0009;z-index:60;opacity:0;
  pointer-events:none;transition:opacity .15s}
.scrim.on{opacity:1;pointer-events:auto}
.drawer{position:fixed;top:0;right:0;bottom:0;width:min(430px,100%);
  background:var(--panel);border-left:1px solid var(--line2);z-index:61;
  transform:translateX(100%);transition:transform .18s ease;
  overflow-y:auto;padding:20px}
.drawer.on{transform:none}
.drawer h3{font-family:var(--pix);font-size:15px;letter-spacing:.03em;
  margin-bottom:3px}
.drawer .role{color:var(--torch);font-size:12.5px;margin-bottom:8px}
.drawer p.b{color:var(--muted);font-size:13.5px;margin:0 0 16px}
.close{position:absolute;top:14px;right:16px;background:var(--panel2);
  border:1px solid var(--line2);color:var(--text);border-radius:3px;
  width:28px;height:28px;cursor:pointer;font-size:15px;line-height:1}
.animrow{display:flex;gap:12px;align-items:flex-end;padding:12px 0;
  border-bottom:1px solid var(--line)}
.animrow .lab{flex:1;min-width:0}
.animrow .lab b{font-family:var(--pix);font-size:10px;letter-spacing:.09em;
  text-transform:uppercase;display:block}
.animrow .lab span{font-family:var(--mono);font-size:10.5px;color:var(--dim);
  font-variant-numeric:tabular-nums}
.animstage{background:var(--cell);border:1px solid var(--line);border-radius:3px;
  padding:6px;display:flex;align-items:flex-end;justify-content:center}
.paths{margin-top:16px;font-family:var(--mono);font-size:11px;
  color:var(--muted);background:var(--panel2);border:1px solid var(--line);
  border-radius:3px;padding:10px;overflow-x:auto;white-space:pre;line-height:1.7}

/* ---------- notes ---------- */
.notes{border-top:1px solid var(--line);margin-top:40px;padding:26px 0 50px;
  display:grid;gap:18px;grid-template-columns:repeat(auto-fit,minmax(250px,1fr))}
.note h4{font-family:var(--pix);font-size:10px;letter-spacing:.1em;
  text-transform:uppercase;color:var(--torch);margin:0 0 7px}
.note p{margin:0;color:var(--muted);font-size:13.5px}
.note code{font-family:var(--mono);font-size:11.5px;background:var(--panel2);
  border:1px solid var(--line);border-radius:2px;padding:1px 4px;color:var(--text);
  overflow-wrap:anywhere;word-break:break-word}
.empty{color:var(--dim);font-size:13.5px;padding:20px 0}
@media (prefers-reduced-motion:reduce){*{transition:none!important}}
@media (max-width:620px){
  .mast{padding:22px 0 16px}
  .drawer{width:100%}
  input[type=search]{width:110px}
}
</style>

<header>
 <div class="wrap mast">
  <div>
    <div class="eyebrow">Generated pixel art &middot; v1.0.0</div>
    <h1>Stickman RPG Asset Pack</h1>
    <p class="sub">Every sprite here is drawn by Python from a shared palette and a
      parametric skeleton &mdash; so a colour change updates the whole set, and each
      character's five animations come from interpolated joint angles rather than
      hand-drawn frames.</p>
  </div>
  <div class="totals" id="totals"></div>
 </div>
</header>

<div class="bar"><div class="wrap barin">
  <div class="tabs" role="tablist" id="tabs"></div>
  <div class="spacer"></div>
  <div class="ctl"><label for="q">Find</label>
    <input type="search" id="q" placeholder="sword, undead&hellip;"></div>
  <div class="ctl"><label for="anim">Play</label>
    <select id="anim"></select></div>
  <div class="ctl"><label for="bd">Backdrop</label>
    <select id="bd">
      <option value="bd-cell">Neutral</option>
      <option value="bd-checker">Checker</option>
      <option value="bd-ink">Ink</option>
      <option value="bd-stone">Stone</option>
      <option value="bd-light">Light</option>
    </select></div>
  <div class="ctl"><label for="zoom">Zoom</label>
    <input type="range" id="zoom" min="1" max="6" step="1" value="3">
    <span class="id" id="zv">3&times;</span></div>
</div></div>

<main class="wrap" id="main"></main>

<div class="wrap notes">
  <div class="note"><h4>Nearest-neighbour, always</h4>
    <p>In Godot set <code>textures/canvas_textures/default_texture_filter=0</code>.
    Without it the engine interpolates on scale-up and every sprite becomes a blurry
    smear &mdash; this one line is the whole fix.</p></div>
  <div class="note"><h4>Integer scales only</h4>
    <p>Characters are 32&times;40 for 3&times;. Tiles are 16&times;16 for 3&times;
    or 4&times;. Fractional scaling makes some pixels bigger than others, which looks
    wrong even when you can't say why.</p></div>
  <div class="note"><h4>One crop per character</h4>
    <p>All 25 frames of a character share a single auto-fitted crop window, measured
    across every animation, so a sprite never jitters when it switches state.</p></div>
  <div class="note"><h4>Icons match the hand</h4>
    <p>An inventory icon and the weapon in a hero's fist are the same drawing routine
    at a different angle, so the two can never drift apart.</p></div>
</div>

<div class="scrim" id="scrim"></div>
<aside class="drawer" id="drawer" aria-hidden="true">
  <button class="close" id="close" aria-label="Close">&times;</button>
  <div id="dbody"></div>
</aside>

<script>
const D = __DATA__;
const $ = s => document.querySelector(s);
const el = (t,c,h) => { const e=document.createElement(t); if(c)e.className=c;
  if(h!==undefined)e.innerHTML=h; return e; };

/* ---- totals ---- */
const T=D.totals;
[["heroes",T.heroes],["enemies",T.enemies],["weapons",T.weapons],
 ["tiles",T.dungeon],["effects",T.fx],["frames",T.character_frames+T.fx_frames]]
 .forEach(([k,v])=>{ const d=el("div","tot");
   d.append(el("b",null,String(v)), el("span",null,k)); $("#totals").append(d); });

/* ---- state ---- */
const S={tab:"heroes",anim:"idle",bd:"bd-cell",zoom:3,q:""};
const A=D.anims, AM={}; A.forEach(a=>AM[a.name]=a);
$("#anim").innerHTML=A.map(a=>`<option value="${a.name}">${a.name}</option>`).join("");
const TABS=[["heroes","Heroes"],["enemies","Enemies"],["weapons","Weapons"],
            ["dungeon","Dungeon"],["fx","Effects"]];
TABS.forEach(([k,l])=>{ const b=el("button","tab",l); b.role="tab";
  b.dataset.k=k; b.setAttribute("aria-selected",k===S.tab);
  b.onclick=()=>{S.tab=k;syncTabs();render();}; $("#tabs").append(b); });
const syncTabs=()=>document.querySelectorAll(".tab").forEach(
  b=>b.setAttribute("aria-selected",b.dataset.k===S.tab));

/* ---- sprite element backed by an atlas cell ---- */
function sprite(atlasKey,row,col,zoom,anim){
  const a=D.atlases[atlasKey], s=el("div","spr");
  s.style.width=(a.cw*zoom)+"px"; s.style.height=(a.ch*zoom)+"px";
  s.style.backgroundImage=`url(${a.src})`;
  s.style.backgroundSize=`${a.cols*a.cw*zoom}px ${a.rows*a.ch*zoom}px`;
  s.style.backgroundPosition=`${-col*a.cw*zoom}px ${-row*a.ch*zoom}px`;
  if(anim){ s.dataset.atlas=atlasKey; s.dataset.row=row; s.dataset.start=anim.start;
    s.dataset.count=anim.count; s.dataset.fps=anim.fps; s.dataset.zoom=zoom;
    anims.push(s); }
  return s;
}
let anims=[];

/* one rAF loop drives every animated sprite at its own fps */
function tick(t){
  for(const s of anims){
    const fps=+s.dataset.fps, n=+s.dataset.count, z=+s.dataset.zoom;
    const a=D.atlases[s.dataset.atlas];
    const f=Math.floor(t/1000*fps)%n;
    const col=(+s.dataset.start)+f;
    const x=-col*a.cw*z;
    if(s._x!==x){ s._x=x;
      s.style.backgroundPosition=`${x}px ${-(+s.dataset.row)*a.ch*z}px`; }
  }
  requestAnimationFrame(tick);
}
requestAnimationFrame(tick);

const match=(o,q)=>!q||JSON.stringify(o).toLowerCase().includes(q);

function card(inner,meta,onclick){
  const c=el("button","card"); c.type="button";
  const st=el("div","stage "+S.bd); st.append(inner);
  c.append(st,meta); if(onclick)c.onclick=onclick; return c;
}
function metaBlock(name,id,chip,chipColor){
  const m=el("div","meta");
  m.append(el("div","nm",name), el("div","id",id));
  if(chip){ const c=el("span","chip",chip); c.style.color=chipColor; m.append(c); }
  return m;
}

/* ---- renderers ---- */
function charGrid(list,atlasKey,groupBy){
  const frag=document.createDocumentFragment();
  const groups = groupBy ? groupBy(list) : [[null,list]];
  for(const [gname,items,gcolor] of groups){
    if(gname){ const h=el("div","grouphead");
      if(gcolor){const sw=el("span","swatch");sw.style.background=gcolor;h.append(sw);}
      h.append(document.createTextNode(gname)); frag.append(h); }
    const g=el("div","grid");
    g.style.setProperty("--col",(Math.max(116, 44+32*S.zoom))+"px");
    for(const o of items){
      const a=AM[S.anim];
      const sp=sprite(atlasKey,o.i,a.start,S.zoom,a);
      const chipC=o.tier?`var(--t${o.tier})`:"var(--torch)";
      const chip=o.tier?`tier ${o.tier} ${o.tier_name}`:o.role;
      g.append(card(sp,metaBlock(o.label,o.name,chip,chipC),()=>openChar(o,atlasKey)));
    }
    frag.append(g);
  }
  return frag;
}

function openChar(o,atlasKey){
  const b=$("#dbody"); b.innerHTML="";
  anims=anims.filter(s=>!s.closest(".drawer"));
  b.append(el("h3",null,o.label));
  b.append(el("div","role",o.tier?`Tier ${o.tier} &middot; ${o.tier_name} &middot; wields ${o.weapon}`
    :`${o.role} &middot; wields ${o.weapon}`));
  b.append(el("p","b",o.blurb));
  for(const a of D.anims){
    const r=el("div","animrow");
    const st=el("div","animstage"); st.append(sprite(atlasKey,o.i,a.start,3,a));
    const lab=el("div","lab");
    lab.append(el("b",null,a.name),
      el("span",null,`${a.count} frames &middot; ${a.fps} fps${a.name==="death"?" &middot; no loop":""}`));
    r.append(st,lab); b.append(r);
  }
  const p=el("div","paths",
    `${o.dir}/\n  ${o.name}.png            still\n  ${o.name}_down.png       defeated\n`+
    D.anims.map(a=>`  ${o.name}_${a.name}_strip.png`.padEnd(34)+`${a.count}f strip`).join("\n")+
    `\n  frames/${o.name}_<anim>_NN.png`);
  b.append(p);
  $("#drawer").classList.add("on"); $("#drawer").setAttribute("aria-hidden","false");
  $("#scrim").classList.add("on");
}
function closeDrawer(){
  $("#drawer").classList.remove("on"); $("#drawer").setAttribute("aria-hidden","true");
  $("#scrim").classList.remove("on");
  anims=anims.filter(s=>!s.closest(".drawer"));
}
$("#close").onclick=closeDrawer; $("#scrim").onclick=closeDrawer;
document.addEventListener("keydown",e=>{if(e.key==="Escape")closeDrawer();});

function weaponGrid(list){
  const order=["legendary","epic","rare","uncommon","common"];
  const frag=document.createDocumentFragment();
  for(const r of order){
    const items=list.filter(w=>w.rarity===r); if(!items.length)continue;
    const h=el("div","grouphead"); const sw=el("span","swatch");
    sw.style.background=`var(--r-${r})`; h.append(sw,document.createTextNode(`${r} (${items.length})`));
    frag.append(h);
    const g=el("div","grid"); g.style.setProperty("--col",(Math.max(112,40+24*S.zoom))+"px");
    for(const w of items){
      const sp=sprite("weapons",Math.floor(w.i/D.atlases.weapons.cols),
                      w.i%D.atlases.weapons.cols,Math.max(2,S.zoom));
      g.append(card(sp,metaBlock(w.label,w.kind+" &middot; "+w.slot,w.rarity,`var(--r-${w.rarity})`),
        ()=>openSimple(w.label,w.rarity+" &middot; "+w.kind+" &middot; "+w.slot,w.blurb,w.file)));
    }
    frag.append(g);
  }
  return frag;
}

function dungeonGrid(list){
  const cats=["floor","wall","structure","prop","pickup"];
  const frag=document.createDocumentFragment();
  for(const c of cats){
    const items=list.filter(d=>d.cat===c); if(!items.length)continue;
    const h=el("div","grouphead");
    h.append(document.createTextNode(`${c} (${items.length})`)); frag.append(h);
    const g=el("div","grid"); g.style.setProperty("--col","132px");
    for(const d of items){
      const box=el("div","tilewrap"+(d.seamless?" tiled":""));
      const z=Math.max(2,S.zoom);
      box.style.backgroundImage=`url(${D.tiles[d.name]})`;
      box.style.backgroundSize=`${16*z}px ${16*z}px`;
      const cd=el("button","card"); cd.type="button";
      const st=el("div","stage "+S.bd); st.style.padding="0"; st.append(box);
      cd.append(st,metaBlock(d.name.replace(/_/g," "),d.file,
        d.seamless?"tiles seamlessly":d.cat, d.seamless?"var(--gold)":"var(--dim)"));
      cd.onclick=()=>openSimple(d.name.replace(/_/g," "),
        d.cat+(d.seamless?" &middot; tiles seamlessly":""),d.blurb,d.file);
      g.append(cd);
    }
    frag.append(g);
  }
  return frag;
}

function fxGrid(list){
  const g=el("div","grid"); g.style.setProperty("--col",(Math.max(126,44+32*S.zoom))+"px");
  for(const f of list){
    const sp=sprite("fx",f.i,0,Math.max(2,S.zoom),{start:0,count:f.frames,fps:f.fps});
    const cd=card(sp,metaBlock(f.name.replace("fx_",""),`${f.frames}f &middot; ${f.fps}fps`,
      "32&times;32","var(--torch)"),
      ()=>openSimple(f.name,`${f.frames} frames &middot; ${f.fps} fps`,f.blurb,f.dir+"/"));
    g.append(cd);
  }
  return g;
}

function openSimple(title,sub,blurb,path){
  const b=$("#dbody"); b.innerHTML="";
  b.append(el("h3",null,title),el("div","role",sub),el("p","b",blurb),
           el("div","paths",path));
  $("#drawer").classList.add("on"); $("#scrim").classList.add("on");
  $("#drawer").setAttribute("aria-hidden","false");
}

const NOTES={
 heroes:"Ten playable classes. Each has idle, walk, attack, hurt and death cycles \u2014 25 frames \u2014 sharing one crop so switching state never shifts the sprite.",
 enemies:"Twenty-nine enemies across three tiers. Death animations topple the body onto the ground and drain its colour, ending on the pose saved as &lt;name&gt;_down.png.",
 weapons:"Forty-seven icons across twenty-three archetypes. Rarity is metadata plus a faint backing glow \u2014 the tier colours here are the same hexes baked into the sprites.",
 dungeon:"Forty-three 16\u00d716 tiles. Floors and walls marked \u201ctiles seamlessly\u201d are shown here genuinely repeating, so you can check the seams before you stamp a grid with them.",
 fx:"Sixteen impact and spell effects at 32\u00d732, six frames each. Drawn opaque at the core \u2014 translucent fire over a dark background just reads as grey smudge."
};

function render(){
  anims=anims.filter(s=>s.closest(".drawer"));
  const m=$("#main"); m.innerHTML="";
  const q=S.q.trim().toLowerCase();
  const sec=el("section");
  const list=D[S.tab].filter(o=>match(o,q));
  const h=el("div","shead");
  h.append(el("h2",null,TABS.find(t=>t[0]===S.tab)[1]),
           el("span","n",`${list.length} of ${D[S.tab].length}`));
  sec.append(h,el("p","snote",NOTES[S.tab]));
  if(!list.length){ sec.append(el("p","empty",`Nothing matches \u201c${S.q}\u201d.`)); }
  else if(S.tab==="heroes") sec.append(charGrid(list,"heroes_all"));
  else if(S.tab==="enemies") sec.append(charGrid(list,"enemies_all",
    l=>[1,2,3].map(t=>[`tier ${t} \u2014 ${(l.find(e=>e.tier===t)||{}).tier_name||""}`,
      l.filter(e=>e.tier===t),`var(--t${t})`]).filter(g=>g[1].length)));
  else if(S.tab==="weapons") sec.append(weaponGrid(list));
  else if(S.tab==="dungeon") sec.append(dungeonGrid(list));
  else sec.append(fxGrid(list));
  m.append(sec);
}

$("#q").oninput=e=>{S.q=e.target.value;render();};
$("#anim").onchange=e=>{S.anim=e.target.value;render();};
$("#bd").onchange=e=>{S.bd=e.target.value;render();};
$("#zoom").oninput=e=>{S.zoom=+e.target.value;$("#zv").textContent=S.zoom+"\u00d7";render();};
render();
</script>
"""

html = HTML.replace("__DATA__", json.dumps(data, separators=(",", ":")))
open(OUT, "w", encoding="utf-8").write(html)
print(OUT, round(len(html) / 1024), "KB")
