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
 public ShapeResolution Root(){var p=Owner.ServerPos.XYZ.Add(Owner.LocalEyePos);var end=Owner.ServerPos.GetViewVector();return new(V(p),new Vector3(end.X,end.Y,end.Z),Vector3.UnitY,null,new HashSet<Voxel>(),null);}
 public bool IsLiving(long id)=>api.World.GetEntityById(id) is EntityAgent e&&e.Alive&&e.EntityId!=Owner.EntityId&&CanAttack(e);
 private bool CanAttack(Entity e)=>e is EntityPlayer?player.HasPrivilege("attackplayers")&&api.World.Config.GetBool("allowPvP",true):player.HasPrivilege("attackcreatures");
 public ShapeResolution? Trace(ShapeResolution parent,TargetMode mode,bool inward,double range,Func<long,bool> accepts){
  var p=parent.Origin;var d=Vector3.Normalize(parent.Forward);var start=new Vec3d(p.X,p.Y,p.Z);var end=new Vec3d(p.X+d.X*range,p.Y+d.Y*range,p.Z+d.Z*range);
  BlockSelection? block=null;EntitySelection? entity=null;
  api.World.RayTraceForSelection(start,end,ref block,ref entity,null,e=>mode!=TargetMode.Blocks&&e.EntityId!=Owner.EntityId&&accepts(e.EntityId));
  if(entity?.Entity!=null&&mode!=TargetMode.Blocks){var hit=entity.Entity.ServerPos.XYZ;return new(V(hit),d,Vector3.UnitY,null,new HashSet<Voxel>(),entity.Entity.EntityId);}
  if(block==null||mode==TargetMode.Entities)return null;
  var b=block.Position;var n=block.Face.Normali;var normal=new Vector3(n.X,n.Y,n.Z)*(inward?-1:1);var origin=new Vector3(b.X+.5f,b.Y+.5f,b.Z+.5f)+(inward?Vector3.Zero:normal);
  return new(origin,normal,Math.Abs(normal.Y)>.5?Vector3.UnitZ:Vector3.UnitY,normal,new HashSet<Voxel>{new(b.X,b.Y,b.Z)},null);
 }
 public IEnumerable<long> LivingIn(IReadOnlySet<Voxel> voxels){if(voxels.Count==0)yield break;var seen=new HashSet<long>();foreach(var v in voxels){foreach(var e in api.World.GetEntitiesAround(new Vec3d(v.X+.5,v.Y+.5,v.Z+.5),4,4)){if(!IsLiving(e.EntityId)||seen.Contains(e.EntityId))continue;var p=e.ServerPos;var b=e.CollisionBox;if(p.X+b.X2>v.X&&p.X+b.X1<v.X+1&&p.Y+b.Y2>v.Y&&p.Y+b.Y1<v.Y+1&&p.Z+b.Z2>v.Z&&p.Z+b.Z1<v.Z+1){seen.Add(e.EntityId);yield return e.EntityId;}}}}
 public bool Damage(long id,int amount){if(!IsLiving(id))return false;var e=api.World.GetEntityById(id);return e.ReceiveDamage(new DamageSource{Source=EnumDamageSource.Player,SourceEntity=Owner,CauseEntity=Owner,Type=EnumDamageType.BluntAttack,DamageTier=0},amount);}
}
