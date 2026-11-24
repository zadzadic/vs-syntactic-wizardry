using SyntacticWizardry.Core;
using System.Numerics;
var engine=new Engine(new Registry());var spell=Engine.Starter();void Check(bool b,string m){if(!b)throw new Exception(m);}
Check(engine.Validate(spell)==null,"starter valid");Check(engine.Cost(spell,.75f)==4.5f,"wand cost");Check(engine.Decode(Engine.Encode(spell)).Cells.Count==2,"round trip");var world=new Fake();var root=new ShapeResolution(Vector3.Zero,Vector3.UnitZ,Vector3.UnitY,null,new HashSet<Voxel>(),null);Check(engine.Execute(world,spell,root)==1&&world.Amount==1,"hit");world.Miss=true;Check(engine.Execute(world,spell,root)==0,"miss stops continuation");spell.Cells[1].Settings["potence"]=11;Check(engine.Validate(spell)!=null,"reject bad potency");spell.Cells[1].Settings.Clear();spell.Cells[1].Row=2;Check(engine.Validate(spell)!=null,"reject gap");var reg=new Registry();
var center=new Vector3(.5f);
Check(SpatialGeometry.Sphere(center,1,1,0).Count==7,"sphere r1");
Check(SpatialGeometry.Sphere(center,1,1,1).Count==6&&SpatialGeometry.Sphere(center,1,1,2).Count==6,"hemispheres");
Check(SpatialGeometry.Box(center,Vector3.UnitZ,Vector3.UnitY,2,3,4).Count==24,"box dimensions");
Check(SpatialGeometry.Box(center,Vector3.UnitY,Vector3.UnitY,2,3,4).Count==24,"vertical box");
Check(SpatialGeometry.Cone(center,Vector3.UnitZ,3,3,3).Count==14,"cone layers");
Check(SpatialGeometry.Circle(center,Vector3.UnitZ,Vector3.UnitY,2).Count==13,"circle disk");
Check(SpatialGeometry.Triangle(center,Vector3.UnitZ,Vector3.UnitY,5,3).Count==9,"triangle rows");
Check(SpatialGeometry.CellAt(new Vector3(-.1f,-1.1f,-2.1f))==new Voxel(-1,-2,-3),"negative coordinates");
world.Miss=false;world.Area=true;
foreach(var id in new[]{"sphere","box","cone","circle","triangle","floating"}){
 var s=new Spell{Name=id,Cells=new(){new(){Row=0,Id=id},new(){Row=1,Id="damage"}}};
 Check(engine.Validate(s)==null,id+" validates");Check(engine.Execute(world,s,root)==1,id+" area hit");Check(world.LastVoxels>0,id+" occupied voxels");
}
var chain=new Spell{Name="chain",Cells=new(){new(){Row=0,Id="target",Settings=new(){{"target_type",1}}},new(){Row=1,Id="floating",Settings=new(){{"distance",3}}},new(){Row=2,Id="sphere"},new(){Row=3,Id="damage"}}};
Check(engine.Execute(world,chain,root)==1&&world.LastVoxels==7,"chained shape clears direct entity and resolves area");
var fc=new Cell{Id="floating",Settings=new(){{"distance",3}}};
var fp=reg["floating"].Resolve(new SyntacticWizardry.Core.ExecutionContext(world,chain,fc,root,_=>true));
Check(fp!.Origin==new Vector3(0,0,3)&&fp.Policy==VectorPolicy.Radial&&fp.DirectEntity==null,"floating frame");
Check(reg["sphere"].Cost(new Cell{Id="sphere"})==7,"sphere canonical cost");
Check(engine.Decode(Engine.Encode(chain)).Cells.Count==4,"new spell serialization");
Console.WriteLine("Core tests passed");
class Fake:ISpellWorld {public bool Miss;public int Amount;public bool Area;public int LastVoxels;public bool IsLiving(long id)=>id==1;public bool Damage(long id,int amount){Amount+=amount;return true;}public IEnumerable<long> LivingIn(IReadOnlySet<Voxel> v){LastVoxels=v.Count;return Area&&v.Count>0?new long[]{1}:Array.Empty<long>();}public ShapeResolution? Trace(ShapeResolution p,TargetMode m,bool i,double r,Func<long,bool>a)=>Miss||!a(1)?null:p with{DirectEntity=1};}
