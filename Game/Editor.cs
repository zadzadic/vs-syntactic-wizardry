using SyntacticWizardry.Core;
using Vintagestory.API.Client;
namespace SyntacticWizardry.Game;
internal sealed class Editor:GuiDialog {
 private Spell spell;private State state;private readonly Action<Request> send;private int selected;private string name;private bool rebuilding;
 public override string ToggleKeyCombinationCode=>"sw-editor";
 public Editor(ICoreClientAPI api,State s,Action<Request> send):base(api){state=s;this.send=send;try{spell=WizardryMod.Engine.Decode(s.SpellJson);}catch{spell=Engine.Starter();}name=spell.Name;Build();}
 private static ElementBounds B(double x,double y,double w,double h)=>ElementBounds.Fixed(x,y,w,h);
 private Cell? Current=>spell.Cells.FirstOrDefault(c=>c.Row*5+c.Column==selected);
 private void Change(string id){spell.Cells.RemoveAll(c=>c.Row*5+c.Column==selected);if(id!="")spell.Cells.Add(new Cell{Row=selected/5,Column=selected%5,Id=id});Build();}
 private bool Select(int i){selected=i;Build();return true;}
 private void Build(){if(rebuilding)return;rebuilding=true;SingleComposer?.Dispose();var font=CairoFont.WhiteSmallText();var bounds=ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);var c=capi.Gui.CreateCompo("sw-editor",bounds).AddShadedDialogBG(B(0,0,590,435)).AddDialogTitleBar("Syntactic Wizardry — Spell Creation",()=>TryClose());
  c.AddStaticText("Spell name",font,B(15,40,100,25)).AddTextInput(B(115,38,310,28),v=>name=v,null,"name");
  for(int i=0;i<25;i++){int n=i;var cell=spell.Cells.FirstOrDefault(x=>x.Row*5+x.Column==n);string label=(n==selected?"> ":"")+(cell?.Id??"Empty");c.AddSmallButton(label,()=>Select(n),B(15+(i%5)*81,78+(i/5)*42,77,37));}
  c.AddStaticText($"Cell {selected/5+1}, {selected%5+1}",font,B(432,78,145,25));c.AddSmallButton("Target",()=>{Change("target");return true;},B(432,106,145,28));c.AddSmallButton("Damage",()=>{Change("damage");return true;},B(432,140,145,28));c.AddSmallButton("Clear",()=>{Change("");return true;},B(432,174,145,28));
  var current=Current;if(current!=null&&WizardryMod.Registry.TryGet(current.Id,out var def)){int y=210;foreach(var prop in def.Properties){var p=prop;var val=def.Value(current,p.Id);var label=p.Options!=null?p.Options[val-p.Minimum]:val.ToString();c.AddStaticText(p.Id+": "+label,font,B(432,y,150,24));c.AddSmallButton("-",()=>{current.Settings[p.Id]=Math.Max(p.Minimum,def.Value(current,p.Id)-1);Build();return true;},B(432,y+24,67,25));c.AddSmallButton("+",()=>{current.Settings[p.Id]=Math.Min(p.Maximum,def.Value(current,p.Id)+1);Build();return true;},B(507,y+24,67,25));y+=55;}}
  string error=WizardryMod.Engine.Validate(spell)??$"Wand cost: {WizardryMod.Engine.Cost(spell,.75f):0.##} Mana";c.AddStaticText(error,font,B(15,302,410,35)).AddStaticText($"Mana: {state.Mana:0.##}/{state.MaxMana:0.##}",font,B(15,340,410,25));c.AddSmallButton("Save to wand",Save,B(15,375,160,34));c.AddSmallButton("Reset",()=>{spell=Engine.Starter();name=spell.Name;Build();return true;},B(185,375,100,34));c.AddSmallButton("Close",()=>{TryClose();return true;},B(295,375,100,34));SingleComposer=c.Compose();SingleComposer.GetTextInput("name").SetValue(name);rebuilding=false;
 }
 private bool Save(){spell.Name=name.Trim();if(WizardryMod.Engine.Validate(spell)!=null){Build();return true;}send(new Request{Action="save",SpellJson=Engine.Encode(spell),Revision=state.Revision});return true;}
 public void Update(State s){state=s;if(s.Message=="Written spell saved to wand."){TryClose();return;}Build();}
}
