using System.Numerics;
using System.Text.Json;
namespace SyntacticWizardry.Core;
public enum ComponentKind { Shape, Effect, Modifier }
public enum TargetMode { Blocks, Entities, All }
public enum VectorPolicy { Forward, Radial, SurfaceNormal }
public record Property(string Id, int Minimum, int Maximum, int Default, string[]? Options = null);
public sealed class Cell { public int Row {get;set;} public int Column {get;set;} public string Id {get;set;}=""; public Dictionary<string,int> Settings {get;set;}=new(); }
public sealed class Spell { public int Schema {get;set;}=1; public string Name {get;set;}="Untitled"; public List<Cell> Cells {get;set;}=new(); }
public record Voxel(int X,int Y,int Z);
public record ShapeResolution(Vector3 Origin,Vector3 Forward,Vector3 Up,Vector3? SurfaceNormal,IReadOnlySet<Voxel> Voxels,long? DirectEntity,VectorPolicy Policy=VectorPolicy.Forward);
public interface ISpellWorld {
 ShapeResolution? Trace(ShapeResolution parent, TargetMode mode, bool inward, double range, Func<long,bool> accepts);
 bool IsLiving(long id);
 IEnumerable<long> LivingIn(IReadOnlySet<Voxel> voxels);
 bool Damage(long id,int amount);
}
public record ExecutionContext(ISpellWorld World,Spell Spell,Cell Cell,ShapeResolution Parent,Func<long,bool> AcceptsContinuation);
public abstract class Definition {
 public abstract string Id {get;} public abstract ComponentKind Kind {get;}
 public abstract IReadOnlyList<Property> Properties {get;}
 public int Value(Cell cell,string key) { var p=Properties.First(x=>x.Id==key); return cell.Settings.GetValueOrDefault(key,p.Default); }
 public abstract float Cost(Cell cell);
 public virtual bool Accepts(ISpellWorld world,long id)=>false;
 public virtual ShapeResolution? Resolve(ExecutionContext context)=>throw new NotSupportedException();
 public virtual int Apply(ExecutionContext context)=>throw new NotSupportedException();
}
public sealed class TargetDefinition : Definition {
 public override string Id=>"target"; public override ComponentKind Kind=>ComponentKind.Shape;
 public override IReadOnlyList<Property> Properties {get;}=new Property[]{new("target_type",0,2,0,new[]{"Blocks","Entities","All"}),new("impact_direction",0,1,1,new[]{"Outward","Inward"})};
 public override float Cost(Cell cell)=>3;
 public override ShapeResolution? Resolve(ExecutionContext c)=>c.World.Trace(c.Parent,(TargetMode)Value(c.Cell,"target_type"),Value(c.Cell,"impact_direction")==1,64,c.AcceptsContinuation);
}
public sealed class DamageDefinition : Definition {
 public override string Id=>"damage"; public override ComponentKind Kind=>ComponentKind.Effect;
 public override IReadOnlyList<Property> Properties {get;}=new Property[]{new("potence",1,10,1),new("damage_kind",3,3,3,new[]{"Physical"})};
 public override float Cost(Cell cell)=>3*Value(cell,"potence");
 public override bool Accepts(ISpellWorld w,long id)=>w.IsLiving(id);
 public override int Apply(ExecutionContext c) {
  var ids=c.Parent.DirectEntity is long id?new[]{id}:c.World.LivingIn(c.Parent.Voxels);
  return ids.Distinct().Count(x=>Accepts(c.World,x)&&c.World.Damage(x,Value(c.Cell,"potence")));
 }
}
public sealed class Registry {
 private readonly Dictionary<string,Definition> definitions=new(StringComparer.Ordinal);
 public Registry(){Register(new TargetDefinition());Register(new DamageDefinition());}
 public void Register(Definition d)=>definitions.Add(d.Id,d);
 public bool TryGet(string id,out Definition d)=>definitions.TryGetValue(id,out d!);
 public Definition this[string id]=>definitions[id];
 public IEnumerable<Definition> All=>definitions.Values;
}
public sealed class Engine(Registry registry) {
 public const int Rows=5,Columns=5, MaxJson=8192;
 public static Spell Starter()=>new(){Name="Target Damage",Cells=new(){new(){Row=0,Column=0,Id="target",Settings=new(){{"target_type",1}}},new(){Row=1,Column=0,Id="damage"}}};
 public string? Validate(Spell? spell){
  if(spell is null||spell.Schema!=1||spell.Name is null||spell.Name.Length is <1 or >48||spell.Name.Any(char.IsControl)||spell.Cells is null||spell.Cells.Count is <2 or >25)return "Invalid spell format.";
  var positions=new HashSet<(int,int)>();
  foreach(var c in spell.Cells){if(c is null||c.Id is null||c.Row<0||c.Row>=Rows||c.Column<0||c.Column>=Columns||!positions.Add((c.Row,c.Column))||!registry.TryGet(c.Id,out var d)||c.Settings is null)return "Invalid cell.";
   foreach(var p in c.Settings){var def=d.Properties.FirstOrDefault(x=>x.Id==p.Key);if(def is null||p.Value<def.Minimum||p.Value>def.Maximum)return "Invalid property.";}}
  var rows=spell.Cells.GroupBy(c=>c.Row).OrderBy(g=>g.Key).ToArray();
  if(rows[0].Key!=0||rows[0].Any(c=>registry[c.Id].Kind!=ComponentKind.Shape))return "First row must contain Shapes.";
  for(int i=0;i<rows.Length;i++){if(rows[i].Key!=i)return "Rows must be contiguous."; if(rows[i].Any(c=>registry[c.Id].Kind==ComponentKind.Modifier))return "Modifiers are not implemented."; if(rows[i].Select(c=>registry[c.Id].Kind).Distinct().Count()!=1)return "Do not mix Shapes and Effects in a row in this build.";}
  if(registry[rows[^1].First().Id].Kind!=ComponentKind.Effect)return "Last row must contain Effects.";
  // Bound branching; this milestone implements Target only.
  if(rows.Any(r=>registry[r.First().Id].Kind==ComponentKind.Shape&&r.Count()!=1))return "One Shape per row in this build.";
  return null;
 }
 public float Cost(Spell spell,float multiplier=1)=>spell.Cells.Sum(c=>registry[c.Id].Cost(c))*multiplier;
 public static string Encode(Spell s)=>JsonSerializer.Serialize(s);
 public Spell Decode(string json){if(string.IsNullOrEmpty(json)||json.Length>MaxJson)throw new FormatException("Spell data too large or empty.");var s=JsonSerializer.Deserialize<Spell>(json,new JsonSerializerOptions{MaxDepth=12});var error=Validate(s);if(error!=null)throw new FormatException(error);return s!;}
 public int Execute(ISpellWorld world,Spell spell,ShapeResolution root){var error=Validate(spell);if(error!=null)throw new ArgumentException(error);int hits=0;
  void Row(int row,ShapeResolution parent){var cells=spell.Cells.Where(c=>c.Row==row).OrderBy(c=>c.Column).ToArray();if(cells.Length==0)return;bool shape=false;
   bool Accept(long id){var next=spell.Cells.Where(c=>c.Row==row+1&&registry[c.Id].Kind==ComponentKind.Effect).ToArray();return next.Length==0||next.Any(c=>registry[c.Id].Accepts(world,id));}
   foreach(var c in cells){var d=registry[c.Id];var ctx=new ExecutionContext(world,spell,c,parent,Accept);if(d.Kind==ComponentKind.Shape){shape=true;var resolved=d.Resolve(ctx);if(resolved!=null)Row(row+1,resolved);}else if(d.Kind==ComponentKind.Effect)hits+=d.Apply(ctx);}
   if(!shape)Row(row+1,parent);
  } Row(0,root);return hits;
 }
}
