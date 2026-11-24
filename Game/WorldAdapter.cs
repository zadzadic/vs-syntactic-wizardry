using System.Numerics;
using SyntacticWizardry.Core;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
namespace SyntacticWizardry.Game;
internal sealed class WorldAdapter(ICoreServerAPI api,IServerPlayer player):ISpellWorld {
 private EntityPlayer Owner=>player.Entity;
 private static Vector3 V(Vec3d p)=>new((float)p.X,(float)p.Y,(float)p.Z);
 public ShapeResolution Root(){var p=Owner.Pos.XYZ.Add(Owner.LocalEyePos);var end=Owner.Pos.GetViewVector();return new(V(p),new Vector3(end.X,end.Y,end.Z),Vector3.UnitY,null,new HashSet<Voxel>(),null);}
 public bool IsLiving(long id)=>api.World.GetEntityById(id) is EntityAgent e&&e.Alive&&e.EntityId!=Owner.EntityId&&CanAttack(e);
 private bool CanAttack(Entity e)=>e is EntityPlayer?player.HasPrivilege("attackplayers")&&api.World.Config.GetBool("allowPvP",true):player.HasPrivilege("attackcreatures");
 public ShapeResolution? Trace(ShapeResolution parent,TargetMode mode,bool inward,double range,System.Func<long,bool> accepts){
  var p=parent.Origin;var d=Vector3.Normalize(parent.Forward);var start=new Vec3d(p.X,p.Y,p.Z);var end=new Vec3d(p.X+d.X*range,p.Y+d.Y*range,p.Z+d.Z*range);
  BlockSelection? block=null;EntitySelection? entity=null;
  api.World.RayTraceForSelection(start,end,ref block,ref entity,null,e=>mode!=TargetMode.Blocks&&e.EntityId!=Owner.EntityId&&accepts(e.EntityId));
  if(entity?.Entity!=null&&mode!=TargetMode.Blocks){var hit=entity.Entity.Pos.XYZ;return new(V(hit),d,Vector3.UnitY,null,new HashSet<Voxel>(),entity.Entity.EntityId);}
  if(block==null||mode==TargetMode.Entities)return null;
  var b=block.Position;var n=block.Face.Normali;var normal=new Vector3(n.X,n.Y,n.Z)*(inward?-1:1);var origin=new Vector3(b.X+.5f,b.Y+.5f,b.Z+.5f)+(inward?Vector3.Zero:normal);
  return new(origin,normal,Math.Abs(normal.Y)>.5?-Vector3.UnitZ:Vector3.UnitY,normal,new HashSet<Voxel>{new(b.X,b.Y,b.Z)},null);
 }
 public IEnumerable<long> LivingIn(IReadOnlySet<Voxel> voxels){
  if(voxels.Count==0)yield break;
  int minX=voxels.Min(v=>v.X),maxX=voxels.Max(v=>v.X),minY=voxels.Min(v=>v.Y),maxY=voxels.Max(v=>v.Y),minZ=voxels.Min(v=>v.Z),maxZ=voxels.Max(v=>v.Z);
  var center=new Vec3d((minX+maxX+1)*.5,(minY+maxY+1)*.5,(minZ+maxZ+1)*.5);
  float radius=(float)Math.Sqrt(Math.Pow((maxX-minX+1)*.5,2)+Math.Pow((maxZ-minZ+1)*.5,2))+4;
  float vertical=(maxY-minY+1)*.5f+4;
  foreach(var e in api.World.GetEntitiesAround(center,radius,vertical)){
   if(!IsLiving(e.EntityId))continue;var p=e.Pos;var b=e.CollisionBox;bool hit=false;
   int x0=Math.Max(minX,(int)Math.Floor(p.X+b.X1)),x1=Math.Min(maxX,(int)Math.Ceiling(p.X+b.X2)-1);
   int y0=Math.Max(minY,(int)Math.Floor(p.Y+b.Y1)),y1=Math.Min(maxY,(int)Math.Ceiling(p.Y+b.Y2)-1);
   int z0=Math.Max(minZ,(int)Math.Floor(p.Z+b.Z1)),z1=Math.Min(maxZ,(int)Math.Ceiling(p.Z+b.Z2)-1);
   for(int y=y0;y<=y1&&!hit;y++)for(int z=z0;z<=z1&&!hit;z++)for(int x=x0;x<=x1&&!hit;x++)hit=voxels.Contains(new Voxel(x,y,z));
   if(hit)yield return e.EntityId;
  }
 }
 public bool Damage(long id,int amount){if(!IsLiving(id))return false;var e=api.World.GetEntityById(id);return e.ReceiveDamage(new DamageSource{Source=EnumDamageSource.Player,SourceEntity=Owner,CauseEntity=Owner,Type=EnumDamageType.BluntAttack,DamageTier=0},amount);}
}
